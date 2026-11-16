"""Rebuilds wwwroot/data/countries.json and wwwroot/data/country-shapes.json for the map explorer.

Run from a folder containing these downloads, passing the output folder as the only argument:
    python3 build-country-data.py ../wwwroot/data

  ne.geojson      https://cdn.jsdelivr.net/gh/nvkelso/natural-earth-vector@v5.1.2/geojson/ne_110m_admin_0_countries.geojson
  ne50.geojson    https://cdn.jsdelivr.net/gh/nvkelso/natural-earth-vector@v5.1.2/geojson/ne_50m_admin_0_countries.geojson
  places.geojson  https://cdn.jsdelivr.net/gh/nvkelso/natural-earth-vector@v5.1.2/geojson/ne_110m_populated_places_simple.geojson
  wc.json         https://cdn.jsdelivr.net/npm/world-countries@5.0.0/countries.json

Natural Earth is public domain (shapes, population, capital locations).
world-countries is licensed ODbL (names, capitals, languages, currencies, borders, area).
"""
import json, sys
out = sys.argv[1]
ne = json.load(open('ne.geojson'))['features']
ne50 = json.load(open('ne50.geojson'))['features']
places = json.load(open('places.geojson'))['features']
wc = json.load(open('wc.json'))
known = {c['cca3'] for c in wc}

def iso(p):
    for k in ('ISO_A3_EH', 'ISO_A3', 'ADM0_A3'):
        v = p.get(k)
        if v and v != '-99' and v in known:
            return v
    return None

pop = {}
for f in ne50 + ne:
    p = f['properties']; i = iso(p)
    if i and p.get('POP_EST'):
        pop.setdefault(i, (int(p['POP_EST']), int(p['POP_YEAR'])))

caps = {}
for f in places:
    p = f['properties']
    if (p.get('featurecla') or '').startswith('Admin-0 capital') and p.get('adm0_a3') in known:
        caps.setdefault(p['adm0_a3'], [round(p['latitude'], 2), round(p['longitude'], 2)])

def rnd(c):
    return [rnd(x) for x in c] if isinstance(c[0], list) else [round(c[0], 2), round(c[1], 2)]

shapes, unmatched = [], []
for f in ne:
    i = iso(f['properties'])
    if not i:
        unmatched.append(f['properties']['NAME']); continue
    g = f['geometry']
    shapes.append({'type': 'Feature', 'properties': {'id': i}, 'geometry': {'type': g['type'], 'coordinates': rnd(g['coordinates'])}})
shape_ids = {s['properties']['id'] for s in shapes}

countries = []
for c in sorted(wc, key=lambda c: c['name']['common']):
    i = c['cca3']
    idd = c.get('idd') or {}
    suffixes = idd.get('suffixes') or []
    countries.append({
        'id': i,
        'name': c['name']['common'],
        'official': c['name']['official'],
        'capital': c.get('capital') or [],
        'region': c.get('region') or '',
        'subregion': c.get('subregion') or '',
        'population': pop.get(i, (None, None))[0],
        'populationYear': pop.get(i, (None, None))[1],
        'area': c['area'] if c.get('area', -1) > 0 else None,
        'languages': sorted((c.get('languages') or {}).values()),
        'currencies': [f"{v['name']} ({k})" for k, v in (c.get('currencies') or {}).items()],
        'borders': [b for b in c.get('borders') or [] if b in known],
        'landlocked': bool(c.get('landlocked')),
        'unMember': bool(c.get('unMember')),
        'independent': bool(c.get('independent')),
        'domain': (c.get('tld') or [''])[0],
        'callingCode': (idd.get('root') or '') + (suffixes[0] if len(suffixes) == 1 else ''),
        'center': c['latlng'],
        'capitalLocation': caps.get(i),
        'hasShape': i in shape_ids,
    })

json.dump(countries, open(out + '/countries.json', 'w'), ensure_ascii=False, separators=(',', ':'))
json.dump({'type': 'FeatureCollection', 'features': shapes}, open(out + '/country-shapes.json', 'w'), separators=(',', ':'))
print('countries', len(countries), 'shapes', len(shapes), 'unmatched shapes', unmatched)
print('no population', [c['name'] for c in countries if c['population'] is None])
print('no capital loc', sum(1 for c in countries if not c['capitalLocation']))
