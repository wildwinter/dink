# DinkViewer

This tool outputs the Dink structure as an HTML page that is searchable and
viewable, showing clearly how the Dink is structured. It's typically more
readable than the Ink itself for non-programmers.

Everything is bundled into one HTML file so you can share it with people
via messages or email.

It's also printable in an easy-to-read format.

For example:

```text
DinkViewer --project dink.jsonc --destFolder some/folder
```

An HTML file named after your main Ink file will be created.

If you don't specify `--destFolder` then the file will be created in a temp folder.

The resulting HTML file will be automatically displayed in your system browser unless you pass `--silent`.

**IMPORTANT:** This utility relies on the Dink Structure
file (e.g. `myproject-dink.json`) having been
created and being up to date. This means you should have
recently run `DinkCompiler` and made sure `--dinkStructure` was on the command line or `outputDinkStructure` set in
the project file.

## Tool Arguments

* `--project <projectFile>` (REQUIRED)

    The project's config file.\
    e.g. `--project some/folder/with/dink.jsonc`

* `--destFolder <folder>`

    The place to copy the resulting HTML file\
    e.g. `--destFolder some/output/folder`

* `--silent`

    If supplied, don't open the system browser.

## Features

### Copying IDs

Click on the ID next to any given line to copy it to your clipboard.

### Finding by ID

Paste an ID from another file or an error report and hit 'Find ID' and the structure will unfold to where that ID is, if it's available.

### File and Line Number

Hovering over any element will show the Ink file and line it
came from.

### Printing

The print button (or your normal browser Print function) will output an expanded easy-readable version of the structure.
