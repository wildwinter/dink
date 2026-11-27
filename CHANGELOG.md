# Changelog

All notable changes to Dink will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Entries before 0.1.8 are not documented here - see the [git history](https://github.com/wildwinter/dink/commits/main) for details.

## [Unreleased]

### Fixed
- A build no longer fails with a false "two chunks of text" error when the last line of one included file and the first line of the next sit on the same line number. In lenient mode the same case left the second line without an ID. Fixed in the bundled localiser, which moves to 0.0.4.14.
- A knot or stitch tag on its own line (such as `#ws:draft1` or `#type:conversation`) is no longer lost when a `//` comment line comes before it. The comment counted as the start of the knot's content, so the tag that followed was silently dropped, and the knot's lines fell back to the file's writing status.
- A tag on a line of its own that comes after a knot or stitch has started now prints a warning naming the file and line, rather than being ignored without a word. Such tags only count at the top of a file, knot or stitch.
- A build now fails if one line ID is used on more than one line, and lists every file and line that uses it. This happens when a line is copied along with its `#id:` tag. Before, the build went through, and only one of those lines made it into the recording script, the localisation files and the runtime strings: the others were silently dropped, and the line that survived took the row of the first one, so the recording script looked out of order. To fix it, remove the `#id:` tag from every copy except the original and build again. Dinky does this for you when you save.

## [0.3.6]

### Changed
- Changing a character's `TTSVoice` in characters.json now regenerates that character's Google TTS lines on the next build. Each generated WAV now records a hash of the voice request (voice, language, encoding and sample rate) alongside the existing text hash, and a clip is remade when either differs. Previously only a change of text did this, and the only way to pick up a new voice was `replaceExisting`, which remakes every line in the project.
- Clips made by earlier versions carry no voice hash, and are treated as up to date until their text changes, so upgrading doesn't regenerate (and bill) a whole project at once. To catch voices you changed before upgrading, run one build with `replaceExisting` on. Scratch recordings made in Dinky are likewise left alone when a voice changes.

## [0.3.5]

### Changed
- Ink is now compiled with "count all visits" switched on, the same as `inklecate -c`. Visit counts are recorded for every knot and stitch rather than only those the script itself reads, so game code can ask for the visit count of any of them at runtime.

## [0.3.4]

### Fixed
- PDF export no longer fails with "'Rectangle' must not be null here" on larger scripts. The block and group separators were empty paragraphs carrying a bottom border, and MigraDoc cannot format one of those when it lands on a page boundary, so whether a script exported depended on where its pages happened to break. Page layout is unchanged.

## [0.3.3]

### Fixed
- Updated simple-vc-lib to 0.5.2, which fixes Plastic SCM status reporting: `cm status` output is now parsed in the format `cm` actually prints.

## [0.3.2]

### Fixed
- Updated simple-vc-lib to 0.5.1, which fixes Plastic SCM: a file that is already checked out now saves again without needing a second checkout.

## [0.3.1]

### Fixed
- Snippets with identical text in the same block no longer share a `SnippetID`. IDs are still derived from the snippet's text, so snippets that don't collide keep the ID they already had, but a collision (for example four identical `*crying*` barks in one shuffle) is salted and rehashed until it is unique. A structure file that already contains duplicates is healed on the next build.

## [0.3.0]

### Added
- Command-line overrides for the project file's Google TTS (`googleTTS`) settings, applied for a single run:
  - `--noTts`: don't generate TTS, even if the project file enables it. It wins over `--tts`, so a machine without the Google key can still build the project.
  - `--ttsAuth <file>`: use a different Google TTS authentication (JSON key) file.
  - `--ttsOutputFolder <folder>`: write generated TTS audio to a different folder.
  - `--ttsReplaceExisting`: regenerate every line, even where the existing audio is up to date.
  - `--ttsSkipUnchanged`: only regenerate lines whose text has changed. It wins over `--ttsReplaceExisting`.

## [0.2.1]
- Forgot to add the Unreal changes.
- Update simple-vc-lib.

## [0.2.0]

### Added
- Structured scene JSON (`-dink-structure.json`) now includes a `Characters` list on each scene: the distinct speaking CharacterIDs across the knot and all its stitches, in order of first appearance. The minimal runtime file is unchanged.
