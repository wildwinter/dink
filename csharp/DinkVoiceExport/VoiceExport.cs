namespace DinkVoiceExport;

using DinkTool;
using Dink;

public class ExportSettings
{
    public string DestFolder {get; set;} = "";
    public string AudioStatus {get;set;} = "";
    public string AudioFolder {get;set;} = "";
    public List<string> Tags {get; set;} = new List<string>();
    public string Character {get; set;} = "";
    public string Scene {get; set;} = "";

    public bool Init()
    {
        if (!string.IsNullOrEmpty(AudioFolder) && !Path.IsPathFullyQualified(AudioFolder))
        {
            AudioFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, AudioFolder));
        }
        if (!Path.IsPathFullyQualified(DestFolder))
        {
            DestFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, DestFolder));
        }
        return true;
    }
}

public class VoiceExport
{
    private ProjectEnvironment _env;
    private ExportSettings _exportSettings;

    public VoiceExport(ProjectSettings projectSettings, ExportSettings exportSettings)
    {
        _env = new ProjectEnvironment(projectSettings);
        _exportSettings = exportSettings;

    }

    public bool Run()
    {
        if (!_env.Init()||!_exportSettings.Init())
            return false;

        string sourceFolder = "";
        if (!string.IsNullOrEmpty(_exportSettings.AudioStatus))
        {
            AudioStatusDefinition audioStatusDefinition = new AudioStatusDefinition();
            if (_env.GetAudioStatusByLabel(_exportSettings.AudioStatus, out audioStatusDefinition))
            {
                sourceFolder = audioStatusDefinition.Folder;
            }
            else
            {
                Console.Error.WriteLine($"Couldn't find audio status:'{_exportSettings.AudioStatus}'");
                return false;
            }
        }
        else if (!string.IsNullOrEmpty(_exportSettings.AudioFolder))
        {
            sourceFolder = _exportSettings.AudioFolder;
        }
        if (!Directory.Exists(sourceFolder))
        {
            Console.Error.WriteLine($"Source audio folder missing:'{sourceFolder}'");
            return false;
        }

        if (!ReadScenes(_env.DestDinkStructureFile, out List<DinkScene> scenes))
            return false;

        if (!MatchLines(scenes, out List<DinkLine> lines))
            return false;

        if (lines.Count==0)
        {
            Console.WriteLine("No lines found.");
            return false;
        }

        Console.WriteLine($"Matched {lines.Count} lines.");

        if (!CopyWAVs(sourceFolder, _exportSettings.DestFolder, lines))
            return false;
        
        return true;
    }

    private bool ReadScenes(string scenesFile, out List<DinkScene> scenes)
    {
        scenes = new List<DinkScene>();
        if (!File.Exists(scenesFile))
        {
            Console.WriteLine($"{scenesFile} not found - make sure the Dink Compiler was run before using this utility.");
            return false;
        }

        string fileText = File.ReadAllText(scenesFile);
        scenes = DinkJson.ReadScenes(fileText);
        Console.WriteLine($"Read {scenesFile}.");
        return true;
    }

    private bool MatchLines(List<DinkScene> scenes, out List<DinkLine> lines)
    {
        lines = new List<DinkLine>();

        foreach (var scene in scenes)
        {
            if (!string.IsNullOrEmpty(_exportSettings.Scene) && scene.SceneID!=_exportSettings.Scene)
                continue;

            foreach (var block in scene.Blocks)
            {
                foreach (var snippet in block.Snippets)
                {
                    foreach (var beat in snippet.Beats)
                    {
                        if (beat is DinkLine dinkLine)
                        {
                            if (!string.IsNullOrEmpty(_exportSettings.Character))
                            {
                                if (dinkLine.CharacterID!=_exportSettings.Character)
                                    continue;
                            }
                            if (_exportSettings.Tags.Count>0)
                            {
                                bool foundTag = false;
                                foreach (string tag in _exportSettings.Tags)
                                {
                                    if (tag.EndsWith(":"))
                                    {
                                        foundTag = dinkLine.Tags.Any(s => s.StartsWith(tag));
                                    }
                                    else
                                    {
                                        foundTag = dinkLine.Tags.Contains(tag);
                                    }
                                    if (foundTag)
                                        break;
                                }
                                if (!foundTag)
                                    continue;
                            }
                            lines.Add(dinkLine);
                        }
                    }
                }
            }
        }
        return true;
    }

    private string? FindWAV(string sourceFolder, string id)
    {
        foreach (var filePath in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            if (nameWithoutExt.StartsWith(id, StringComparison.OrdinalIgnoreCase))
                return filePath;
        }
        return null;
    }

    private bool CopyWAVs(string sourceFolder, string destFolder, List<DinkLine> lines)
    {
        Directory.CreateDirectory(destFolder);
        int copied = 0;
        int skipped = 0;
        foreach (var line in lines)
        {
            string? sourceFile = FindWAV(sourceFolder, line.LineID);
            if (sourceFile==null)
            {
                Console.WriteLine($"No source file found for ID '{line.LineID}'");
                skipped++;
                continue;
            }
            copied++;
            File.Copy(sourceFile, Path.Combine(destFolder, Path.GetFileName(sourceFile)), overwrite: true);
        }
        Console.WriteLine($"VoiceExport complete. Copied {copied} files.");
        if (skipped>0)
            Console.WriteLine($"{skipped} lines couldn't be found in source folder.");
        return true;
    }
}