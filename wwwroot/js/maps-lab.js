(() => {
    const presets = [
        {
            id: 'world',
            label: 'World overview',
            bounds: [[-42, -169], [75, 190]]
        },
        {
            id: 'north-america',
            label: 'North America',
            bounds: [[13, -168], [72, -50]]
        },
        {
            id: 'south-america',
            label: 'South America',
            bounds: [[-55, -93], [15, -30]]
        },
        {
            id: 'europe',
            label: 'Europe',
            bounds: [[35, -11], [72, 45]]
        },
        {
            id: 'africa',
            label: 'Africa',
            bounds: [[-35, -19], [38, 55]]
        },
        {
            id: 'asia',
            label: 'Asia',
            bounds: [[-12, 40], [55, 146]]
        },
        {
            id: 'oceania',
            label: 'Oceania focus',
            bounds: [[-55, 110], [-5, 180]]
        }
    ];

    const root = document.getElementById('syphonic-map-app');
    if (!root || !window.L)
        return;

    const presetContainer = root.querySelector('[data-map-presets]');
    const mapContainer = root.querySelector('#map-leaflet');
    const meta = root.querySelector('[data-map-meta]');

    if (!presetContainer || !mapContainer || !meta)
        return;

    const map = L.map(mapContainer, {
        worldCopyJump: true
    }).setView([20, 10], 2);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://openstreetmap.org">OpenStreetMap</a>'
    }).addTo(map);

    const applyBounds = bounds => {
        map.fitBounds(bounds, {
            animate: false,
            padding: [18, 18]
        });
    };

    applyBounds(L.latLngBounds(presets[0].bounds));

    presetContainer.innerHTML = '';
    presets.forEach(preset => {
        const pill = document.createElement('button');
        pill.type = 'button';
        pill.className = 'btn btn-sm btn-outline-primary';
        pill.textContent = preset.label;
        pill.addEventListener('click', () => {
            presetContainer.querySelectorAll('button[data-map-preset="1"]').forEach(btn =>
                btn.classList.remove('active'));
            pill.classList.add('active');

            meta.textContent = `Focus: ${preset.label}`;
            applyBounds(L.latLngBounds(preset.bounds));
            map.invalidateSize(false);
        });
        pill.dataset.mapPreset = '1';
        presetContainer.appendChild(pill);
    });

    presetContainer.querySelector('button')?.classList.add('active');

    requestAnimationFrame(() => map.invalidateSize(false));
})();
