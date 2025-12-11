namespace DinkTool;

using System.Text.Json;

public class AudioStatusDefinition
{
    public string Status { get; set; } = "Unknown";
    public string Folder { get; set; } = "";
    public bool Recorded { get; set; } = false;
    public string Color {get; set;} = "";
}

public class WritingStatusDefinition
{
    public string Status { get; set; } = "Unknown";
    public string WsTag { get; set; } = "";
    public bool Record { get; set; } = false;
    public bool Loc { get; set; } = false;
    public string Color {get; set;} = "";
    public bool Estimate {get; set;} = false;
}

public class GoogleTTSSettings 
{
    public bool Generate {get;set;} = false;
    public string Authentication {get;set;} = "";
    public string OutputFolder {get;set;} = "";
    public bool ReplaceExisting {get;set;} = false;
}

public class Estimate
{
    public string Tag {get;set;} = "";
    public int Lines {get;set;} = 0;
}

public class ProjectSettings
{
    // Project file
    // Can be omitted
    public string ProjectFile = "";

    // Source ink file.
    public string Source = "";

    // Folder to output compiled assets to.
    public string DestFolder = "";

    // Default locale code - just gets used in filenames
    public string DefaultLocaleCode = "en-GB";

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

    // If true, outputs the recording script file (xlsx)
    public bool OutputRecordingScript = false;

    // Sometimes you want to output every single line in a recording or loc script
    // to see what you've got.
    public bool IgnoreWritingStatus = false;

    // Output a statistics document
    public bool OutputStats = false;

    // Output the line origins as a JSON document
    public bool OutputOrigins = false;


    // This is where the game will look for
    // audio files that start with the ID names of the lines.
    // The folders (and their children) will be searched in this
    // order, so if a line is found in (say) the Audio/Recorded folder first, 
    // its status in the voice script will be set to Recorded.
    // If not found, the status will be set to Unknown.
    public List<AudioStatusDefinition> AudioStatus { get; set; } = new();

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

    // Control which comments are seen on which script
    public Dictionary<string, List<string>> CommentFilters { get; set; } = new();
    
    // Control which tags are seen on which script
    public Dictionary<string, List<string>> TagFilters { get; set; } = new();

    public GoogleTTSSettings GoogleTTS {get; set; } = new();

    public List<Estimate> Estimates {get;set;} = new();

    public static ProjectSettings? LoadFromProjectFile(string projectFile)
    {
        if (!Path.IsPathFullyQualified(projectFile))
        {
            projectFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, projectFile));
        }
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

        ProjectSettings? result = JsonSerializer.Deserialize<ProjectSettings>(json, jsonOptions);
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

public class ProjectEnvironment
{
    private ProjectSettings _settings;
    public string SourceInkFile {get; private set;}
    public string SourceInkFolder {
        get {
            return Path.GetDirectoryName(SourceInkFile)??"";
        }
    }
    public string ProjectFile {get; private set;}
    public string ProjectFolder {get;private set;}
    public string DestFolder {get; private set;}
    public string DefaultLocaleCode {get {return _settings.DefaultLocaleCode;}}
    public bool LocActionBeats {get{return _settings.LocActionBeats;}}
    public bool OutputDinkStructure {get{return _settings.OutputDinkStructure;}}
    public bool OutputLocalization {get{return _settings.OutputLocalization;}}
    public bool OutputRecordingScript {get{return _settings.OutputRecordingScript;}}
    public bool OutputOrigins {get{return _settings.OutputOrigins;}}
    public bool IgnoreWritingStatus {get {return _settings.IgnoreWritingStatus;}}
    public bool OutputStats{ get {return _settings.OutputStats;}}
    public string RootFilename {get{return Path.GetFileNameWithoutExtension(SourceInkFile);}}
    public List<AudioStatusDefinition> AudioStatusSettings {get; private set;}
    public List<WritingStatusDefinition> WritingStatusSettings {get; private set;}
    public Dictionary<string, List<string>> CommentFilters {get {return _settings.CommentFilters;}}
    public Dictionary<string, List<string>> TagFilters {get {return _settings.TagFilters;}}
    public GoogleTTSSettings GoogleTTS {get; private set;}
    public List<Estimate> Estimates {get {return _settings.Estimates;}}

    public ProjectEnvironment(ProjectSettings settings)
    {
        _settings = settings;    
        SourceInkFile = "";
        ProjectFile = "";
        DestFolder = "";
        ProjectFolder = "";
        AudioStatusSettings = new List<AudioStatusDefinition>();
        WritingStatusSettings = new List<WritingStatusDefinition>();
        GoogleTTS = new GoogleTTSSettings();
    }

    public bool Init()
    {
        ProjectFile = _settings.ProjectFile;
        if (!string.IsNullOrWhiteSpace(ProjectFile))
        {
            if (!Path.IsPathFullyQualified(ProjectFile))
            {
                ProjectFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ProjectFile));
            }
            ProjectFolder = Path.GetDirectoryName(ProjectFile)??"";
        }

        SourceInkFile = _settings.Source;
        bool foundInkFile = false;
        if (string.IsNullOrEmpty(SourceInkFile))
        {
            Console.Error.WriteLine("No Ink file specified.");
            return false;
        }
        if (Path.IsPathFullyQualified(SourceInkFile))
        {
            foundInkFile = File.Exists(SourceInkFile);
            if (!foundInkFile)
            {
                Console.WriteLine($"Tried to find ink file: '{SourceInkFile}'");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(ProjectFolder))
            {
                var lookForFile = Path.GetFullPath(Path.Combine(ProjectFolder, SourceInkFile));
                if (File.Exists(lookForFile))
                {
                    SourceInkFile = lookForFile;
                    foundInkFile = true;
                }
                else
                {
                    Console.WriteLine($"Tried to find ink file: '{lookForFile}'");
                }
            }
            if (!foundInkFile)
            {
                var lookForFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, SourceInkFile));
                if (File.Exists(lookForFile))
                {
                    SourceInkFile = lookForFile;
                    foundInkFile = true;
                }
                else
                {
                    Console.WriteLine($"Tried to find ink file: '{lookForFile}'");
                }
            }
        }
        if (!foundInkFile)
        {
            Console.WriteLine($"Source Ink file doesn't exist:'{SourceInkFile}'");
            return false;
        }
        Console.WriteLine($"Using source ink file: '{SourceInkFile}'");

        if (string.IsNullOrWhiteSpace(ProjectFolder))
        {
            ProjectFolder = Path.GetDirectoryName(SourceInkFile)??"";
        }

        DestFolder = _settings.DestFolder;
        if (string.IsNullOrWhiteSpace(DestFolder))
            DestFolder = Environment.CurrentDirectory;
        if (!Path.IsPathFullyQualified(DestFolder))
            DestFolder = Path.GetFullPath(Path.Combine(ProjectFolder, DestFolder));
        if (!Directory.Exists(DestFolder))
            Directory.CreateDirectory(DestFolder);
        Console.WriteLine($"Using destination folder: '{DestFolder}'");

        if (LocActionBeats)
            Console.WriteLine($"Including action beat text in localization output.");

        string audioFolderRoot = ProjectFolder;
        if (string.IsNullOrEmpty(audioFolderRoot))
            audioFolderRoot = SourceInkFolder;
        bool hasUnknown = false;
        foreach (var audioStatusDef in _settings.AudioStatus)
        {
            var expandedFolder = audioStatusDef.Folder;
            if (audioStatusDef.Status=="Unknown")
                hasUnknown = true;
            if (!Path.IsPathFullyQualified(expandedFolder))
                expandedFolder = Path.GetFullPath(Path.Combine(audioFolderRoot,expandedFolder));
            if (!Directory.Exists(expandedFolder))
            {
                Console.WriteLine($"Warning: Audio folder '{expandedFolder}' doesn't exist.");
            }
            else
            {
                audioStatusDef.Folder = expandedFolder;
                AudioStatusSettings.Add(audioStatusDef);
            }
        }
        if (!hasUnknown)
            AudioStatusSettings.Add(new AudioStatusDefinition());
        AudioStatusSettings.Reverse();

        WritingStatusSettings.AddRange(_settings.WritingStatus);
        var unknown = WritingStatusSettings.FirstOrDefault(x => x.Status == "Unknown");
        if (unknown==null)
            WritingStatusSettings.Add(new WritingStatusDefinition());
        WritingStatusSettings.Reverse();

        GoogleTTS = _settings.GoogleTTS;
        string ttsAuthFile = GoogleTTS.Authentication;
        if (!string.IsNullOrEmpty(ttsAuthFile))
        {
            var lookForFile = ttsAuthFile;
            if (!Path.IsPathFullyQualified(lookForFile))
            {
                lookForFile = Path.GetFullPath(Path.Combine(ProjectFolder, ttsAuthFile));
                if (!File.Exists(lookForFile))
                {
                    lookForFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, ttsAuthFile));
                }
            }
            if (!File.Exists(lookForFile))
            {
                Console.Error.WriteLine("Can't find GoogleTTS authentication file:"+lookForFile);
                return false;
            }
            GoogleTTS.Authentication = lookForFile;
        }
        if (!Path.IsPathFullyQualified(GoogleTTS.OutputFolder))
            GoogleTTS.OutputFolder = Path.GetFullPath(Path.Combine(audioFolderRoot,GoogleTTS.OutputFolder));
        
        return true;
    }

    // If it's a relative file, first looks in the same folder as the main Ink file,
    // then looks in the same folder as the project (if there is one),
    // then looks in the CWD
    public string? FindFileInSource(string fileName)
    {
        if (Path.IsPathFullyQualified(fileName))
            return fileName;
        string filePath = Path.GetFullPath(Path.Combine(SourceInkFolder, fileName));
        if (File.Exists(filePath))
            return filePath;
        if (!string.IsNullOrEmpty(ProjectFile)) {
            filePath = Path.GetFullPath(Path.Combine(ProjectFolder, fileName));
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
        return Path.GetFullPath(Path.Combine(DestFolder, RootFilename + suffix));
    }

    public string DestDinkFile {get{return MakeDestFile("-dink.json");}}
    public string DestCompiledInkFile {get{return MakeDestFile(".json");}}
    public string DestDinkStructureFile {get{return MakeDestFile("-dink-structure.json");}}
    public string DestRecordingScriptFile {get{return MakeDestFile("-recording.xlsx");}}
    public string DestRuntimeStringsFile {get{return MakeDestFile($"-strings-{DefaultLocaleCode}.json");}}
    public string DestLocFile {get{return MakeDestFile("-loc.xlsx");}}
    public string DestStatsFile {get{return MakeDestFile("-stats.xlsx");}}
    public string DestOriginsFile {get {return MakeDestFile("-origins.json");}}
    public List<string> GetCommentFilters(string commentType)
    {
        if (CommentFilters.TryGetValue(commentType, out List<string>? filters))
        {
            return filters;
        }
        // By default everything gets through
        return new List<string>(){"*"};
    }

    public List<string> GetTagFilters(string tagType)
    {
        if (TagFilters.TryGetValue(tagType, out List<string>? filters))
        {
            return filters;
        }
        // By default nothing gets through
        return new List<string>();
    }

    public bool GetAudioStatusByLabel(string status, out AudioStatusDefinition audioStatusDef)
    {
        audioStatusDef = new AudioStatusDefinition();
        var result = AudioStatusSettings.FirstOrDefault(x => x.Status == status);
        if (result!=null)
        {
            audioStatusDef = result;
            return true;
        }
        return false;
    }
}