using Dink;
using DinkTool;
using System.Text;

namespace DinkViewer;

public static class WordExporter
{
    public static bool ExportToWord(string jsonContent, ProjectEnvironment env, ViewerSettings settings)
    {
        string destFile = Path.Combine(settings.DestFolder, env.RootFilename + "-viewer.docx");
        var scenes = DinkJson.ReadScenes(jsonContent);

        if (!GenerateWordDoc(scenes, env.RootFilename, destFile))
            return false;

        System.Console.WriteLine($"Wrote {destFile}");

        /*if (!settings.Silent)
        {
            System.Console.WriteLine($"Opening in browser...");
            BrowserUtils.OpenURL(destFile);
        }*/

        return true;
    }

    private static bool GenerateWordDoc(List<DinkScene> scenes, string rootName, string destFile)
    {
        return true;
    }
}