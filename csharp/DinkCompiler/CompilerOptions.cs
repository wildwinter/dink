namespace DinkCompiler;

using System.Text.Json;
using DocumentFormat.OpenXml.Bibliography;

public class CompilerOptions
{
    // Project file
    // Can be omitted
    public string ProjectFile = "";

    // Source ink file.
    public string Source = "";

    // Folder to output compiled assets to.
    public string DestFolder = "";

    // If false (default), assumes that ACTION Beats shouldn't
    // get their text localised, and so will not be in the string tables
    // but will be in the Dink minimal. 
    // If true, includes the text of action beats in the string tables
    // to be localised, and not in the Dink minimal
    public bool LocActionBeats = false;

    public static CompilerOptions? LoadFromProjectFile(string projectFile)
    {
        if (!File.Exists(projectFile))
        {
            Console.Error.WriteLine($"Project file '{projectFile}' not found.");
            return null;
        }

        string json = File.ReadAllText(projectFile);
        var jsonOptions = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
            UnknownTypeHandling = System.Text.Json.Serialization.JsonUnknownTypeHandling.JsonElement
        };

        CompilerOptions? result = JsonSerializer.Deserialize<CompilerOptions>(json, jsonOptions);
        if (result==null)
        {
            Console.Error.WriteLine($"Couldn't load project file '{projectFile}'.");
            return null;
        }
        Console.WriteLine($"Loading project settings from: '{projectFile}'");
        result.ProjectFile = projectFile;
        return result;
    }

}

public class CompilerEnvironment
{
    private CompilerOptions _options;
    public string SourceInkFile {get; private set;}
    public string SourceInkFolder {
        get {
            return Path.GetDirectoryName(SourceInkFile)??"";
        }
    }
    public string ProjectFile {get; private set;}
    public string ProjectFolder {
        get {
            return Path.GetDirectoryName(ProjectFile)??"";
        }
    }
    public string DestFolder {get; private set;}
    public bool LocActionBeats {get{return _options.LocActionBeats;}}
    public string RootFilename {get{return Path.GetFileNameWithoutExtension(SourceInkFile);}}

    public CompilerEnvironment(CompilerOptions options)
    {
        _options = options;    
        SourceInkFile = "";
        ProjectFile = "";
        DestFolder = "";
    }

    public bool Init()
    {
        ProjectFile = _options.ProjectFile;
        if (!string.IsNullOrWhiteSpace(ProjectFile))
        {
            if (!Path.IsPathFullyQualified(ProjectFile))
            {
                ProjectFile = Path.GetFullPath(ProjectFile);
            }
        }

        SourceInkFile = _options.Source;
        if (string.IsNullOrEmpty(SourceInkFile))
        {
            Console.Error.WriteLine("No Ink file specified.");
            return false;
        }
        if (!Path.IsPathFullyQualified(SourceInkFile))
        {
            Console.WriteLine($"{ProjectFolder}:{SourceInkFile}");
            if (File.Exists(Path.Combine(ProjectFolder, SourceInkFile)))
            {
                SourceInkFile = Path.Combine(ProjectFolder, SourceInkFile);
            }
            else if (File.Exists(Path.Combine(SourceInkFile)))
            {
                SourceInkFile = Path.Combine(SourceInkFile);
            }
        }
        if (!File.Exists(SourceInkFile))
        {
            Console.Error.WriteLine($"Source Ink file doesn't exist:'{SourceInkFile}'");
            return false;
        }
        Console.WriteLine($"Using source ink file: '{SourceInkFile}'");

        DestFolder = _options.DestFolder;
        if (String.IsNullOrWhiteSpace(DestFolder))
            DestFolder = Environment.CurrentDirectory;
        DestFolder = Path.GetFullPath(DestFolder);
        if (!Directory.Exists(DestFolder))
            Directory.CreateDirectory(DestFolder);
        Console.WriteLine($"Using destination folder: '{DestFolder}'");

        if (LocActionBeats)
            Console.WriteLine($"Including action beat text in localization output.");

        return true;
    }

    // If it's a relative file, first looks in the same folder as the main Ink file,
    // then looks in the same folder as the project (if there is one),
    // then looks in the CWD
    public string? FindFileInSource(string fileName)
    {
        if (Path.IsPathFullyQualified(fileName))
            return fileName;
        string filePath = Path.Combine(SourceInkFolder, fileName);
        if (File.Exists(filePath))
            return filePath;
        if (!string.IsNullOrEmpty(ProjectFile)) {
            filePath = Path.Combine(ProjectFolder, fileName);
            if (File.Exists(filePath))
                return filePath;
        }
        filePath = Path.GetFullPath(fileName);
        if (File.Exists(filePath))
            return filePath;
        return null;
    }

    public string MakeDestFile(string suffix)
    {
        return Path.Combine(DestFolder, RootFilename + suffix);
    }
}