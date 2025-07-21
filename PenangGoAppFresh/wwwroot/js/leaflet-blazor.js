window.leafletBlazor = {
    map: null,
    markers: [],
    initMap: function (mapId, sites, centerLat, centerLng, zoom) {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
        this.map = L.map(mapId).setView([centerLat, centerLng], zoom);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);
        this.markers = [];
        for (let i = 0; i < sites.length; i++) {
            let site = sites[i];
            let marker = L.marker([site.y, site.x]).addTo(this.map);
            marker.bindPopup(`<b>${site.name}</b><br/>${site.category}`);
            this.markers.push(marker);
        }
    }
}; 