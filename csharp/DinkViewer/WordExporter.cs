using Dink;
using DinkTool;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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

        if (!settings.Silent)
        {
            System.Console.WriteLine($"Opening Word document...");
            SystemUtils.OpenUsingDefaultApp(destFile);
        }

        return true;
    }

    private static bool GenerateWordDoc(List<DinkScene> scenes, string rootName, string destFile)
    {
        try
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(destFile, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                bool wasLastOperationASeparator = false;
                bool isFirstScene = true;

                AddParagraph(body, $"Dink Script: {rootName}", new ParagraphProperties(new Justification() { Val = JustificationValues.Center }), new RunProperties(new Bold(), new FontSize() { Val = "32" }));
                wasLastOperationASeparator = false;

                foreach (var scene in scenes)
                {
                    if (!isFirstScene)
                    {
                        if (!wasLastOperationASeparator) { AddSceneBreak(body); wasLastOperationASeparator = true; }
                    }
                    ParagraphProperties scenePPr;
                    if (isFirstScene)
                        scenePPr = new ParagraphProperties(new SpacingBetweenLines() { After = "0" });
                    else
                        scenePPr = new ParagraphProperties(new SpacingBetweenLines() { Before = "240", After = "0" });
                    isFirstScene = false;

                    AddParagraph(body, string.IsNullOrEmpty(scene.SceneID) ? "Scene" : scene.SceneID, scenePPr, new RunProperties(new Bold(), new FontSize() { Val = "28" }));
                    wasLastOperationASeparator = false;
                    
                    if (scene.Comments != null && scene.Comments.Count > 0)
                    {
                        AddComments(body, scene.Comments);
                        wasLastOperationASeparator = false;
                    }

                    foreach (var block in scene.Blocks)
                    {
                        if (!wasLastOperationASeparator) { AddBlockSeparator(body); wasLastOperationASeparator = true; }

                        if (!string.IsNullOrEmpty(block.BlockID))
                        {
                            AddParagraph(body, block.BlockID, new ParagraphProperties(), new RunProperties(new Bold(), new FontSize() { Val = "22" }));
                            wasLastOperationASeparator = false;
                        }

                        if (block.Comments != null && block.Comments.Count > 0)
                        {
                            AddComments(body, block.Comments);
                            wasLastOperationASeparator = false;
                        }

                        var groupedSnippets = block.Snippets.GroupBy(s => s.Group > 0 ? s.Group.ToString() : s.SnippetID).ToList();
                        
                        foreach (var group in groupedSnippets)
                        {
                            if (!wasLastOperationASeparator) { AddGroupSeparator(body); wasLastOperationASeparator = true; }

                            if (group.Count() > 1)
                            {
                                var first = group.First();
                                AddParagraph(body, $"Group ({first.GroupCount} snippets)", new ParagraphProperties(new Indentation() { Left = "720" }), new RunProperties(new Italic()));
                                wasLastOperationASeparator = false;

                                if (first.GroupComments != null)
                                {
                                    AddComments(body, first.GroupComments, "720");
                                    wasLastOperationASeparator = false;
                                }
                            }
                            
                            foreach (var snippet in group)
                            {
                                if (!wasLastOperationASeparator) { AddSnippetSeparator(body); wasLastOperationASeparator = true; }

                                if (snippet.Comments != null && snippet.Comments.Count > 0)
                                {
                                    AddComments(body, snippet.Comments, "720");
                                    wasLastOperationASeparator = false;
                                }

                                bool isConsecutiveBeat = false;
                                foreach (var beat in snippet.Beats)
                                {
                                    if (beat.Comments != null && beat.Comments.Count > 0)
                                    {
                                        AddComments(body, beat.Comments, "1440");
                                        wasLastOperationASeparator = false;
                                        isConsecutiveBeat = false;
                                    }

                                    if (beat is DinkLine line)
                                    {
                                        var pPr = new ParagraphProperties(new Indentation() { Left = "3600" });
                                        if (isConsecutiveBeat)
                                        {
                                            pPr.Append(new SpacingBetweenLines() { Before = "120" });
                                        }

                                        AddParagraph(body, line.CharacterID + (!string.IsNullOrEmpty(line.Qualifier) ? $" ({line.Qualifier})" : ""), pPr, new RunProperties(new RunFonts() { Ascii = "Courier New" }));
                                        wasLastOperationASeparator = false;

                                        if (!string.IsNullOrEmpty(line.Direction))
                                        {
                                            AddParagraph(body, $"({line.Direction})", new ParagraphProperties(new Indentation() { Left = "2880" }), new RunProperties(new Italic(), new RunFonts() { Ascii = "Courier New" }));
                                            wasLastOperationASeparator = false;
                                        }
                                        
                                        Paragraph p = body.AppendChild(new Paragraph());
                                        p.Append(new ParagraphProperties(new Indentation() { Left = "2160" }).CloneNode(true));
                                        
                                        Run dialogueRun = p.AppendChild(new Run());
                                        dialogueRun.Append(new RunProperties(new RunFonts() { Ascii = "Courier New" }).CloneNode(true));
                                        dialogueRun.Append(new Text(line.Text));
                                        
                                        wasLastOperationASeparator = false;
                                        isConsecutiveBeat = true;
                                    }
                                    else if (beat is DinkAction action)
                                    {
                                        var pPr = new ParagraphProperties(new Indentation() { Left = "720" });
                                        if (isConsecutiveBeat)
                                        {
                                            pPr.Append(new SpacingBetweenLines() { Before = "120" });
                                        }
                                        AddParagraph(body, action.Text, pPr, new RunProperties(new RunFonts() { Ascii = "Courier New" }));
                                        wasLastOperationASeparator = false;
                                        isConsecutiveBeat = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error generating Word doc: {e.Message}");
            return false;
        }

        return true;
    }

    private static void AddParagraph(Body body, string text, ParagraphProperties pPr, RunProperties rPr)
    {
        Paragraph p = body.AppendChild(new Paragraph());
        if (pPr != null)
        {
            p.Append(pPr.CloneNode(true));
        }
        Run r = p.AppendChild(new Run());
        if (rPr != null)
        {
            r.Append(rPr.CloneNode(true));
        }
        r.Append(new Text(text));
    }
    
    private static void AddComments(Body body, List<string>? comments, string indent = "0")
    {
        if (comments != null && comments.Count > 0)
        {
            foreach (var comment in comments)
            {
                AddParagraph(body, $"// {comment}", new ParagraphProperties(new Indentation() { Left = indent }), new RunProperties(new Italic(), new Color() { Val = "808080" }));
            }
        }
    }

    private static void AddSceneBreak(Body body)
    {
        Paragraph p = body.AppendChild(new Paragraph());
        Run r = p.AppendChild(new Run());
        r.Append(new Break() { Type = BreakValues.Page });
    }

    private static void AddBlockSeparator(Body body)
    {
        Paragraph p = body.AppendChild(new Paragraph());
        ParagraphProperties pPr = p.AppendChild(new ParagraphProperties());
        pPr.Append(new SpacingBetweenLines() { Before = "0", After = "240" });
        pPr.Append(new ParagraphBorders(new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12, Color = "888888" }));
    }

    private static void AddGroupSeparator(Body body)
    {
        Paragraph p = body.AppendChild(new Paragraph());
        ParagraphProperties pPr = p.AppendChild(new ParagraphProperties());
        pPr.Append(new SpacingBetweenLines() { Before = "0", After = "120" });
        pPr.Append(new ParagraphBorders(new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = "A9A9A9" }));
    }

    private static void AddSnippetSeparator(Body body)
    {
        Paragraph p = body.AppendChild(new Paragraph());
        ParagraphProperties pPr = p.AppendChild(new ParagraphProperties());
        pPr.Append(new SpacingBetweenLines() { Before = "0", After = "120" });
        pPr.Append(new ParagraphBorders(new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Dotted), Size = 4, Color = "C0C0C0" }));
    }
}
