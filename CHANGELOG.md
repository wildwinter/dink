# Changelog

All notable changes to Dink will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Entries before 0.1.8 are not documented here - see the [git history](https://github.com/wildwinter/dink/commits/main) for details.

## [Unreleased]

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
