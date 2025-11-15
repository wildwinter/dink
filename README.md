# dink
**dink** 

BLABHBABAB WORK IN PROGRESS
```
```
### Contents
* [The Basics](#the-basics)
* [Source Code](#source-code)
* [Releases](#releases)
* [Usage](#usage)
    * [Overview](#overview)
* [Contributors](#contributors)
* [License](#license)

## The Basics
The `DinkCompiler` will take in an Ink file (for example, `myproject.ink`) and its includes, process it, and the results are the following:
* Any lines of text in the source Ink file that don't have a unique identifier of the form `#id:xxx` tags will have been added. (Resulting in an updated `myproject.ink`.)
* The Ink file will have been compiled to JSON as Ink usually does. (Resulting in `myproject.json`.)
* Any sections of the Ink that follow the **[Dink format](#the-dink-spec)** will have been scanned, and the resulting Dink structures including comments are output to JSON. (Resulting in `myproject-dink.json`.)
* Text strings are exported (after adjusting for Dink) to JSON in a form that can be easily interpreted. Includes line text, comments, and if it's a voice line will include the speaker and any performance direction. (Resulting in `myproject-strings.json`.) They are also exported to an Excel file. (Resulting in `myproject-strings.xslx`)
* Voice lines parsed by Dink are exported to an Excel file containing voice-specific coments and tags. (Resulting in `myproject-voice.xslx`.)

## Source Code
The source can be found on [Github](https://github.com/wildwinter/dink), and is available under the MIT license.

## Releases
Releases are available in the releases area in [Github](https://github.com/wildwinter/dink/releases):
???

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


## Usage
## Command-Line Tool
This is a command-line utility with a few arguments. A few simple examples:

Use the file `main.ink` (and any included ink files) as the source, and output the resulting files in the `somewhere` folder:

`./DinkCompiler --source=../../tests/test1/main.ink --destFolder=../somewhere`

### Arguments
* `--source=<sourceInkFile>`
    
    Entrypoint to use for the Ink processing.
    e.g. `--source=some/folder/with/main.ink`

* `--destFolder=<folder>`
    
    Root folder to scan for Ink files to localise relative to working dir. 
    e.g. `--destFolder=gameInkFiles/` 
    Default is the current working dir.

### Overview
* xxx

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