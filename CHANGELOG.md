# Changelog

All notable changes to Dink will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Entries before 0.1.8 are not documented here - see the [git history](https://github.com/wildwinter/dink/commits/main) for details.

## [Unreleased]

### Added
- Google TTS settings can be overridden from the command line. `--noTts` turns generation off for a run even if the project file enables it (and wins over `--tts`); `--ttsAuth`, `--ttsOutputFolder`, `--ttsReplaceExisting` and `--ttsSkipUnchanged` override the other `googleTTS` values.

## [0.2.1]
- Forgot to add the Unreal changes.
- Update simple-vc-lib.

## [0.2.0]

### Added
- Structured scene JSON (`-dink-structure.json`) now includes a `Characters` list on each scene: the distinct speaking CharacterIDs across the knot and all its stitches, in order of first appearance. The minimal runtime file is unchanged.
