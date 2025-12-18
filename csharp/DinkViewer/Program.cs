using DinkTool;
using DinkViewer;
using System.CommandLine;

RootCommand command = new("A viewer for dink story files.");

Option<string> projectOption = new("--project")
{
    Description = "A project setting json file to read the options from e.g. dink.jsonc.",
};
command.Options.Add(projectOption);

Option<string> sourceOption = new("--source")
{
    Description = "The entrypoint Ink file to use if --project doesn't specify it."
};
command.Options.Add(sourceOption);

Option<string> destFolderOption = new("--destFolder")
{
    Description = "The destination folder to write out the viewable file."
};
command.Options.Add(destFolderOption);

Option<bool> silentOption = new("--silent")
{
    Description = "If specified, will skip opening the html file in a browser."
};
command.Options.Add(silentOption);

command.Validators.Add(result =>
{
    // Is a project file specified?
    var isProjectPresent = result.GetResult(projectOption) is not null;
    if (!isProjectPresent)
        result.AddError("'--project' must be specified.");
});

command.SetAction(parseResult =>
{
    ProjectSettings projectSettings = new ProjectSettings();
    
    string? projectFile = parseResult.GetValue<string>(projectOption);
    if (projectFile!=null)
        projectSettings = ProjectSettings.LoadFromProjectFile(projectFile)??projectSettings;

    if (string.IsNullOrWhiteSpace(projectSettings.Source))
    {
        var sourceFile = parseResult.GetValue<string>(sourceOption);
        if (sourceFile==null)
        {
            Console.Error.WriteLine("No source file specified in project or as an argument.");
            return -1;
        }

        projectSettings.Source = sourceFile;
    }

    ViewerSettings viewerSettings = new ViewerSettings();
    viewerSettings.DestFolder = parseResult.GetValue<string>(destFolderOption)??"";
    viewerSettings.Silent = parseResult.GetValue<bool>(silentOption);

    ProjectEnvironment env = new ProjectEnvironment(projectSettings);
    if (!env.Init()||!viewerSettings.Init())
        return -1;

    var viewer = new Viewer(env, viewerSettings);
    if (!viewer.Run()) {
        Console.Error.WriteLine("Not exported.");
        return -1;
    }
    return 0;
});

ParseResult parseResult = command.Parse(args);
return parseResult.Invoke();