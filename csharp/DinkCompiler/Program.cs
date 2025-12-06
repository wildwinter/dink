using DinkCompiler;
using DinkTool;
using System.CommandLine;

RootCommand command = new("Compiler chain for Dink");

Option<string> projectOption = new("--project")
{
    Description = "A project setting json file to read the options from e.g. dink.jsonc.",
};
command.Options.Add(projectOption);

Option<string> sourceOption = new("--source")
{
    Description = "The root Ink file to use as the source for the compile."
};
command.Options.Add(sourceOption);

Option<string> destFolderOption = new("--destFolder")
{
    Description = "The destination folder to write out all the compiled files."
};
command.Options.Add(destFolderOption);

Option<bool> locActionBeatsOption = new("--locActionBeatText")
{
    Description = "If true, include action beat text in the string exports. If false, keeps it in Dink minimal export."
};
command.Options.Add(locActionBeatsOption);

Option<bool> dinkStructureOption = new("--dinkStructure")
{
    Description = "Output the structured Dink JSON file."
};
command.Options.Add(dinkStructureOption);

Option<bool> stringsOption = new("--localization")
{
    Description = "Output the strings Excel file."
};
command.Options.Add(stringsOption);

Option<bool> voiceOption = new("--recordingScript")
{
    Description = "Output the voice lines Excel file."
};
command.Options.Add(voiceOption);

Option<bool> ignoreWritingStatusOption = new("--ignoreWritingStatus")
{
    Description = "Ignore the writing status and output everything in the loc and recording files."
};
command.Options.Add(ignoreWritingStatusOption);

Option<bool> outputStatsOption = new("--stats")
{
    Description = "Output the stats - line counts, to be recorded etc. etc. as an Excel document."
};
command.Options.Add(outputStatsOption);

Option<bool> googleTTSOption = new("--tts")
{
    Description = "Generate text-to-speech."
};
command.Options.Add(googleTTSOption);

command.Validators.Add(result =>
{
    // Is a project file specified?
    var isProjectPresent = result.GetResult(projectOption) is not null;
    if (!isProjectPresent)
    {
        // Enforce some vars
        var isSourcePresent = result.GetResult(sourceOption) is not null;
        if (!isSourcePresent)
        {
            result.AddError("Either '--project' or '--source' must be specified.");
        }
    }
});

command.SetAction(parseResult =>
{
    ProjectSettings settings = new ProjectSettings();
    
    string? projectFile = parseResult.GetValue<string>(projectOption);
    if (projectFile!=null)
        settings = ProjectSettings.LoadFromProjectFile(projectFile)??settings;

    settings.Source = parseResult.GetValue<string>(sourceOption)??settings.Source;
    settings.DestFolder = parseResult.GetValue<string>(destFolderOption)??settings.DestFolder;
    if (parseResult.GetValue<bool>(locActionBeatsOption))
        settings.LocActionBeats = true;
    if (parseResult.GetValue<bool>(dinkStructureOption))
        settings.OutputDinkStructure = true;
    if (parseResult.GetValue<bool>(stringsOption))
        settings.OutputLocalization = true;
    if (parseResult.GetValue<bool>(voiceOption))
        settings.OutputRecordingScript = true;
    if (parseResult.GetValue<bool>(ignoreWritingStatusOption))
        settings.IgnoreWritingStatus = true;
    if (parseResult.GetValue<bool>(outputStatsOption))
        settings.OutputStats = true;
    if (parseResult.GetValue<bool>(googleTTSOption))
        settings.GoogleTTS.Generate = true;

    var compiler = new Compiler(settings);
    if (!compiler.Run()) {
        Console.Error.WriteLine("Not compiled.");
        return -1;
    }
    return 0;
});

ParseResult parseResult = command.Parse(args);
return parseResult.Invoke();