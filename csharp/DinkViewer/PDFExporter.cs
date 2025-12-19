using Dink;
using DinkTool;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using System.Reflection;

namespace DinkViewer;

public static class PDFExporter
{
    static PDFExporter()
    {
        // Ensure custom font resolver is used for cross-platform support
        if (GlobalFontSettings.FontResolver is not DinkFontResolver)
        {
            try 
            {
                GlobalFontSettings.FontResolver = new DinkFontResolver();
            }
            catch 
            {
                // Can fail if already set elsewhere, ignore
            }
        }
    }

    public static bool ExportToPDF(string jsonContent, ProjectEnvironment env, ViewerSettings settings)
    {
        string destFile = Path.Combine(settings.DestFolder, env.RootFilename + "-readable.pdf");
        var scenes = DinkJson.ReadScenes(jsonContent);

        if (!GeneratePDFDoc(scenes, env.RootFilename, destFile))
            return false;

        Console.WriteLine($"Wrote {destFile}");

        if (!settings.Silent)
        {
            Console.WriteLine($"Opening PDF document...");
            SystemUtils.OpenUsingDefaultApp(destFile);
        }

        return true;
    }

    private static bool GeneratePDFDoc(List<DinkScene> scenes, string rootName, string destFile)
    {
        try
        {
            var document = new Document();
            DefineStyles(document);

            var section = document.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.LeftMargin = Unit.FromInch(1);
            section.PageSetup.RightMargin = Unit.FromInch(1);
            section.PageSetup.TopMargin = Unit.FromInch(1);
            section.PageSetup.BottomMargin = Unit.FromInch(1);

            // Title
            var title = section.AddParagraph($"Dink Script: {rootName}");
            title.Style = "Title";
            title.Format.SpaceAfter = Unit.FromPoint(12);

            bool wasLastOperationASeparator = false;
            bool isFirstScene = true;

            foreach (var scene in scenes)
            {
                if (!isFirstScene)
                {
                    if (!wasLastOperationASeparator)
                    {
                        section.AddPageBreak();
                        wasLastOperationASeparator = true;
                    }
                }

                // Scene Header
                var sceneHeader = section.AddParagraph(string.IsNullOrEmpty(scene.SceneID) ? "Scene" : scene.SceneID);
                sceneHeader.Style = "Heading1";
                if (isFirstScene) sceneHeader.Format.SpaceBefore = 0;
                
                isFirstScene = false;
                wasLastOperationASeparator = false;

                if (scene.Comments != null && scene.Comments.Count > 0)
                {
                    AddComments(section, scene.Comments, 0);
                    wasLastOperationASeparator = false;
                }

                foreach (var block in scene.Blocks)
                {
                    if (!wasLastOperationASeparator) { AddBlockSeparator(section); wasLastOperationASeparator = true; }

                    if (!string.IsNullOrEmpty(block.BlockID))
                    {
                        var blockP = section.AddParagraph(block.BlockID);
                        blockP.Style = "Heading2";
                        wasLastOperationASeparator = false;
                    }

                    if (block.Comments != null && block.Comments.Count > 0)
                    {
                        AddComments(section, block.Comments, 0);
                        wasLastOperationASeparator = false;
                    }

                    var groupedSnippets = block.Snippets.GroupBy(s => s.Group > 0 ? s.Group.ToString() : s.SnippetID).ToList();

                    foreach (var group in groupedSnippets)
                    {
                        if (!wasLastOperationASeparator) { AddGroupSeparator(section); wasLastOperationASeparator = true; }

                        if (group.Count() > 1)
                        {
                            var first = group.First();
                            var groupP = section.AddParagraph($"Group ({first.GroupCount} snippets)");
                            groupP.Format.LeftIndent = Unit.FromInch(0.5);
                            groupP.Format.Font.Italic = true;
                            wasLastOperationASeparator = false;

                            if (first.GroupComments != null)
                            {
                                AddComments(section, first.GroupComments, 0.5f);
                                wasLastOperationASeparator = false;
                            }
                        }

                        foreach (var snippet in group)
                        {
                            if (!wasLastOperationASeparator) { AddSnippetSeparator(section); wasLastOperationASeparator = true; }

                            if (snippet.Comments != null && snippet.Comments.Count > 0)
                            {
                                AddComments(section, snippet.Comments, 0.5f);
                                wasLastOperationASeparator = false;
                            }

                            foreach (var beat in snippet.Beats)
                            {
                                if (beat.Comments != null && beat.Comments.Count > 0)
                                {
                                    AddComments(section, beat.Comments, 1.0f);
                                    wasLastOperationASeparator = false;
                                }

                                if (beat is DinkLine line)
                                {
                                    // Character Name
                                    var charP = section.AddParagraph(line.CharacterID + (!string.IsNullOrEmpty(line.Qualifier) ? $" ({line.Qualifier})" : ""));
                                    charP.Style = "ScriptText";
                                    charP.Format.LeftIndent = Unit.FromInch(2.5);
                                    wasLastOperationASeparator = false;

                                    // Direction
                                    if (!string.IsNullOrEmpty(line.Direction))
                                    {
                                        var dirP = section.AddParagraph($"({line.Direction})");
                                        dirP.Style = "ScriptText";
                                        dirP.Format.Font.Italic = true;
                                        dirP.Format.LeftIndent = Unit.FromInch(2.0);
                                        wasLastOperationASeparator = false;
                                    }

                                    // Dialogue
                                    var dialP = section.AddParagraph(line.Text);
                                    dialP.Style = "ScriptText";
                                    dialP.Format.LeftIndent = Unit.FromInch(1.5);
                                    dialP.Format.SpaceAfter = 6;
                                    wasLastOperationASeparator = false;
                                }
                                else if (beat is DinkAction action)
                                {
                                    var actP = section.AddParagraph(action.Text);
                                    actP.Style = "ScriptText";
                                    actP.Format.LeftIndent = Unit.FromInch(0.5);
                                    actP.Format.SpaceAfter = 6;
                                    wasLastOperationASeparator = false;
                                }
                            }
                        }
                    }
                }
            }

            var renderer = new PdfDocumentRenderer();
            renderer.Document = document;
            renderer.RenderDocument();
            renderer.PdfDocument.Save(destFile);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error generating PDF doc: {e.Message}");
            Console.WriteLine(e.StackTrace);
            return false;
        }

        return true;
    }

    private static void DefineStyles(Document document)
    {
        // Get the predefined style Normal.
        var style = document.Styles["Normal"];
        style!.Font.Name = "Roboto";
        style.Font.Size = 11;

        style = document.Styles.AddStyle("Title", "Normal");
        style.Font.Size = 16;
        style.Font.Bold = true;
        style.ParagraphFormat.Alignment = ParagraphAlignment.Center;

        style = document.Styles.AddStyle("Heading1", "Normal");
        style.Font.Size = 14;
        style.Font.Bold = true;
        style.Font.Color = Colors.DarkBlue;
        style.ParagraphFormat.SpaceBefore = 12;

        style = document.Styles.AddStyle("Heading2", "Normal");
        style.Font.Size = 11;
        style.Font.Bold = true;
        style.Font.Color = Colors.DarkBlue;

        style = document.Styles.AddStyle("Comment", "Normal");
        style.Font.Italic = true;
        style.Font.Color = Colors.Gray;

        style = document.Styles.AddStyle("ScriptText", "Normal");
        style.Font.Name = "Courier Prime";
    }

    private static void AddComments(Section section, List<string>? comments, float indentInches)
    {
        if (comments != null && comments.Count > 0)
        {
            foreach (var comment in comments)
            {
                var p = section.AddParagraph($"// {comment}");
                p.Style = "Comment";
                p.Format.LeftIndent = Unit.FromInch(indentInches);
            }
        }
    }

    private static void AddBlockSeparator(Section section)
    {
        var p = section.AddParagraph();
        p.Format.Borders.Bottom.Width = 1;
        p.Format.Borders.Bottom.Color = Colors.DarkGray;
        p.Format.SpaceAfter = 12;
    }

    private static void AddGroupSeparator(Section section)
    {
        var p = section.AddParagraph();
        p.Format.Borders.Bottom.Width = 0.5;
        p.Format.Borders.Bottom.Color = Colors.Gray;
        p.Format.SpaceAfter = 6;
    }

    private static void AddSnippetSeparator(Section section)
    {
        var p = section.AddParagraph();
        p.Format.Borders.Bottom.Width = 0.5;
        p.Format.Borders.Bottom.Color = Colors.LightGray;
        p.Format.SpaceAfter = 6;
    }
}

public class DinkFontResolver : IFontResolver
{
    public string DefaultFontName => "Roboto";

    public byte[]? GetFont(string faceName)
    {
        return LoadFontFromResource(faceName);
    }

    private byte[]? LoadFontFromResource(string faceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Search for resource ending with "fonts.<faceName>" to be namespace-agnostic
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.EndsWith("fonts." + faceName, StringComparison.OrdinalIgnoreCase));

            if (resourceName != null)
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        return bytes;
                    }
                }
            }
        }
        catch { /* Ignore */ }
        return null;
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        string suffix = "";
        if (bold && italic) suffix = "-BoldItalic";
        else if (bold) suffix = "-Bold";
        else if (italic) suffix = "-Italic";
        else suffix = "-Regular";

        string filename;

        if (familyName.Equals("Courier Prime", StringComparison.OrdinalIgnoreCase))
        {
            filename = "CourierPrime" + suffix + ".ttf";
        }
        else if (familyName.Equals("Roboto", StringComparison.OrdinalIgnoreCase))
        {
             filename = "Roboto" + suffix + ".ttf";
        }
        else
        {
            // Default fallback
            filename = "Roboto" + suffix + ".ttf";
        }

        return new FontResolverInfo(filename);
    }
}
