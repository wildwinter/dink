# DinkVoiceExport

This was created to help out the audio team. You give it a spec of particular Dink lines, and it collects the corresponding WAV files and copies them to a destination.

For example:

```text
DinkVoiceExport --project dink.jsonc --tags vo:radio --audioStatus Recorded
```

This would copy any lines with the tag `#vo:radio` out of the `Recorded` folder and copy them somewhere else, so they can be processed as a batch.

```text
DinkVoiceExport --project dink.jsonc --character DJBRIAN --audioFolder myArchiveFolder/someFiles
```

This would copy any lines for the character `DJBRIAN` out
of a specific folder and copy them somewhere else, so they can be processed as a batch.

**IMPORTANT:** This utility relies on the Dink Structure
file (e.g. `myproject-dink.json`) having been
created and being up to date. This means you should have
recently run `DinkCompiler` and made sure `--dinkStructure` was on the command line or `outputDinkStructure` set in
the project file.

## Tool Arguments

* `--project <projectFile>` (REQUIRED)

    The project's config file.\
    e.g. `--project some/folder/with/dink.jsonc`

* `--destFolder <folder>` (REQUIRED)

    The place to copy the resultling files\
    e.g. `--destFolder some/output/folder`

* `--audioStatus <statusLabel>`

    Which status folder to use (see [Audio Status](#audio-file-status)). The tool will try and fine WAVs here.\
    If you don't specify this, you need to specify `--audioFolder`\
    e.g. `--audioStatus Recorded`

* `--audioFolder <aWavFolder>`

    Which folder should the tool look in for WAVs?\
    If you don't specify this, you need to specify `--audioStatus`\
    e.g. `--audioFolder /my/audio/files`

* `--character <characterID>`

    Which character name to look for.\
    You need to specify at least one of this, `--tags`, or `--scene`\
    e.g. `--character DAVE`

* `--scene <sceneID>`

    Which scene ID to look for (knot name).\
    You need to specify at least one of this, `--tags`, or `--character`\
    e.g. `--scene MyFirstScene`

* `--tags <tag1>,<tag2>,...`

    Which tags to match. These need to all be matched for a line to be selected. If the tag ends in `:` then all tags starting with `tag:` will pass the criteria.\
    You need to specify at least one of this, `--scene`, or `--character`\
    e.g. `--tags vo:` - all tags starting `#vo:`\
    `--tags vo:radio,vo:loud` - all loud radio lines\
    `--tags sfx` - all lines tagged `#sfx`