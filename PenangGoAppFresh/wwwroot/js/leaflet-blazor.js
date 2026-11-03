window.leafletBlazor = {
    map: null,
    markers: [],
    sites: [],
    clusterLayer: null,

    initMap: function (mapId, sites, centerLat, centerLng, zoom) {
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
        this.map = L.map(mapId).setView([centerLat, centerLng], zoom);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);
        this.sites = sites;
        var self = this;
        this.map.on('zoomend', function () { self.renderMarkers(); });
        this.renderMarkers();
    },

    // Lightweight clustering: markers within ~40px are grouped, re-clustered on zoom
    renderMarkers: function () {
        if (this.clusterLayer) {
            this.map.removeLayer(this.clusterLayer);
        }
        this.clusterLayer = L.layerGroup();
        this.markers = [];

        var sites = (this.sites || []).filter(function (s) { return s.x !== 0 && s.y !== 0; });
        var clusterRadius = 40;
        var clusters = [];

        for (var i = 0; i < sites.length; i++) {
            var placed = false;
            for (var j = 0; j < clusters.length; j++) {
                if (this.distancePx(sites[i], clusters[j].lat, clusters[j].lng) < clusterRadius) {
                    clusters[j].sites.push(sites[i]);
                    placed = true;
                    break;
                }
            }
            if (!placed) {
                clusters.push({ lat: sites[i].y, lng: sites[i].x, sites: [sites[i]] });
            }
        }

        for (var k = 0; k < clusters.length; k++) {
            var cluster = clusters[k];
            if (cluster.sites.length === 1) {
                var site = cluster.sites[0];
                var marker = L.marker([site.y, site.x]).addTo(this.clusterLayer);
                marker.bindPopup('<b>' + site.name + '</b><br/>' + site.category);
                this.markers.push(marker);
            } else {
                var avgLat = 0, avgLng = 0;
                for (var m = 0; m < cluster.sites.length; m++) {
                    avgLat += cluster.sites[m].y;
                    avgLng += cluster.sites[m].x;
                }
                avgLat /= cluster.sites.length;
                avgLng /= cluster.sites.length;

                var divIcon = L.divIcon({
                    className: 'site-cluster-icon',
                    html: '<div class="site-cluster">' + cluster.sites.length + '</div>',
                    iconSize: [34, 34]
                });

                var clusterMarker = L.marker([avgLat, avgLng], { icon: divIcon })
                    .addTo(this.clusterLayer)
                    .bindPopup('<b>' + cluster.sites.length + ' heritage sites</b>');
                this.markers.push(clusterMarker);
            }
        }

        this.clusterLayer.addTo(this.map);
    },

    distancePx: function (site, lat, lng) {
        var p1 = this.map.latLngToContainerPoint([site.y, site.x]);
        var p2 = this.map.latLngToContainerPoint([lat, lng]);
        var dx = p1.x - p2.x, dy = p1.y - p2.y;
        return Math.sqrt(dx * dx + dy * dy);
    }
};