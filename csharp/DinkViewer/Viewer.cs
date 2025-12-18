namespace DinkViewer;

using DinkTool;

public class ViewerSettings
{
    public string DestFolder {get; set;} = "";
    public bool Silent { get; set; } = false;
    public bool ExportToWord {get; set;} = false;
    public bool ExportToPdf {get; set;} = false;

    public bool Init()
    {
        if (string.IsNullOrEmpty(DestFolder))
        {
            string systemTempPath = Path.GetTempPath();
            string randomName = Path.GetRandomFileName();
            string tempDirectoryPath = Path.Combine(systemTempPath, randomName);
            DestFolder = tempDirectoryPath;
        }
        if (!Path.IsPathFullyQualified(DestFolder))
        {
            DestFolder = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, DestFolder));
        }
        return true;
    }
}

public class Viewer
{
    private ProjectEnvironment _env;
    private ViewerSettings _viewerSettings;

    public Viewer(ProjectEnvironment env, ViewerSettings exportSettings)
    {
        _env = env;
        _viewerSettings = exportSettings;

    }

    public bool Run()
    {
        if (!ReadStructureJson(_env.DestDinkStructureFile, out string jsonContent))
            return false;

        Directory.CreateDirectory(_viewerSettings.DestFolder);

        if (_viewerSettings.ExportToWord)
        {
            return WordExporter.ExportToWord(jsonContent, _env, _viewerSettings);
        }
        if (_viewerSettings.ExportToPdf)
        {
            return PDFExporter.ExportToPdf(jsonContent, _env, _viewerSettings);
        }
        return WebExporter.ExportToWebPage(jsonContent, _env, _viewerSettings);
    }

    private bool ReadStructureJson(string scenesFile, out string jsonContent)
    {
        jsonContent = "";
        if (!File.Exists(scenesFile))
        {
            Console.WriteLine($"{scenesFile} not found - make sure the Dink Compiler was run using --outputStructure before using this utility.");
            return false;
        }

        jsonContent = File.ReadAllText(scenesFile);
        Console.WriteLine($"Read {scenesFile}.");
        return true;
    }
    
    }