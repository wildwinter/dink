# dink

**Very much work in progress!**

*Dink*, a contraction of *dialogue ink*, is a way of formatting dialogue lines while using/writing in Ink, and a set of tools for parsing and supporting that content.

Ink is a system of writing for text flow, so it's a bit of an odd idea to restrict it, really, using Ink for only a part of its potential. But there are a lot of us now using Ink for controlling the flow of spoken dialogue lines and scenes.

So this presents a markup for specifying dialogue lines and scene actions in an easy to write/read form, and tools to help integrate that into a project.

```text
=== MyScene
#dink
FRED (O.S.): It was a cold day in November...

Cut to a shot of a boat.

FRED (O.S.): (guilty) It wasn't me who sank the boat.
FRED (O.S.): It was Dave.

// ART: Remember Dave has a red hat.
Zoom in on Dave standing on deck.
DAVE: Thar she blows! 
-> DONE
```

## Summary

* The `DinkCompiler`:
  * Adds IDs to all the lines of Ink to identify them for localization and voice.
  * Compiles the Ink using *inklecate*.
  * If a list of character names is supplied, checks that all the Dink scenes use valid characters.
  * Produces a minimal JSON file giving metadata for each line that Ink won't provide (such as the speaker, directions etc.)
  * Produces a JSON file with all the strings used in Ink and Dink needed for runtime.
  * Optionally produces a JSON file detailing a more detailed Dink structure (scenes, blocks, snippets, showing runs of dialogue).
  * Optionally produces an Excel file with all the strings in for localization.
  * Optionally produces an Excel file for voice recording, including mapping to actors if supplied. Checks the **status of existing audio files** to figure out what has actually been recorded.
  * Optionally lets you track the writing status and recording status of each file and line, and produces an Excel file with all those stats. 
  * Allows you to control which comments end up in the localization and recording files.
* At runtime:
  * Load the compiled Ink story, as normal. (Remember, Dink compiled it for you!)
  * Load the Dink runtime data, which will give you extra information about each line of dialogue or action - the speaker, the direction etc.
  * Load the strings file that Dink generated, and use it to display the strings at runtime. (Because if you want you can swap it out for translations, instead of using the strings embedded in the Ink.)
* All these extra features are only for Knots and Stitches tagged as #dink. All your other Ink will work as usual (but you will get the localization and statuses for free!).
  
### Contents

* [The Basics](#the-basics)
* [Source Code](#source-code)
* [Releases](#releases)
* [Usage](#usage)
  * [The Dink Spec](#the-dink-spec)
  * [Character List](#character-list)
  * [Writing Status](#writing-status)
  * [Audio File Status](#audio-file-status)
  * [Comment and Tag Filtering](#comment-and-tag-filtering)
  * [Command-Line Tool](#command-line-tool)
  * [Config File](#config-file)
* [Contributors](#contributors)
* [License](#license)

## The Basics

The `DinkCompiler` will take in an Ink file (for example, `myproject.ink`) and its includes, process it, and the results are the following:

* **Updated Source File (`myproject.ink`)**: Any lines of text in the source Ink file that don't have a unique identifier of the form `#id:xxx` tags will have been added.
* **Compiled Ink File (`myproject.json`)**: The Ink file compiled to JSON using `inklecate`, as Ink usually does.
* **Dink Runtime File (`myproject-dink-min.json`)**: A JSON structure containing one entry for each LineID, with the runtime data you'll need for each line that you won't get from Ink e.g. the character speaking etc.
* **Strings Runtime File (`myproject-strings-min.json`)**: A JSON file containing an entry for every string in Ink, along with the string used in the original script. This is probably your master language file for runtime - you'll want to create copies of it for your localisation. When you display an Ink or Dink line you'll want to use the string data in here rather than in Ink itself.
* **Dink Structure File (`myproject-dink-structure.json`)**: (Optional) A JSON structure containing all the Dink scenes and their blocks, snippets, and beats, and useful information such as tags, lines, comments and so on. This is most likely to be useful in your edit pipeline for updating items in your editor based on Dink scripts - for example, creating placeholder scene layouts.
* **Stats File (`myproject-stats.xslx`)**: (Optional) An Excel file giving details of the status of every line in the game, both writing and recording.
* **Strings File for Localisation (`myproject-strings.xslx`)**: (Optional) An Excel file containing an entry for every string in Ink that needs localisation. When they are Dink lines, will include helpful data such as comments, the character speaking.
* **Recording Script File (`myproject-recording.xslx`)**: (Optional) An Excel file containing an entry for every line of dialogue that needs to be recorded, along with helpful comments and direction, and if you have provided a `characters.json` file, the Actor associated with the character.

## Source Code

The source can be found on [Github](https://github.com/wildwinter/dink), and is available under the MIT license.

## Releases

Releases will be available in the releases area in [Github](https://github.com/wildwinter/dink/releases).

## Usage

### The Dink Spec

A Dink **scene** is the equivalent of an Ink **knot**.

Each Dink scene consists of one or more Dink **blocks**. A Dink block is the equivalent of an ink **stitch**.

A scene might only contain one block, the "main" block, which is unnamed. Any further blocks will be named after the stitch.

Each Dink block consists of one or more Dink **snippets**. A Dink snippet is the equivalent of an Ink flow fragment - it is a run of lines that doesn't have any flow changes or diversions in it.

Each **snippet** consists of **beats**.

Each beat can either be a **line of dialogue**, or a **line of action**.

At a very simplistic level this can be interpreted as "X happens, then X happens".

```text
== MyScene
#dink

// Comment that applies to the following line
// Another comment that'll apply to the same line.
ACTOR (qualifier): (direction) Dialogue line. #tag1 #tag3 #tag4 #id:xxxxxx

// Comment will get carried over.
// LOC: This comment will go to the localisers
(Type) Line of action #tag1 #tag2 #id:xxxxxx // This comment too.
-> DONE
```

Comments, *qualifier* and *direction* are optional, as are the tags except *#id:* which must exist and be unique. The Dink compiler will generate these (based on the Ink Localiser tool I made a while back).

Here is a simple scene, with only one (anonymous) block:

```javascript
== MyScene
#dink
// VO: This comment will go to the voice actors
// This comment will go to everyone
DAVE (V.O.): It was a quiet morning in May... #id:intro_R6Sg

// Dave is working at the counter
DAVE: Morning. #id:intro_XC5r

// Fred has come in from the street.
FRED: Hello to you too!

(SFX) The door slams. #id:intro_yS6G // Make this loud!
-> DONE
```

Here is a scene with an anonymous block to start and then another:

```javascript
== MyOtherScene
#dink
// This is the anon block
FRED (V.O.): It was a cold day in December... #id:main_MyOtherScene_R6Sg
-> Part2

// This is the block called Part2
= Part2
// This is fred talking.
FRED: Good morning! #id:main_MyOtherScene_Part2_R6Sg
-> DONE
```

#### Comments

Comments use `//` to make them meaningful to Dink, but any content in block-style comments
(e.g. `/* */`) will be skipped, like in normal Ink.

Comments *above* a block (i.e. above the knot or the stitch) will appear in the comments for that block.

Comments above a beat will appear in the comments for that beat, and so will comments on the end of a beat.

```text
// This comment will appear in the comments for MyScene's main block
// And so will this comment.
== MyScene
#dink
// This comment will appear in the comments for this next beat
DAVE (V.O.): It was a quiet morning in May... #id:intro_R6Sg // And so will this comment.
-> DONE
```

See also [Comment and Tag Filtering](#comment-and-tag-filtering) to find out how you can control which comment gets output where!

### Character List

You can supply a `characters.json` file in the same folder as the main Ink file. If, so it should
be this format:

```jsonc
[
    {"ID":"FRED", "Actor":"Dave"},
    {"ID":"JIM", "Actor":""},
]
```

When the Dink scripts are parsed, the character name on a Dink line like:

```text
FRED (O.S): (hurriedly) Look out!
```

will be checked against that character list, and if it isn't present the process will fail.

The **Actors** column will be copied in to the recording script export, for ease of use with recording.

### Writing Status

*You don't need to use this, but it might be handy!*

This tool lets you mark each line of text, whether Dink or not, with a tag that
gives its writing status. And then lets you export an Excel file showing the status
of all the lines in the project.

You can define a list of statuses in your project file, whatever your project needs.
Each status type has:
* A tag which you'll put in your Ink file.
* A status label which will end up in the status Excel document.
* An optional colour which will end up in the status Excel documen (to easily see those unfinished lines!)
* Flags to say whether a line of this status should be included in recording or in localization.

If you don't specify any statuses in the project file, the system won't be used, and all your lines will be included in recording and localization.

#### The Status Tag
In Ink, you'll use a tag starting with `#ws:` - for example, `#ws:final`, `#ws:draft1` etc.
You can define these.

#### Defining Statuses

This list of statuses can be customised in the [Project Config File](#config-file).

Here's an example set of statuses. These enable you to use
`#ws:stub`, `#ws:draft1`, `#ws:draft2`, `#ws:final`:

```jsonc
"writingStatus": [
    {
        // Label to put in the Excel file
        "status": "Final", 
        // Tag to use e.g. #ws:final
        "wstag": "final", 
        // If true, export lines of this status to the recording script
        "record": true, 
        // If true, export lines of this status to the localization script
        "loc": true, 
        // A color to make the Excel file pretty!
        "color": "33FF33" 
    },
    {
        "status": "Second Draft",
        "wstag": "draft2",
        "color": "FFFF33"
    },
    {
        "status": "First Draft",
        "wstag": "draft1",
        "color": "FF8833"
    },
    {
        "status": "Stub",
        "wstag": "stub",
        "color": "FF3333"
    }
]
```

#### Applying Tags
You can put a tag on a line, as you might expect:
```textpp
FRED: Hello folks! #id:main_Script1_HG5x #ws:draft1
```

But it would be really annoying to have to do that on every line. So you can also apply a status at the top of a stitch, then it'll apply to every line in that stitch (unless you override it on an individual line). Similarly you can apply it to the knot containing the stitch or to the file itself!

```text 
//Myfile.ink
#ws:stub

== Scene1
FRED: This line will appear as a stub because it inherits from the top of the file.
-> Scene2

== Scene2
#ws:draft1
FRED: This line will appear as draft 1.
FRED: And so will this.
JIM: But this will appear as draft2. #ws:draft2
-> Stitch1

= Stitch1
#ws:final
JIM: Everything here will be final.
JIM: Apart from this. #ws:draft
JIM: But this will will be final.
-> Stitch2

= Stitch2
GEORGE: I'm still Draft1 because that's what my Knot says.
-> DONE

== Scene3
FRED: But I am back to being a stub!
JIM: Unlike me! #ws:final
```

### Audio File Status

*You don't need to use this, but it might be handy!*

This tool assumes that you want to store your audio dialogue files in folders somewhere in your game. And that you put them into different folders depending on the status of the audio line.

It assumes that you name your audio file after the LineID of the line.

So, if you have this line:

```text
DAVE: Morning. #id:intro_XC5r
```

Then you probably have a file named `intro_XC5r.wav` or something similar. Any file extension is fine, so long as the filename starts with the line ID. So `intro_XC5r_v1.mp3` is also fine.

By default, the recording export routine will look for something matching that file in the following order:

* `./Audio/Final`
* `./Audio/Recorded`
* `./Audio/Scratch`
* `./Audio/TTS`

And the first one it finds, it will set as the `AudioStatus` of the file in the output `-recording.xlsx` Excel voice recording file. Or `Unknown` if it can't find it at all.

(All these folders are searched for under your project folder if you have one, or your main Ink file folder otherwise.)

This list of folders and statuses can be customised in the [Project Config File](#config-file), like so:

```jsonc
    // This is the default where the game will look for
    // audio files that start with the ID names of the lines.
    // The folders (and their children) will be searched in this
    // order, so if a line is found in (say) the Audio/Recorded folder first, 
    // its status in the recording script will be set to Recorded.
    // If not found, the status will be set to Unknown.
    // Folders will be relative to the project file.
    "audioStatus":[
        {
            // Label to put in the Excel file
            "status": "Final",
            // If a file is in this folder, it gets treated as Final
            "folder": "Audio/Final",
            // A color to make the Excel file pretty!
            "color": "33FF33",
            // Means this status counts as recorded for figuring out actor requirements.
            "recorded": true
        },
        {
            "status": "Recorded",
            "folder": "Audio/Recorded",
            "color": "FFFF33",
            // Means this status counts as recorded for figuring out actor requirements.
            "recorded": true
        },
        {
            "status": "Scratch",
            "folder": "Audio/Scratch",
            "color": "FF8833"
        },
        {
            "status": "TTS",
            "folder": "Audio/TTS",
            "color": "FF3333"
        }
    ]
```

### Comment and Tag Filtering

*You don't need to use this, but it might be handy!*

When creating the voice recording script and the localization document, by default Dink
includes all comments (and all tags for the recording script). But you can tweak that in settings so that only specific comments and tags get
into those particular scripts.

e.g. if your script has something like:
```text
// This is the line about the blue mushroom.
// SFX: Make sure there's a blue mushroom sound here.
// VO: Remember this is at a distance from the enemy.
// LOC: This is a toadstool, genus todus stoolus.
FRED: It's big, and it's blue!
```
That's an awful lot of comments to end up everywhere.
You can set up your comment filter to use whatever prefixes suit your project.
The '?' option means "If a line has no prefix, include it."
By default, everything is included.

```jsonc
// Control which comments are seen on which script
// By default everything is passed.
"commentFilters": {
    // For localisation, include comments with no prefix, but also prefix LOC: and VO:
    "loc": ["?","LOC", "VO"],
    // For recording script, includes comments with no prefix, but also prefix VO:
    "record": ["?","VO"]
}
```

Similarly, if your script includes tags for the VO processing team:

```text
FRED: It's big, and it's blue! #vo:loud #vo:radio
```

Then this would make sure those tags are passed through. By default, NO tags are
passed to the recording script (or you'd be overwhelmed!)

```jsonc
// Control which tags are seen on which script
// By default nothing is passed.
"tagFilters": {
    // Currently only the recording script exports tags. Same rules as comment filters!
    // This passes tags such as #vo:loud or #vo:soft
    "record": ["vo"]
}
```

Comment and tag filters can be customised in the [Project Config File](#config-file).

### Command-Line Tool

This is a command-line utility with a few arguments. A few simple examples:

Use the file `main.ink` (and any included ink files) as the source, and output the resulting files in the `somewhere` folder:

`./DinkCompiler --source ../../tests/test1/main.ink --destFolder ../somewhere`

Or instead, grab all the settings from a project file:
`./DinkCompiler --project dinkproject.jsonc`

#### Arguments

* `--source <sourceInkFile>` (REQUIRED)

    Entrypoint to use for the Ink processing.\
    e.g. `--source some/folder/with/main.ink`

* `--destFolder <folder>`

    Folder to put all the output files.\
    e.g. `--destFolder gameInkFiles/`\
    Default is the current working dir.

* `--locActionBeats`

    If present, includes the text of action beats as something that
    needs to be localised by including it in `-strings` files.\
    If false, skips that text, but does include it in `-dink-min`.\
    Default is false.

* `--dinkStructure`

    If present, outputs the structured Dink JSON file (`*-dink-structure.json`).

* `--localization`

    If present, outputs the strings Excel file (`*-strings.xlsx`).

* `--recordingScript`

    If present, outputs the recording script Excel file (`*-recording.xlsx`).

* `--stats`

    If present, outputs a file of the status of all the lines as an Excel file (`*-stats.xlsx`).

* `--ignoreWritingStatus`

    If present, ignores the writing status when deciding what to include in the recording script
    or localization script. Useful for a full dump of lines.

* `--project project/config.jsonc`

    If supplied, configuration will be read from the given JSON file, instead
    of given as command-line switches. This also means that the folder that the
    supplied file is in will be treated as a potential source file for the Ink
    and for the characters.json if those aren't fully qualified paths.

### Config File

A JSON or JSONC file (i.e. JSON with comments) having all or some of the required options:

```jsonc
{
    // What's the source Ink file?
   "source":"main.ink",

    // Where's the folder to output everything?
    "destFolder":"../examples",

    // Localise actions?
    // Default is false, which means no text in Action beats
    // will be localised
    "locActionBeats":false,

    // Localise actions?
    // Default is false, which means no text in Action beats
    // will be localised
    "locActionBeats": false,
    
    // If true, outputs the structured dink file (json)
    "outputDinkStructure": false,
    
    // If true, outputs the strings file (xlsx)
    "outputLocalization": false,
    
    // If true, outputs the recording script file (xlsx)
    "outputRecordingScript": false,

    // Sometimes you want to output every single line in a recording or loc script
    // to see what you've got.
    "ignoreWritingStatus": false,

    // If true, outputs the stats file (xlsx)
    "outputStats": true,

    // This is the default where the game will look for
    // audio files that start with the ID names of the lines.
    // The folders (and their children) will be searched in this
    // order, so if a line is found in (say) the Audio/Recorded folder first, 
    // its status in the recording script will be set to Recorded.
    // If not found, the status will be set to Unknown.
    "audioStatus":[
        {
            "status": "Final",
            "folder": "Audio/Final",
            "color": "33FF33",
            // Means this status counts as recorded for figuring out actor requirements.
            "recorded": true
        },
        {
            "status": "Recorded",
            "folder": "Audio/Recorded",
            "color": "FFFF33",
            // Means this status counts as recorded for figuring out actor requirements.
            "recorded": true
        },
        {
            "status": "Scratch",
            "folder": "Audio/Scratch",
            "color": "FF8833"
        },
        {
            "status": "TTS",
            "folder": "Audio/TTS",
            "color": "FF3333"
        }
    ]

    // Writing status tags - OPTIONAL - can be written on an Ink line as #ws:someStatus
    // e.g. #ws:final or #ws:draft1
    // If defined here, the following rules kick in:
    // - If a file has a tag, everything in it defaults to that tag.
    // - If a knot has a writing tag that overrides the file tag.
    // - If a stitch has a writing tag that overrides the knot or file tag.
    // - If a line has a writing tag that overrides the stitch, knot or file tag.
    // - Only statuses with a record value of true will get sent to the recording script.
    // - Only statuses with a loc value of true will get sent to the localization strings.
    // - The writing status file will show all statuses.
    // If this section is not defined, no writing status tags are used and everything will be
    // sent to recording script and localization.
    // If a line has no status it will be treated as "Unknown".
    // The color is what ends up in the Excel file for a line of this status - it's optional.
    "writingStatus": [
        {
            "status": "Final",
            "wstag": "final",
            "record": true,
            "loc": true,
            "color": "33FF33"
        },
        {
            "status": "Second Draft",
            "wstag": "draft2",
            "color": "FFFF33"
        },
        {
            "status": "First Draft",
            "wstag": "draft1",
            "color": "FF8833"
        },
        {
            "status": "Stub",
            "wstag": "stub",
            "color": "FF3333"
        }
    ],

    // Control which comments are seen on which script
    "commentFilters": {
        // For localisation, include comments with no prefix, but also prefix LOC:
        "loc": ["?","LOC"],
        // For recording script, includes comments with no prefix, but also prefix VO:
        "record": ["?","VO"]
    },

    // Control which tags are seen on which script
    // By default nothing is passed.
    "tagFilters": {
        // Currently only the recording script exports tags. Same rules as comment filters!
        // This passes tags such as #vo:loud or #vo:soft
        "record": ["vo"]
    }
}
```

## Contributors

* [wildwinter](https://github.com/wildwinter) - original author

## License

```text
MIT License

Copyright (c) 2025 Ian Thomas

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
