using DinkTool;
using DinkVoiceExport;
using System.CommandLine;

RootCommand command = new("Voice export for Dink.");

Option<string> projectOption = new("--project")
{
    Description = "A project setting json file to read the options from e.g. dink.jsonc.",
};
command.Options.Add(projectOption);

Option<string> audioStatusOption = new("--audioStatus")
{
    Description = "The audioStatus to collect WAVs from (the folder specified in the Project Settings). Specify this or --audioFolder"
};
command.Options.Add(audioStatusOption);

Option<string> audioFolderOption = new("--audioFolder")
{
    Description = "The folder to collect WAVs from. Specify this or --audioStatus"
};
command.Options.Add(audioFolderOption);

Option<string> destFolderOption = new("--destFolder")
{
    Description = "The destination folder to write out all the exported WAV files."
};
command.Options.Add(destFolderOption);

Option<string> tagsOption = new ("--tags")
{
    Description = "One or more comma-separated tags to look for. If a tag ends with a : symbol, then this'll treat it like a prefix. "+
        "e.g. --tags vo:loud,a:,radioeffect will include any line with a tag starting with #a:"
};
command.Options.Add(tagsOption);

Option<string> characterOption = new ("--character")
{
    Description = "A specific character to look for."
};
command.Options.Add(characterOption);

Option<string> sceneOption = new ("--scene")
{
    Description = "A specific scene to look for."
};
command.Options.Add(sceneOption);

command.Validators.Add(result =>
{
    // Is a project file specified?
    var isProjectPresent = result.GetResult(projectOption) is not null;
    if (!isProjectPresent)
        result.AddError("'--project' must be specified.");

    if (result.GetResult(audioFolderOption)==null &&
        result.GetResult(audioStatusOption)==null)
        result.AddError("--audioStatus or --audioFolder must be specified.");

    var isDestPresent = result.GetResult(destFolderOption) is not null;
    if (!isDestPresent)
        result.AddError("'--destFolder' must be specified.");

    if (result.GetResult(tagsOption)==null &&
        result.GetResult(characterOption)==null &&
        result.GetResult(sceneOption)==null)
    {
        result.AddError("One of --tags, --character, or --scene must be specified.");
    }
});

command.SetAction(parseResult =>
{
    ProjectSettings projectSettings = new ProjectSettings();
    
    string? projectFile = parseResult.GetValue<string>(projectOption);
    if (projectFile!=null)
        projectSettings = ProjectSettings.LoadFromProjectFile(projectFile)??projectSettings;

    ExportSettings exportSettings = new ExportSettings();
    exportSettings.DestFolder = parseResult.GetValue<string>(destFolderOption)??projectSettings.DestFolder;
    string? tags = parseResult.GetValue<string>(tagsOption);
    if (tags!=null)
    {
        exportSettings.Tags = tags.Split(",").ToList();
    }
    exportSettings.Character = parseResult.GetValue<string>(characterOption)??"";
    exportSettings.Scene = parseResult.GetValue<string>(sceneOption)??"";
    exportSettings.AudioFolder = parseResult.GetValue<string>(audioFolderOption)??"";
    exportSettings.AudioStatus = parseResult.GetValue<string>(audioStatusOption)??"";

    var exporter = new VoiceExport(projectSettings, exportSettings);
    if (!exporter.Run()) {
        Console.Error.WriteLine("Not exported.");
        return -1;
    }
    return 0;
});

ParseResult parseResult = command.Parse(args);
return parseResult.Invoke();