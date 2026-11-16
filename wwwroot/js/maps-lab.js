// Map explorer: click a country for its facts, colour the map by a statistic,
// or play the find-the-country challenge. Data comes from two static files in wwwroot/data.
(() => {
    const root = document.getElementById('syphonic-map-app');
    if (!root || !window.L) return;

    const $ = selector => root.querySelector(selector);
    const mapContainer = $('#map-leaflet');
    const presetContainer = $('[data-map-presets]');
    const panel = $('[data-country-panel]');
    const legend = $('[data-map-legend]');
    const message = $('[data-map-message]');
    const exploreTools = $('[data-explore-tools]');
    const searchForm = $('[data-country-search]');
    const searchInput = $('#countrySearch');
    const nameList = $('#countryNames');
    const colorSelect = $('[data-color-by]');
    const randomButton = $('[data-random-country]');
    const challengeButton = $('[data-challenge-toggle]');

    // Region buttons. `matches` decides which countries the challenge asks about in that region.
    const northAmerica = ['North America', 'Central America', 'Caribbean'];
    const presets = [
        { label: 'World', bounds: [[-42, -169], [75, 190]], matches: () => true },
        { label: 'North America', bounds: [[13, -168], [72, -50]], matches: c => northAmerica.includes(c.subregion) },
        { label: 'South America', bounds: [[-55, -93], [15, -30]], matches: c => c.subregion === 'South America' },
        { label: 'Europe', bounds: [[35, -11], [72, 45]], matches: c => c.region === 'Europe' },
        { label: 'Africa', bounds: [[-35, -19], [38, 55]], matches: c => c.region === 'Africa' },
        { label: 'Asia', bounds: [[-12, 40], [55, 146]], matches: c => c.region === 'Asia' },
        { label: 'Oceania', bounds: [[-55, 110], [-5, 180]], matches: c => c.region === 'Oceania' }
    ];

    const colors = {
        outline: '#1c5cab',
        fill: '#2a78d6',
        selected: '#eb6834',
        noData: '#e9ecef',
        // One hue, light to dark: darker always means "more".
        ramp: ['#cde2fb', '#9ec5f4', '#5598e7', '#256abf', '#0d366b'],
        correct: '#0ca30c',
        wrong: '#d03b3b'
    };

    const metrics = {
        population: { label: 'Population', unit: 'people', value: c => c.population },
        area: { label: 'Area', unit: 'km²', value: c => c.area },
        density: {
            label: 'Population density',
            unit: 'people per km²',
            value: c => (c.population && c.area ? c.population / c.area : null)
        }
    };

    const whole = new Intl.NumberFormat('en');
    const compact = new Intl.NumberFormat('en', { notation: 'compact', maximumFractionDigits: 1 });
    const formatDensity = value => (value >= 10 ? whole.format(Math.round(value)) : value.toFixed(1));

    // ---------- State ----------
    let countries = [];
    const byId = new Map();        // country id (ISO alpha-3) -> country facts
    const layerById = new Map();   // country id -> Leaflet layer for its outline
    const ranks = {};              // metric key -> Map(country id -> rank, 1 = largest)
    let selectedId = null;
    let colorBy = 'none';
    let bins = [];                 // upper thresholds of the colour bands for the current metric
    let activePreset = presets[0];
    let marker = null;             // capital (or centre) marker for the selected country
    let challenge = null;          // null while exploring

    // ---------- Small DOM helper: builds elements with textContent, never innerHTML ----------
    const make = (tag, className, text) => {
        const element = document.createElement(tag);
        if (className) element.className = className;
        if (text !== undefined && text !== null) element.textContent = text;
        return element;
    };

    // ---------- Map ----------
    const map = L.map(mapContainer, { worldCopyJump: true }).setView([20, 10], 2);
    const tiles = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://openstreetmap.org">OpenStreetMap</a>'
    }).addTo(map);

    const fit = bounds => map.fitBounds(bounds, { animate: false, padding: [18, 18] });
    fit(L.latLngBounds(activePreset.bounds));

    presets.forEach((preset, index) => {
        const button = make('button', 'btn btn-sm btn-outline-primary', preset.label);
        button.type = 'button';
        if (index === 0) button.classList.add('active');
        button.addEventListener('click', () => {
            presetContainer.querySelectorAll('button').forEach(b => b.classList.remove('active'));
            button.classList.add('active');
            activePreset = preset;
            fit(L.latLngBounds(preset.bounds));
        });
        presetContainer.appendChild(button);
    });

    // ---------- Styling ----------
    const bandFor = value => {
        const index = bins.findIndex(limit => value <= limit);
        return index === -1 ? colors.ramp.length - 1 : index;
    };

    const styleFor = id => {
        if (challenge) {
            const flash = challenge.flash.get(id);
            return {
                color: '#ffffff',
                weight: 1,
                opacity: 1,
                fillColor: flash || colors.ramp[1],
                fillOpacity: 1
            };
        }

        const isSelected = id === selectedId;
        const style = {
            color: isSelected ? colors.selected : colors.outline,
            weight: isSelected ? 3 : 1,
            opacity: isSelected ? 1 : 0.6,
            fillColor: colors.fill,
            fillOpacity: isSelected ? 0.2 : 0.04
        };

        if (colorBy !== 'none') {
            const value = metrics[colorBy].value(byId.get(id));
            style.fillColor = value === null || value === undefined ? colors.noData : colors.ramp[bandFor(value)];
            style.fillOpacity = 0.85;
            if (!isSelected) {
                style.color = '#ffffff';
                style.opacity = 1;
            }
        }

        return style;
    };

    const restyle = () => {
        layerById.forEach((layer, id) => layer.setStyle(styleFor(id)));
        if (selectedId && layerById.has(selectedId) && !challenge) layerById.get(selectedId).bringToFront();
    };

    // ---------- Colour by statistic ----------
    const renderLegend = () => {
        legend.replaceChildren();
        legend.hidden = colorBy === 'none' || Boolean(challenge);
        if (legend.hidden) return;

        const metric = metrics[colorBy];
        legend.appendChild(make('span', 'fw-semibold me-2', `${metric.label} (${metric.unit})`));
        const format = colorBy === 'density' ? formatDensity : value => compact.format(value);
        colors.ramp.forEach((color, index) => {
            const item = make('span', 'sy-map-legend-item');
            const swatch = make('span', 'sy-swatch');
            swatch.style.backgroundColor = color;
            const text = index === 0
                ? `up to ${format(bins[0])}`
                : index === colors.ramp.length - 1
                    ? `over ${format(bins[index - 1])}`
                    : `${format(bins[index - 1])} to ${format(bins[index])}`;
            item.append(swatch, text);
            legend.appendChild(item);
        });
        const none = make('span', 'sy-map-legend-item');
        const swatch = make('span', 'sy-swatch');
        swatch.style.backgroundColor = colors.noData;
        none.append(swatch, 'no data');
        legend.appendChild(none);
    };

    const setColorBy = key => {
        colorBy = key;
        bins = [];
        if (key !== 'none') {
            // Five equal-sized groups of countries (quintiles), so each colour is used about as often.
            const values = countries
                .filter(c => c.hasShape)
                .map(metrics[key].value)
                .filter(v => v !== null && v !== undefined)
                .sort((a, b) => a - b);
            for (let step = 1; step < colors.ramp.length; step++) {
                bins.push(values[Math.floor((values.length * step) / colors.ramp.length)]);
            }
        }
        restyle();
        renderLegend();
    };

    // ---------- Country panel ----------
    const addFact = (list, term, value) => {
        if (value === null || value === undefined || value === '') return;
        list.appendChild(make('dt', 'col-5 text-muted fw-normal', term));
        list.appendChild(make('dd', 'col-7 mb-1', value));
    };

    const rankText = (key, id) => {
        const rank = ranks[key].get(id);
        return rank ? ` (rank ${rank} of ${ranks[key].size})` : '';
    };

    const renderCountry = country => {
        panel.replaceChildren();
        panel.appendChild(make('h2', 'h4 mb-0', country.name));
        if (country.official && country.official !== country.name) {
            panel.appendChild(make('p', 'small text-muted mb-2', country.official));
        }
        panel.appendChild(make('p', 'small mb-3', [country.region, country.subregion].filter(Boolean).join(' · ')));

        const facts = make('dl', 'row small mb-3');
        addFact(facts, country.capital.length > 1 ? 'Capitals' : 'Capital', country.capital.join(', ') || 'None');
        if (country.population) {
            addFact(facts, 'Population', `${whole.format(country.population)}${rankText('population', country.id)}`);
        }
        if (country.area) {
            addFact(facts, 'Area', `${whole.format(Math.round(country.area))} km²${rankText('area', country.id)}`);
        }
        const density = metrics.density.value(country);
        if (density) addFact(facts, 'Density', `${formatDensity(density)} people per km²`);
        addFact(facts, country.languages.length > 1 ? 'Languages' : 'Language', country.languages.join(', '));
        addFact(facts, country.currencies.length > 1 ? 'Currencies' : 'Currency', country.currencies.join(', '));
        addFact(facts, 'Coastline', country.landlocked ? 'Landlocked (no sea coast)' : 'Has a sea coast');
        addFact(facts, 'UN member', country.unMember ? 'Yes' : 'No');
        addFact(facts, 'Calling code', country.callingCode);
        addFact(facts, 'Internet domain', country.domain);
        panel.appendChild(facts);

        const neighbours = country.borders.map(id => byId.get(id)).filter(Boolean)
            .sort((a, b) => a.name.localeCompare(b.name));
        panel.appendChild(make('h3', 'h6', neighbours.length ? `Land neighbours (${neighbours.length})` : 'Land neighbours'));
        if (neighbours.length === 0) {
            panel.appendChild(make('p', 'small text-muted mb-0', 'None. This is an island or a territory with no land border.'));
        } else {
            const wrap = make('div', 'd-flex flex-wrap gap-1');
            neighbours.forEach(neighbour => {
                const button = make('button', 'btn btn-sm btn-outline-secondary', neighbour.name);
                button.type = 'button';
                button.addEventListener('click', () => selectCountry(neighbour.id));
                wrap.appendChild(button);
            });
            panel.appendChild(wrap);
        }
    };

    const selectCountry = (id, { move = true } = {}) => {
        const country = byId.get(id);
        if (!country) return;

        selectedId = id;
        message.textContent = '';
        restyle();
        renderCountry(country);

        if (marker) marker.remove();
        const spot = country.capitalLocation || (country.hasShape ? null : country.center);
        if (spot) {
            marker = L.circleMarker(spot, {
                radius: 6, color: '#ffffff', weight: 2, fillColor: colors.selected, fillOpacity: 1, interactive: false
            }).addTo(map);
            const label = country.capitalLocation && country.capital.length ? `${country.capital[0]} (capital)` : country.name;
            marker.bindTooltip(label, { permanent: true, direction: 'top', offset: [0, -6] });
        }

        if (move) {
            const layer = layerById.get(id);
            if (layer) map.fitBounds(layer.getBounds(), { padding: [30, 30], maxZoom: 6 });
            else map.setView(country.center, 6);
        }

        // Keep the choice in the address bar so the view can be shared or bookmarked.
        const url = new URL(window.location.href);
        url.searchParams.set('country', id);
        window.history.replaceState(null, '', url);
    };

    // ---------- Search and random ----------
    const findByName = text => {
        const query = text.trim().toLowerCase();
        if (!query) return null;
        return countries.find(c => c.name.toLowerCase() === query)
            || countries.find(c => c.official.toLowerCase() === query)
            || countries.find(c => c.name.toLowerCase().startsWith(query))
            || countries.find(c => c.name.toLowerCase().includes(query))
            || null;
    };

    searchForm.addEventListener('submit', event => {
        event.preventDefault();
        const match = findByName(searchInput.value);
        if (match) {
            searchInput.value = match.name;
            selectCountry(match.id);
        } else {
            message.textContent = searchInput.value.trim() ? `No country matches “${searchInput.value.trim()}”.` : '';
        }
    });

    randomButton.addEventListener('click', () => {
        const pool = countries.filter(c => c.independent && c.id !== selectedId);
        if (pool.length) selectCountry(pool[Math.floor(Math.random() * pool.length)].id);
    });

    colorSelect.addEventListener('change', () => setColorBy(colorSelect.value));

    // ---------- Find-the-country challenge ----------
    const roundLength = 10;
    const maxTries = 3;

    const shuffle = items => {
        const copy = [...items];
        for (let i = copy.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [copy[i], copy[j]] = [copy[j], copy[i]];
        }
        return copy;
    };

    const renderChallenge = feedback => {
        panel.replaceChildren();
        const target = challenge.queue[challenge.index];
        const finished = challenge.index >= challenge.queue.length;

        panel.appendChild(make('p', 'small text-muted mb-1',
            finished ? 'Challenge finished' : `Question ${challenge.index + 1} of ${challenge.queue.length} · ${challenge.scope}`));

        if (finished) {
            panel.appendChild(make('h2', 'h4', `You found ${challenge.score} of ${challenge.queue.length}`));
            panel.appendChild(make('p', 'small text-muted', 'A country counts when you click it within three tries. Scores here are for practice and are not saved.'));
            const again = make('button', 'btn btn-primary btn-sm me-2', 'Play again');
            again.type = 'button';
            again.addEventListener('click', startChallenge);
            panel.appendChild(again);
        } else {
            panel.appendChild(make('h2', 'h4', `Find ${target.name}`));
            panel.appendChild(make('p', 'small text-muted', 'Click the country on the map. Place names are hidden during the challenge.'));
            if (feedback) panel.appendChild(make('p', `fw-semibold ${feedback.good ? 'text-success' : 'text-danger'}`, feedback.text));
            if (challenge.answered) {
                const facts = make('p', 'small');
                facts.textContent = `${target.name}: capital ${target.capital.join(', ') || 'none'}, ${target.subregion || target.region}.`;
                panel.appendChild(facts);
                const next = make('button', 'btn btn-primary btn-sm me-2',
                    challenge.index + 1 >= challenge.queue.length ? 'See result' : 'Next country');
                next.type = 'button';
                next.addEventListener('click', () => {
                    challenge.index++;
                    challenge.answered = false;
                    challenge.tries = 0;
                    challenge.flash.clear();
                    restyle();
                    renderChallenge();
                });
                panel.appendChild(next);
                next.focus();
            } else {
                const skip = make('button', 'btn btn-outline-secondary btn-sm me-2', 'Show me');
                skip.type = 'button';
                skip.addEventListener('click', () => reveal(target, `Here is ${target.name}.`));
                panel.appendChild(skip);
            }
            panel.appendChild(make('span', 'small text-muted', `Score: ${challenge.score}`));
        }

        const stop = make('button', 'btn btn-link btn-sm d-block px-0 mt-3', 'Back to exploring');
        stop.type = 'button';
        stop.addEventListener('click', stopChallenge);
        panel.appendChild(stop);
    };

    const reveal = (target, text) => {
        challenge.answered = true;
        challenge.flash.set(target.id, colors.selected);
        restyle();
        map.fitBounds(layerById.get(target.id).getBounds(), { padding: [60, 60], maxZoom: 5 });
        renderChallenge({ good: false, text });
    };

    const guess = id => {
        if (challenge.answered || challenge.index >= challenge.queue.length) return;
        const target = challenge.queue[challenge.index];

        if (id === target.id) {
            challenge.answered = true;
            challenge.score++;
            challenge.flash.set(id, colors.correct);
            restyle();
            renderChallenge({ good: true, text: 'Correct!' });
            return;
        }

        challenge.tries++;
        challenge.flash.set(id, colors.wrong);
        restyle();
        const left = maxTries - challenge.tries;
        if (left <= 0) {
            reveal(target, `That was ${byId.get(id).name}. ${target.name} is highlighted on the map.`);
        } else {
            renderChallenge({ good: false, text: `That is ${byId.get(id).name}. ${left} ${left === 1 ? 'try' : 'tries'} left.` });
        }
    };

    function startChallenge() {
        const pool = countries.filter(c => c.hasShape && c.independent && activePreset.matches(c));
        challenge = {
            queue: shuffle(pool).slice(0, roundLength),
            scope: activePreset.label,
            index: 0,
            score: 0,
            tries: 0,
            answered: false,
            flash: new Map()   // country id -> temporary fill colour (right, wrong, revealed)
        };

        // Hide everything that names places: map labels, hover tooltips, the search tools.
        tiles.remove();
        layerById.forEach(layer => layer.unbindTooltip());
        if (marker) marker.remove();
        exploreTools.hidden = true;
        message.textContent = '';
        challengeButton.textContent = 'End challenge';
        mapContainer.classList.add('sy-map-challenge');
        fit(L.latLngBounds(activePreset.bounds));
        restyle();
        renderLegend();
        renderChallenge();
    }

    function stopChallenge() {
        challenge = null;
        tiles.addTo(map);
        layerById.forEach((layer, id) => layer.bindTooltip(byId.get(id).name, { sticky: true }));
        exploreTools.hidden = false;
        challengeButton.textContent = 'Start challenge';
        mapContainer.classList.remove('sy-map-challenge');
        restyle();
        renderLegend();
        if (selectedId) {
            selectCountry(selectedId, { move: false });
        } else {
            panel.replaceChildren(
                make('h2', 'h5', 'Pick a country'),
                make('p', 'small text-muted mb-0', 'Click the map, search by name, or press “Surprise me”. Country facts appear here.'));
        }
    }

    challengeButton.addEventListener('click', () => (challenge ? stopChallenge() : startChallenge()));

    // ---------- Load data ----------
    const loadJson = url => fetch(url).then(response => {
        if (!response.ok) throw new Error(`${url} returned ${response.status}`);
        return response.json();
    });

    challengeButton.disabled = true;
    Promise.all([loadJson(root.dataset.countriesUrl), loadJson(root.dataset.shapesUrl)])
        .then(([countryList, shapes]) => {
            countries = countryList;
            countries.forEach(country => {
                byId.set(country.id, country);
                const option = document.createElement('option');
                option.value = country.name;
                nameList.appendChild(option);
            });

            Object.keys(metrics).forEach(key => {
                const sorted = countries
                    .map(c => ({ id: c.id, value: metrics[key].value(c) }))
                    .filter(entry => entry.value)
                    .sort((a, b) => b.value - a.value);
                ranks[key] = new Map(sorted.map((entry, index) => [entry.id, index + 1]));
            });

            L.geoJSON(shapes, {
                style: feature => styleFor(feature.properties.id),
                onEachFeature: (feature, layer) => {
                    const id = feature.properties.id;
                    layerById.set(id, layer);
                    layer.bindTooltip(byId.get(id).name, { sticky: true });
                    layer.on('click', () => (challenge ? guess(id) : selectCountry(id, { move: false })));
                    layer.on('mouseover', () => {
                        if (!challenge && id !== selectedId) layer.setStyle({ fillOpacity: colorBy === 'none' ? 0.18 : 1 });
                    });
                    layer.on('mouseout', () => layer.setStyle(styleFor(id)));
                }
            }).addTo(map);

            challengeButton.disabled = false;

            // Open a country straight away when the address says so, e.g. /Maps?country=JPN
            const requested = new URLSearchParams(window.location.search).get('country');
            if (requested && byId.has(requested.toUpperCase())) selectCountry(requested.toUpperCase());
        })
        .catch(() => {
            message.textContent = 'Country data could not be loaded. The map still works, but country facts are unavailable.';
        });

    requestAnimationFrame(() => map.invalidateSize(false));
})();
