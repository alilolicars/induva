// INDUVA Interactive Industrial & Resource Map
// Secure marker popup rendering with zero innerHTML / Html.Raw

(function () {
    const mapElement = document.getElementById("induva-map");
    if (!mapElement) return;

    // Center of Egypt
    const defaultCenter = [26.8206, 30.8025];
    const defaultZoom = 6;

    const map = L.map(mapElement).setView(defaultCenter, defaultZoom);

    // Vendored Leaflet icon fix
    const iconRetinaUrl = '/lib/leaflet/marker-icon-2x.png';
    const iconUrl = '/lib/leaflet/marker-icon.png';
    const shadowUrl = '/lib/leaflet/marker-shadow.png';

    L.Icon.Default.mergeOptions({
        iconRetinaUrl: iconRetinaUrl,
        iconUrl: iconUrl,
        shadowUrl: shadowUrl
    });

    // OpenStreetMap tile layer (graceful fallback)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 18,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    const markerGroup = L.layerGroup().addTo(map);

    function createMarkerPopup(markerData) {
        const container = document.createElement("div");
        container.className = "induva-popup";

        const badge = document.createElement("span");
        badge.className = "badge mb-1 text-uppercase";
        switch (markerData.type) {
            case "factories":
                badge.className += " bg-primary";
                badge.textContent = "Factory";
                break;
            case "suppliers":
                badge.className += " bg-success";
                badge.textContent = "Supplier";
                break;
            case "resources":
                badge.className += " bg-warning text-dark";
                badge.textContent = "Natural Resource";
                break;
            case "demand":
                badge.className += " bg-info text-dark";
                badge.textContent = "Open RFQ / Demand";
                break;
            default:
                badge.className += " bg-secondary";
                badge.textContent = markerData.type;
        }
        container.appendChild(badge);

        const title = document.createElement("h6");
        title.className = "fw-bold mb-1";
        title.textContent = markerData.title || "";
        container.appendChild(title);

        if (markerData.subtitle) {
            const sub = document.createElement("p");
            sub.className = "small text-muted mb-2";
            sub.textContent = markerData.subtitle;
            container.appendChild(sub);
        }

        if (markerData.url) {
            const link = document.createElement("a");
            link.href = markerData.url;
            link.className = "btn btn-sm btn-outline-primary d-block text-center";
            link.textContent = "View Details →";
            container.appendChild(link);
        }

        return container;
    }

    async function loadMarkers() {
        markerGroup.clearLayers();

        const activeLayers = [];
        document.querySelectorAll(".layer-checkbox:checked").forEach(cb => {
            activeLayers.push(cb.value);
        });

        const govSelect = document.getElementById("governorate-select");
        const govId = govSelect ? govSelect.value : "";

        const params = new URLSearchParams();
        activeLayers.forEach(l => params.append("layers", l));
        if (govId) params.append("governorateId", govId);

        try {
            const res = await fetch(`/api/map?${params.toString()}`);
            if (!res.ok) return;

            const markers = await res.json();
            const bounds = [];

            markers.forEach(m => {
                if (typeof m.lat === "number" && typeof m.lng === "number") {
                    const marker = L.marker([m.lat, m.lng]);
                    marker.bindPopup(() => createMarkerPopup(m));
                    markerGroup.addLayer(marker);
                    bounds.push([m.lat, m.lng]);
                }
            });

            if (bounds.length > 0 && govId) {
                map.fitBounds(bounds, { maxZoom: 10, padding: [30, 30] });
            }
        } catch (e) {
            console.warn("Failed to load map markers:", e);
        }
    }

    // Attach event listeners
    document.querySelectorAll(".layer-checkbox").forEach(cb => {
        cb.addEventListener("change", loadMarkers);
    });

    const govSelect = document.getElementById("governorate-select");
    if (govSelect) {
        govSelect.addEventListener("change", loadMarkers);
    }

    loadMarkers();
})();

