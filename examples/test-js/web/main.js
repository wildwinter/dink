import { Storylets } from "../../../engine/storylets.js"
import { Map, MapManager } from "./map.js"

// Set up map first
// -----------------------

const handleSymbolClick = (id, title) => {
    //console.log(`Symbol clicked: ${title} (ID: ${id})`);
    
    const storylet = storylets.GetAvailableStoryletsWithTag("loc", id)[0];
    chooseStorylet(storylet);
};

var mainMap = new Map("main", "../images/town-map.png");
mainMap.addLocation("town_hall", {left: "40%", top: "25%", title: "Town Hall 🏛️"});
mainMap.addLocation("library", {left: "77%", top: "37%", title: "The Library 📚"});
mainMap.addLocation("east", {left: "71.5%", top: "85%", title: "East House 🏠"});
mainMap.addLocation("bar", {left: "22%", top: "62%", title: "Frog & Horses 🍺"});
mainMap.addLocation("cave", {left: "56%", top: "35%", title: "A Cave 🌊"});

var caveMap = new Map("cave", "../images/cave-map.png");
caveMap.addLocation("exit", {left: "40%", top: "95%", title: "Exit 🚪"});
caveMap.addLocation("well", {left: "68%", top: "59%", title: "The Well 💧"});

var mapManager = new MapManager("#map-container", handleSymbolClick);
mapManager.addMap(mainMap);
mapManager.addMap(caveMap);

mapManager.setMap("main");

// -----------------------
var storyRoot = document.querySelector('#story');

// Load Ink story.
var story = new inkjs.Story(storyContent);

// Bind the map handlers to my Ink external functions
story.BindExternalFunction("set_map", function(mapName) {
    mapManager.setMap(mapName);
});

story.BindExternalFunction("get_map", function() {
    return mapManager.getCurrentMapName();
});

// Set up Storylets
var storylets = new Storylets(story);

// Do this when the storylet availability check is completed
storylets.onUpdated = onStoryletsUpdated;

// Add a reset button
const resetButtonContainer = document.getElementById('reset-container');
const resetButton = document.createElement('button');
resetButton.textContent = "Reset Story";
resetButton.addEventListener('click', reset);
resetButtonContainer.appendChild(resetButton);

// Kick off storylet processing which will take at least a frame.
updateStorylets();

function updateStorylets() {
    storylets.StartUpdate();
}

function scrollToBottom() {
    storyRoot.scrollTop = storyRoot.scrollHeight;
}

function onStoryletsUpdated() {

    mapManager.iterateSymbols(function(symbolElement, locationId) {
        // Show or hide symbol based on storylet availability
        const available = storylets.GetFirstAvailableStoryletWithTag("loc", locationId);
        if (available) {
            mapManager.setSymbolDesc(locationId, storylets.getStoryletTag(available, "desc", ""));
            mapManager.showSymbol(locationId);
        } else {
            mapManager.hideSymbol(locationId);
        }
    });

    if (storylets.GetAvailable().length == 0) {
        alert("Story complete! Close this to reset.");
        reset();
        return;
    }
}

function chooseStorylet(storyletName) {
    mapManager.lockMap();
    storylets.ChooseStorylet(storyletName);
    
    var para = document.createElement('h3');
    para.innerHTML = storylets.getStoryletTag(storyletName, "desc", "--");
    storyRoot.appendChild(para);

    runInk();
}

function runInk() {

    while (story.canContinue) {

        // Get ink to generate the next paragraph
        var paragraphText = story.Continue();

        // Create paragraph element
        var para = document.createElement('p');
        para.innerHTML = paragraphText;
        storyRoot.appendChild(para);
    }

    if (story.currentChoices.length == 0) {
        // Ink complete

        var hr = document.createElement('hr');
        storyRoot.appendChild(hr);

        scrollToBottom();

        mapManager.unlockMap();

        updateStorylets();
        return;
    }

    var ul = document.createElement('ul');
    ul.classList.add("choices");

    // Create HTML choices from ink choices
    story.currentChoices.forEach(function (choice) {

        var para = document.createElement('li');
        para.classList.add("choice");
        para.innerHTML = `<a href='#'>${choice.text}</a>`
        ul.appendChild(para);

        // Click on choice
        var paraAnchor = para.querySelectorAll("a")[0];
        paraAnchor.addEventListener("click", function (event) {

            event.preventDefault();

            chooseChoice(choice.index);
        });
    });

    storyRoot.appendChild(ul);

    scrollToBottom();
}

function chooseChoice(index) {
    story.ChooseChoiceIndex(index);
    removeAllChildrenWith(storyRoot, ".choices");
    runInk();
}

function reset() {
    story.ResetState();
    storylets.Reset();

    mapManager.unlockMap();

    removeAllChildren(storyRoot);

    updateStorylets();
}

function removeAllChildrenWith(el, selector) {
    var elements = el.querySelectorAll(selector);
    for (var i = 0; i < elements.length; i++) {
        var child = elements[i];
        child.parentNode.removeChild(child);
    }
}

function removeAllChildren(el) {
    while (el.firstChild) {
        el.removeChild(el.firstChild);
    }
}