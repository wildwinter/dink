export class Map {
    constructor(mapID, imgSrc) {
        this._locations = {};
        this._mapID = mapID;
        this._imgSrc = imgSrc;
    }

    // Format:
    // "town_hall", {left: "40%", top: "25%", title: "Town Hall 🏛️"}
    addLocation(id, data) {
        this._locations[id] = data;
    }

    getLocations() {
        return this._locations;
    }
};

export class MapManager {

    constructor(containerName, clickHandler) {
        this._mapContainer = document.querySelector(containerName);
        this._mapClickHandler = clickHandler;
        this._maps = {};
        this._currentMap = null;
    }
    
    _attachClickListener(element) {
        element.addEventListener('click', (e) => {
            e.stopPropagation();
            const id = element.getAttribute('data-location-id');
            const title = element.getAttribute('data-tooltip-title');
            this._mapClickHandler(id, title);
        });
    }

    _createSymbolHTML(id, data) {
        if (!data.title || !data.left || !data.top) {
            console.error("Missing required data for new symbol.", data);
            return null;
        }

        return `
            <div 
                class="map-hit-area" 
                id="map-${id}"
                style="left: ${data.left}; top: ${data.top}; display:none;"
                data-location-id="${id}"  
                data-tooltip-title="${data.title}"
                data-tooltip-description=""
            >
                <div class="map-symbol-visual"></div>
                <div class="map-tooltip">
                    <div class="tooltip-title" data-title="${data.title}"></div>
                    <div class="tooltip-description" data-description=""></div>
                </div>
            </div>
        `;
    }

    addMap(map) {
        this._maps[map._mapID] = map;
    }

    setMap(mapID) {
        const map = this._maps[mapID];
        if (!map) {
            console.error(`Map with ID '${mapID}' not found.`);
            return;
        }

        // Clear existing symbols
        this._mapContainer.innerHTML = 
            `<img src="${map._imgSrc}" alt="Map Image" class="map-image"/>`;

        // Add new symbols
        const locations = map.getLocations();
        for (const [id, data] of Object.entries(locations)) {
            this._addSymbol(id, data);
        }

        this._currentMap = map;
    }

    getCurrentMapName() {
        if (this._currentMap) {
            return this._currentMap._mapID;
        }
        return null;
    }

    _addSymbol(id, data) {
        const html = this._createSymbolHTML(id, data);
        if (!html) return;

        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = html.trim();
        const newArea = tempDiv.firstChild;
        
        this._mapContainer.appendChild(newArea);

        this._attachClickListener(newArea);
        
        return newArea;
    }
    
    _getSymbolElement(id) {
        const element = document.getElementById(`map-${id}`);
        if (!element) {
            console.warn(`Symbol with ID '${id}' not found.`);
        }
        return element;
    }

    showSymbol(id) {
        const element = this._getSymbolElement(id);
        if (element) {
            element.style.display = 'block'; 
        }
    }

    hideSymbol(id) {
        const element = this._getSymbolElement(id);
        if (element) {
            element.style.display = 'none';
        }
    }

    setSymbolDesc(id, desc) {
        const element = this._getSymbolElement(id);
        if (element) {
            const descriptionElement = element.querySelector('.tooltip-description');
            element.setAttribute('data-tooltip-description', desc);
            if (descriptionElement) {
                descriptionElement.setAttribute('data-description', desc);
            }
        }
    }

    iterateSymbols(callback) {

        const allSymbols = this._mapContainer.querySelectorAll('.map-hit-area');

        allSymbols.forEach(element => {
            const id = element.getAttribute('data-location-id');
            const title = element.getAttribute('data-tooltip-title');
            const isVisible = element.style.display !== 'none';
            
            callback(element, id, title, isVisible);
        });
    }

    // Stop the map being clickable
    lockMap() {
        this._mapContainer.classList.add('locked');
    }

    // Allow the map to be clickable again.
    unlockMap() {
        this._mapContainer.classList.remove('locked');
    }
};
