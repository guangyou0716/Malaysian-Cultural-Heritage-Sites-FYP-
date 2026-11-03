window.panoramaViewer = {
    _viewers: {},

    init: function (containerId, imageUrl, options) {
        this.destroy(containerId);

        var container = document.getElementById(containerId);
        if (!container) return;

        var config = Object.assign({
            type: 'equirectangular',
            panorama: imageUrl,
            autoLoad: true,
            autoRotate: -2,
            compass: false,
            showControls: true,
            hfov: 110,
            vfov: 70,
            minHfov: 50,
            maxHfov: 130,
            pitch: 0,
            yaw: 0
        }, options || {});

        // Respect reduced motion: OS preference or the in-app setting
        var reduceMotion = (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) ||
            document.documentElement.getAttribute('data-reduce-motion') === 'true';
        if (reduceMotion) {
            config.autoRotate = 0;
        }

        if (typeof pannellum !== 'undefined') {
            this._viewers[containerId] = pannellum.viewer(containerId, config);
        } else {
            container.innerHTML = '';
            var img = document.createElement('img');
            img.src = imageUrl;
            img.style.cssText = 'width:100%;height:100%;object-fit:cover;cursor:grab;';
            img.alt = 'Panorama';

            var isDragging = false, startX = 0, startY = 0, rotX = 0, rotY = 0;
            img.addEventListener('mousedown', function (e) {
                isDragging = true; startX = e.clientX; startY = e.clientY;
                img.style.cursor = 'grabbing';
            });
            document.addEventListener('mousemove', function (e) {
                if (!isDragging) return;
                rotY += (e.clientX - startX) * 0.2; startX = e.clientX;
                rotX += (e.clientY - startY) * 0.2; startY = e.clientY;
                rotX = Math.max(-30, Math.min(30, rotX));
                img.style.transform = 'perspective(600px) rotateX(' + (-rotX) + 'deg) rotateY(' + rotY + 'deg) scale(1.3)';
            });
            document.addEventListener('mouseup', function () {
                isDragging = false; img.style.cursor = 'grab';
            });

            container.appendChild(img);
            this._viewers[containerId] = { _fallback: true, _img: img };
        }
    },

    destroy: function (containerId) {
        var viewer = this._viewers[containerId];
        if (!viewer) return;

        if (viewer._fallback) {
            var container = document.getElementById(containerId);
            if (container) container.innerHTML = '';
        } else if (typeof viewer.destroy === 'function') {
            viewer.destroy();
        }
        delete this._viewers[containerId];
    },

    isSupported: function () {
        var canvas = document.createElement('canvas');
        return !!(canvas.getContext('webgl') || canvas.getContext('experimental-webgl'));
    },

    supportsGyro: function () {
        return typeof window.DeviceOrientationEvent !== 'undefined';
    },

    requestFullscreen: function (containerId) {
        var el = document.getElementById(containerId);
        if (!el) return;
        if (el.requestFullscreen) { el.requestFullscreen(); }
        else if (el.webkitRequestFullscreen) { el.webkitRequestFullscreen(); }
    },

    // User-gesture-gated recreation with device orientation enabled.
    // iOS 13+/Android Chrome require a permission prompt, granted from a tap.
    enableGyroscope: async function (containerId, imageUrl, options) {
        var viewer = this._viewers[containerId];
        if (!viewer || viewer._fallback) return false;

        if (window.DeviceOrientationEvent && typeof window.DeviceOrientationEvent.requestPermission === 'function') {
            try {
                var state = await window.DeviceOrientationEvent.requestPermission();
                if (state !== 'granted') return false;
            } catch (e) {
                return false;
            }
        }

        this.init(containerId, imageUrl, Object.assign({ deviceOrientation: true }, options || {}));
        return true;
    }
};
