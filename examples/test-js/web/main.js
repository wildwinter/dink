var storyRoot = document.querySelector('#story');

var storyContent = await loadJson('../dink-content/main.json');
var story = new inkjs.Story(storyContent);

var dinkStory = await loadJson('../dink-content/main-dink-min.json');
var locStrings = await loadJson('../dink-content/main-strings-min.json');

runInk();

async function loadJson(fileName) {
    try {

    const response = await fetch(fileName); 
    if (!response.ok) {
        throw new Error('HTTP error ' + response.status);
    }

    const json = await response.json();
    return json;

    } catch (err) {
        console.error('Failed to load JSON:', err);
    }
}

function scrollToBottom() {
    storyRoot.scrollTop = storyRoot.scrollHeight;
}

function findID(tags) {
    for (const tag of tags) {
        if (tag.startsWith("id:")) {
            return tag.substring(3); // everything after "id:"
        }
    }
    return null; // not found
}

function getDinkBeat(lineID) {
    return dinkStory[lineID] || null;
}

function getLocString(lineID) {
    return locStrings[lineID] || null;
}

function runInk() {

    console.log("Running Ink...");

    while (story.canContinue) {

        // Get ink to generate the next paragraph
        var nextLine = story.Continue();

        var outText = nextLine;

        var lineID = findID(story.currentTags);
        if (lineID!=null) {
            var dinkBeat = getDinkBeat(lineID);
            var locString = getLocString(lineID);
            if (dinkBeat!=null) {
                console.log(dinkBeat);
                if (dinkBeat.BeatType=="Line") {
                    outText = "<b>"+dinkBeat.CharacterID;
                    if (dinkBeat.Qualifier!="")
                        outText+=" <i>("+dinkBeat.Qualifier+")</i>";
                    outText+=":</b> ";
                    outText += locString;
                }
                else if (dinkBeat.BeatType=="Action") {
                    outText = "<i>(" + dinkBeat.Text + ")</i>";
                }
            }
        }

        // Create paragraph element
        var para = document.createElement('p');
        para.innerHTML = outText;
        storyRoot.appendChild(para);
    }

    if (story.currentChoices.length == 0) {
        // Ink complete

        var hr = document.createElement('hr');
        storyRoot.appendChild(hr);

        scrollToBottom();
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
    removeAllChildren(storyRoot);
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

