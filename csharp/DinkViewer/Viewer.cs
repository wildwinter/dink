namespace DinkViewer;

using DinkTool;

public class ViewerSettings
{
    public string DestFolder {get; set;} = "";
    public string DestFile {get; set;} = "";
    public bool Silent { get; set; } = false;
    public bool ExportToWord {get; set;} = false;
    public bool ExportToPdf {get; set;} = false;

    public bool Init()
    {
        // Handle DestFile if specified
        if (!string.IsNullOrEmpty(DestFile))
        {
            // Expand relative paths to be relative to current working directory
            if (!Path.IsPathFullyQualified(DestFile))
            {
                DestFile = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, DestFile));
            }

            // Adjust extension based on export type
            string currentExt = Path.GetExtension(DestFile).ToLowerInvariant();

            if (ExportToWord && currentExt != ".doc" && currentExt != ".docx")
            {
                DestFile = Path.ChangeExtension(DestFile, ".docx");
            }
            else if (ExportToPdf && currentExt != ".pdf")
            {
                DestFile = Path.ChangeExtension(DestFile, ".pdf");
            }
            else if (!ExportToWord && !ExportToPdf && currentExt != ".html")
            {
                DestFile = Path.ChangeExtension(DestFile, ".html");
            }

            // Infer DestFolder from DestFile
            string? directory = Path.GetDirectoryName(DestFile);
            if (!string.IsNullOrEmpty(directory))
            {
                DestFolder = directory;
            }
        }

        // Handle DestFolder fallback
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
            return PDFExporter.ExportToPDF(jsonContent, _env, _viewerSettings);
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