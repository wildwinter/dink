namespace DinkViewer;

using DinkTool;
using Dink;
using System.Text;
using System.Xml.Linq;

public class ViewerSettings
{
    public string DestFolder {get; set;} = "";
    public bool Export {get; private set;} = true;

    public bool Init()
    {
        if (string.IsNullOrEmpty(DestFolder))
        {
            string systemTempPath = Path.GetTempPath();
            string randomName = Path.GetRandomFileName();
            string tempDirectoryPath = Path.Combine(systemTempPath, randomName);
            DestFolder = tempDirectoryPath;
            Export = false;
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
        if (!ReadScenes(_env.DestDinkStructureFile, out List<DinkScene> scenes))
            return false;

        Directory.CreateDirectory(_viewerSettings.DestFolder);
        string destFile = Path.Combine(_viewerSettings.DestFolder, _env.RootFilename+"-viewer.html");
        if (!GenerateViewDocFile(destFile, scenes))
            return false;

        BrowserUtils.OpenURL(destFile);

        return true;
    }

    private bool ReadScenes(string scenesFile, out List<DinkScene> scenes)
    {
        scenes = new List<DinkScene>();
        if (!File.Exists(scenesFile))
        {
            Console.WriteLine($"{scenesFile} not found - make sure the Dink Compiler was run using --outputStructure before using this utility.");
            return false;
        }

        string fileText = File.ReadAllText(scenesFile);
        scenes = DinkJson.ReadScenes(fileText);
        Console.WriteLine($"Read {scenesFile}.");
        return true;
    }
    
    private bool GenerateViewDocFile(string destFile, List<DinkScene> scenes)
    {
        string html = GenerateViewDoc(scenes);
        File.WriteAllText(destFile, html, Encoding.UTF8);
        return true;
    }

    private string GenerateViewDoc(List<DinkScene> scenes)
    { 
        var doc = new XDocument(
            new XDocumentType("html", null, null, null),
            new XElement("html",
                new XElement("head",
                    new XElement("title", "Dink Viewer: "+_env.RootFilename),
                    new XElement("style", GetCss()) 
                ),
                new XElement("body",
                    new XElement("h1", "Dink Viewer: "+_env.RootFilename),
                    new XElement("div", new XAttribute("class", "project-container"),
                        scenes.Select(RenderScene)
                    )
                )
            )
        );
        return doc.ToString();
    }

    private static XElement RenderScene(DinkScene scene)
    {
        return new XElement("details",
            new XElement("summary", 
                new XAttribute("class", "scene-header"), 
                scene.SceneID,
                RenderComments(scene.Comments)
            ),
            new XElement("div", 
                new XAttribute("class", "indent"),
                scene.Blocks.Select(RenderBlock)
            )
        );
    }

    private static XElement RenderBlock(DinkBlock block)
    {
        return new XElement("details",
            new XElement("summary", 
                new XAttribute("class", "block-header"), 
                !string.IsNullOrEmpty(block.BlockID)?block.BlockID:"(main)",
                RenderComments(block.Comments)
            ),
            new XElement("div", 
                new XAttribute("class", "indent"),
                RenderSnippetGroups(block)
            )
        );
    }

    private static List<XElement> RenderSnippetGroups(DinkBlock block)
    {
        List<XElement> groups = new List<XElement>();
        foreach (var snippetGroup in block.IterateSnippetGroups())
        {
            var snippetElements = new List<XElement>();
            foreach(var snippet in snippetGroup)
            {
                snippetElements.Add(RenderSnippet(snippet));
            }
            
            if (snippetGroup[0].Group==0)
            {
                groups.AddRange(snippetElements);
            }
            else
            {
                var xGroup = new XElement("details",
                    new XAttribute("open", true),
                    new XElement("summary", 
                        new XAttribute("class", "group-header"), 
                        $"({snippetGroup[0].GroupCount})",
                        RenderComments(snippetGroup[0].GroupComments)
                    ),
                    new XElement("div", 
                        new XAttribute("class", "indent"),
                        snippetElements
                    )   
                );
                groups.Add(xGroup);
            }
        }
        return groups;
    }

    private static XElement RenderSnippet(DinkSnippet snippet)
    {
        return new XElement("details",
            new XAttribute("open", "true"),
            new XElement("summary", 
                new XAttribute("class", "snippet-header"),
                "TEMP",
                snippet.Group>0?$"{snippet.GroupIndex}/{snippet.GroupCount}":null,
                RenderComments(snippet.Comments)
            ),
            new XElement("div", 
                new XAttribute("class", "indent"),
                new XElement("ul",
                    snippet.Beats.Select(RenderBeat)
                )
            )
        );
    }

    private static XElement? RenderComments(List<string> comments)
    {
        if (comments.Count==0)
            return null;

        return new XElement("div",
            new XAttribute("class", "comments"),
            comments.Select(comment => 
                new XElement("div",
                    new XAttribute("class", "comment"),
                    comment)
            )
        );
    }

    private static XElement? RenderBeat(DinkBeat beat)
    {
        XElement xBeat = new XElement("div",
            new XAttribute("class", "beat"));

        if (beat is DinkLine line)
        {
            xBeat.AddClass("line");
            xBeat.Add(
                new XElement(
                "div",
                    new XAttribute("class", "character"),
                    line.CharacterID,
                    !string.IsNullOrEmpty(line.Qualifier)?$" ({line.Qualifier})":null
                ),
                new XElement(
                "div",
                    new XAttribute("class", "text"),
                    line.Text
                ),
                RenderComments(line.Comments)
            );
            return xBeat;
        }
        else if (beat is DinkAction action)
        {
            xBeat.AddClass("action");
            xBeat.Add(
                new XElement(
                "div",
                    new XAttribute("class", "text"),
                    action.Text
                ),
                RenderComments(action.Comments)
            );
            return xBeat;
        }
        return null;
    }

    private static string GetCss()
    {
        return @"
            body { font-family: 'Segoe UI', sans-serif; padding: 20px; }
            details { margin-bottom: 5px; }
            summary { cursor: pointer; font-weight: bold; padding: 5px; background: #f0f0f0; border-radius: 4px; }
            summary:hover { background: #e0e0e0; }
            .indent { margin-left: 20px; border-left: 2px solid #ddd; padding-left: 10px; }
            .block { margin-top: 10px; }
            .beat { margin: 2px 0; color: #333; }
            .comments { font-style: italic; display:inline-block; padding-left:10px;}
            .comments span { padding-right:10px; font-weight:normal; }

            .line {
                display: grid;

                grid-template-columns: 150px 1fr 1fr; 
                gap: 20px; 
                align-items: start; 
                margin-bottom: 20px;
                border-bottom: 1px solid #eee;
            }

            .line .character {
                font-weight: bold;
                overflow-wrap: break-word; 
                text-align: right;
            }

            .line .text {
                white-space: pre-wrap; 
            }

            .action 
            {
                display: grid;

                grid-template-columns: 1fr 1fr; 
                gap: 20px; 
                align-items: start; 
                margin-bottom: 20px;
                border-bottom: 1px solid #eee;
            }

            .comments {
                display: grid;

                grid-template-columns: 1fr; 
                align-items: start; 
                margin-bottom: 10px;
                font-size:0.8em;
            }

        ";
    }
}

public static class XElementExtensions
{
    public static void AddClass(this XElement element, string className)
    {
        var attr = element.Attribute("class");

        if (attr == null)
        {
            element.Add(new XAttribute("class", className));
        }
        else
        {
            var classes = attr.Value.Split(' ').ToList();
            if (!classes.Contains(className))
            {
                attr.Value += $" {className}";
            }
        }
    }
}