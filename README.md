# dink
**dink** 

BLABHBABAB
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
The `Fountain.Parser` supplied in the tools will take this:
```
Something
```

## Source Code
The source can be found on [Github](https://github.com/wildwinter/dink), and is available under the MIT license.

## Releases
Releases are available in the releases area in [Github](https://github.com/wildwinter/dink/releases):
???

## The Dink Spec

Each Dink scene consists of a set of **beats**. Each beat can either be a line of dialogue, or a line of action.

At a very simplistic level this can be interpreted as "X happens, then X happens".

```
== MyScene
#dink

// Comment that applies to the following line
// Another comment that'll apply to the same line.
ACTOR (qualifier): (direction) Dialogue line. #tag1 #tag3 #tag4 #id:xxxxxx

// Comment will get carried over.
(Type) Line of action #tag1 #tag2 #id:xxxxxx // This comment too.
```

Comments, *qualifier* and *direction* are optional, as are the tags except *#id:* which must exist and be unique.

```
DAVE (V.O.): It was a quiet morning in May... #id:intro_R6Sg

// Dave is working at the counter
DAVE: Morning. #id:intro_XC5r

// Fred has come in from the street.
FRED: Hello to you too!

(SFX) The door slams. #id:intro_yS6G // Make this loud!
```


## Usage

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