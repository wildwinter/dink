# dink

**Very much work in progress!**

*Dink*, a contraction of *dialogue ink*, is a way of formatting dialogue lines while using/writing in Ink, and a set of tools for parsing and supporting that content.

Ink is a system of writing for text flow, so it's a bit of an odd idea to restrict it, really, using Ink for only a part of its potential. But there are a lot of us now using Ink for controlling the flow of spoken dialogue lines and scenes.

So this presents a markup for specifying dialogue lines and scene actions in an easy to write/read form, and tools to help integrate that into a project.

```c
=== MyScene
#dink
FRED (O.S.): It was a cold day in November...

Cut to a shot of a boat.

FRED (O.S.): (guilty) It wasn't me who sank the boat.
FRED (O.S.): It was Dave.

// Art: Remember Dave has a red hat.
Zoom in on Dave standing on deck.
DAVE: Thar she blows! 
-> DONE
```

### Tools
* The `DinkCompiler`:
  * Adds IDs to all the lines of Ink for localisation and voice.
  * Compiles the Ink using *inklecate*.
  * Parses any Dink scenes and produces a JSON file detailing that structure (e.g. who is the speaker for each line? What are the directions?). 
  * Also produces a minimal JSON file giving metadata for each line that Ink won't provide.
  * If a list of character names is supplied, checks that all the Dink scenes use valid characters.
  * Produces a JSON file with all the strings used in Ink and Dink needed for runtime.
  * Produces an Excel file with all the strings in for localisation.
  * Produces an Excel file for voice recording, including mapping to actors if supplied.
  
### Contents
* [The Basics](#the-basics)
* [Source Code](#source-code)
* [Releases](#releases)
* [Usage](#usage)
* [Contributors](#contributors)
* [License](#license)

## The Basics
The `DinkCompiler` will take in an Ink file (for example, `myproject.ink`) and its includes, process it, and the results are the following:
* **Updated Source File (`myproject.ink`)**: Any lines of text in the source Ink file that don't have a unique identifier of the form `#id:xxx` tags will have been added. 
* **Compiled Ink File (`myproject.json`)**: The Ink file compiled to JSON using `inklecate`, as Ink usually does. 
* **Dink Structure File (`myproject-dink-structure.json`)**: A JSON structure containing all the Dink scenes and their snippets and beats, and useful information such as tags, lines, comments and so on. This is most likely to be useful in your edit pipeline for updating items in your editor based on Dink scripts - for example, creating placeholder scene layouts.
* **Dink Runtime File (`myproject-dink-min.json`)**: A JSON structure containing one entry for each LineID, with the runtime data you'll need for each line that you won't get from Ink e.g. the character speaking etc.
* **Strings File for Localisation (`myproject-strings.xslx`)**: An Excel file containing an entry for every string in Ink that needs localisation. When they are Dink lines, will include helpful data such as comments, the character speaking.
* **Strings Runtime File (`myproject-strings-min.json`)**: A JSON file containing an entry for every string in Ink, along with the string used in the original script. This is probably your master language file for runtime - you'll want to create copies of it for your localisation. When you display an Ink or Dink line you'll want to use the string data in here rather than in Ink itself.
* **Voice Script File for Recording (`myproject-voice.xslx`)**: An Excel file containing an entry for every line of dialogue that needs to be recorded, along with helpful comments and direction, and if you have provided a `characters.json` file, the Actor associated with the character.

## Source Code
The source can be found on [Github](https://github.com/wildwinter/dink), and is available under the MIT license.

## Releases
Releases will be available in the releases area in [Github](https://github.com/wildwinter/dink/releases).

## The Dink Spec

A Dink **scene** is the equivalent of an Ink **knot**. 

Each Dink scene consists of one or more Dink **snippets**. A Dink snippet is the equivalent of an ink **stitch**.

A scene might only contain one, the "main" snippet, which is unnamed. Any further snippets will be named after the stitch.

Each **snippet** consists of **beats**.

Each beat can either be a **line of dialogue**, or a **line of action**.

At a very simplistic level this can be interpreted as "X happens, then X happens".

```c
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

Here is a simple scene, with only one (anonymous) snippet:
```c
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

Here is a scene with an anonymous snippet to start and then another:
```c
== MyOtherScene
#dink
// This is the anon snippet
FRED (V.O.): It was a cold day in December... #id:main_MyOtherScene_R6Sg
-> Part2

// This is the snippet called Part2
= Part2
// This is fred talking.
FRED: Good morning! #id:main_MyOtherScene_Part2_R6Sg
-> DONE
```

### Comments
Comments use `//` to make them meaningful to Dink, but any content in block-style comments
(e.g. `/* */`) will be skipped, like in normal Ink.

Comments *above* a snippet (i.e. above the knot or the stitch) will appear in the comments for that snippet.

Comments above a beat will appear in the comments for that beat, and so will comments on the end of a beat.

```c
// This comment will appear in the comments for MyScene's main snippet
// And so will this comment.
== MyScene
#dink
// This comment will appear in the comments for this next beat
DAVE (V.O.): It was a quiet morning in May... #id:intro_R6Sg // And so will this comment.
-> DONE
```

### Characters List
You can supply a `characters.json` file in the same folder as the main Ink file. If, so it should
be this format:

```json
[
    {"ID":"FRED", "Actor":"Dave"},
    {"ID":"JIM", "Actor":""},
]
```
When the Dink scripts are parsed, the character name on a Dink line like:
```c
FRED (O.S): (hurriedly) Look out!
```
Will be checked against that characters list, and if it isn't present the process will fail.

The Actors column will be copied in to the voice script export, for ease of use with recording.

## Usage
## Command-Line Tool
This is a command-line utility with a few arguments. A few simple examples:

Use the file `main.ink` (and any included ink files) as the source, and output the resulting files in the `somewhere` folder:

`./DinkCompiler --source ../../tests/test1/main.ink --destFolder ../somewhere`

### Arguments
* `--source <sourceInkFile>` (REQUIRED)
    
    Entrypoint to use for the Ink processing.
    e.g. `--source some/folder/with/main.ink`

* `--destFolder <folder>`
    
    Folder to put all the output files. 
    e.g. `--destFolder gameInkFiles/` 
    Default is the current working dir.

## Contributors
* [wildwinter](https://github.com/wildwinter) - original author

## License
```
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