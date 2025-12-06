# dink Web Demo

This is a simple demo to show how to use the output of a Dink project in Javascript.

The principles are the same in other systems too.

## Authoring

`dink-src` contains the `.ink` files and the Dink project configuration.

Running DinkCompiler with these params:
```
DinkCompiler --project dink-src\dink.jsonc
```

will create Dink's runtime files in `dink-content`.

## Runtime
At runtime, `index.html` loads the Ink runtime (`ink.js`) and `main.js`, which is where we have the main behaviour for the game.

`main.js` loads the compiled Ink story (`main.json`) which is what you'd normally expect from an Ink game.

`main.js` also loads the Dink runtime files `main-dink.json` (Dink metadata) and `main-strings-en-GB.json` (the English strings for the game).

Then like in a normal Ink project, `main.js` repeatedly calls Ink's `story.Continue()` to get the next line of content.

**Here's where the Dink approach differs:**

Instead of using the text you get back from `story.Continue()`, you look for an Ink tag starting with the characters `id:`. The Dink compiler added one of these to every Ink line.

So if the line was:
```
FRED: (loudly) How is everyone? #id:part1_comedy_5XfA
```

Then at runtime you grab that ID tag from `story.currentTags`. This is what we use in the demo:
```javascript
function findID(tags) {
    for (const tag of tags) {
        if (tag.startsWith("id:")) {
            return tag.substring(3); // everything after "id:"
        }
    }
    return null; // not found
}
```

Now you have the ID, you get the Dink info from the loaded Dink metadata, and the text from the strings:

```javascript
// Assume this happened when the game initialised
var dinkStory = await loadJson('../dink-content/main-dink.json');
// Load the strings. The whole point here is that
// you could replace them with a different language.
var locStrings = await loadJson('../dink-content/main-strings-en-GB.json');
:
:
// During the Ink processing
while (story.canContinue)

    story.Continue();

    var id = findID(story.currentTags);
    if (id!=null)
    {
        // Grab the Dink metadata
        var dinkBeat = dinkStory[id];
        if (dinkBeat!=null && dinkBeat.Type=="Line")
        {
            // It's a Dink dialogue line!

            // The metadata has, for example, who is talking:
            var speaker = dinkInfo.CharacterID;
            
            // Grab the localised text
            var locString = locStrings[id];
        
            // Now do my cool animation to show the
            // speaker saying that line!
        }
        else 
        {
            // It wasn't a Dink dialogue line.
            // But we could at least use the nice localisation system,
            // since there is an ID
            var lineToPrint = locStrings[id];
        }
    }
```

And that's it.