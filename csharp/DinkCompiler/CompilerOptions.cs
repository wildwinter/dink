namespace DinkCompiler;

using System.Text.Json;

public class AudioFolder
{
    public required string Status { get; set; }
    public required string Folder { get; set; }
}

public class WritingStatusDefinition
{
    public string Status { get; set; } = "Unknown";
    public string WsTag { get; set; } = "";
    public bool Record { get; set; } = false;
    public bool Loc { get; set; } = false;
    public string Color {get; set;} = "";
}

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

    // If true, outputs the structured dink file (json)
    public bool OutputDinkStructure = false;

    // If true, outputs the strings file (xlsx)
    public bool OutputLocalization = false;

    // If true, outputs the voice file (xlsx)
    public bool OutputRecordingScript = false;

    // If true, outputs the writing status file (xlsx)
    public bool OutputWritingStatus = false;

    // This is the default where the game will look for
    // audio files that start with the ID names of the lines.
    // The folders (and their children) will be searched in this
    // order, so if a line is found in (say) the Audio/Recorded folder first, 
    // its status in the voice script will be set to Recorded.
    // If not found, the status will be set to Unknown.
    public List<AudioFolder> AudioFolders { get; set; } = new()
    {
        new AudioFolder { Status = "Final",    Folder = "Audio/Final" },
        new AudioFolder { Status = "Recorded", Folder = "Audio/Recorded" },
        new AudioFolder { Status = "Scratch",  Folder = "Audio/Scratch" },
        new AudioFolder { Status = "TTS",      Folder = "Audio/TTS" }
    };

    // Writing status tags - can be written on an Ink line as #ws:someStatus
    // e.g. #ws:final or #ws:draft1
    // If defined here, the following rules kick in:
    // - If a file has a tag, everything in it defaults to that tag.
    // - If a knot has a writing tag that overrides the file tag.
    // - If a stitch has a writing tag that overrides the knot or file tag.
    // - If a line has a writing tag that overrides the stitch, knot or file tag.
    // - Only statuses with a record value of true will get sent to the recording script.
    // - Only statuses with a localise value of true will get sent to the localisation strings.
    // - The writing status file will show all statuses.
    // If this section is not defined, no writing status tags are used and everything will be
    // sent to recording script and localisation.
    // If a line has no status it will be treated as "Unknown".
    public List<WritingStatusDefinition> WritingStatus { get; set; } = new();

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
    public bool OutputDinkStructure {get{return _options.OutputDinkStructure;}}
    public bool OutputLocalization {get{return _options.OutputLocalization;}}
    public bool OutputRecordingScript {get{return _options.OutputRecordingScript;}}
    public bool OutputWritingStatus {get{return _options.OutputWritingStatus;}}
    public string RootFilename {get{return Path.GetFileNameWithoutExtension(SourceInkFile);}}
    public List<AudioFolder> AudioFolders {get; private set;}
    public Dictionary<string, WritingStatusDefinition> WritingStatusOptions {get; private set;}
    
    public CompilerEnvironment(CompilerOptions options)
    {
        _options = options;    
        SourceInkFile = "";
        ProjectFile = "";
        DestFolder = "";
        AudioFolders = new List<AudioFolder>();
        WritingStatusOptions = new Dictionary<string, WritingStatusDefinition>();
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
        if (!Path.IsPathFullyQualified(DestFolder))
            DestFolder = Path.Combine(ProjectFolder, DestFolder);
        if (!Directory.Exists(DestFolder))
            Directory.CreateDirectory(DestFolder);
        Console.WriteLine($"Using destination folder: '{DestFolder}'");

        if (LocActionBeats)
            Console.WriteLine($"Including action beat text in localization output.");

        string audioFolderRoot = ProjectFolder;
        if (string.IsNullOrEmpty(audioFolderRoot))
            audioFolderRoot = SourceInkFolder;
        foreach (var audioFolder in _options.AudioFolders)
        {
            var expandedFolder = audioFolder.Folder;
            if (!Path.IsPathFullyQualified(expandedFolder))
                expandedFolder = Path.Join(audioFolderRoot,expandedFolder);
            if (!Directory.Exists(expandedFolder))
            {
                Console.WriteLine($"Warning: Audio folder '{expandedFolder}' doesn't exist.");
            }
            else
            {
                AudioFolders.Add(new AudioFolder { Status = audioFolder.Status, Folder = expandedFolder });
            }
        }

        foreach (var status in _options.WritingStatus)
        {
            WritingStatusOptions[status.WsTag] = status;
        }

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