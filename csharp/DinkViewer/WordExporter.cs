using Dink;
using DinkTool;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DinkViewer;

public static class WordExporter
{
    public static bool ExportToWord(string jsonContent, ProjectEnvironment env, ViewerSettings settings)
    {
        string destFile = Path.Combine(settings.DestFolder, env.RootFilename + "-readable.docx");
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

                AddStylesPartToPackage(wordDocument);

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
                        scenePPr = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading1" }, new SpacingBetweenLines() { After = "0" });
                    else
                        scenePPr = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading1" }, new SpacingBetweenLines() { Before = "240", After = "0" });
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
                            AddParagraph(body, block.BlockID, new ParagraphProperties(new ParagraphStyleId() { Val = "Heading2" }), new RunProperties(new Bold(), new FontSize() { Val = "22" }));
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

    private static void AddStylesPartToPackage(WordprocessingDocument doc)
    {
        StyleDefinitionsPart part;
        part = doc.MainDocumentPart!.AddNewPart<StyleDefinitionsPart>();
        Styles root = new Styles();
        root.Save(part);

        // Create a new "Heading 1" style
        Style styleHeading1 = new Style() { Type = StyleValues.Paragraph, StyleId = "Heading1" };
        styleHeading1.Append(new Name() { Val = "heading 1" });
        styleHeading1.Append(new BasedOn() { Val = "Normal" });
        styleHeading1.Append(new NextParagraphStyle() { Val = "Normal" });
        styleHeading1.Append(new UIPriority() { Val = 9 });
        styleHeading1.Append(new PrimaryStyle());
        styleHeading1.Append(new Rsid() { Val = "00F258AE" });
        styleHeading1.Append(new StyleParagraphProperties(new KeepNext(), new KeepLines(), new SpacingBetweenLines() { Before = "240", After = "0" }, new OutlineLevel() { Val = 0 }));
        styleHeading1.Append(new StyleRunProperties(new RunFonts() { Ascii = "Calibri Light", HighAnsi = "Calibri Light" }, new Color() { Val = "2F5496", ThemeColor = ThemeColorValues.Accent1, ThemeShade = "BF" }, new FontSize() { Val = "32" }, new FontSizeComplexScript() { Val = "32" }));
        root.Append(styleHeading1);

        // Create a new "Heading 2" style
        Style styleHeading2 = new Style() { Type = StyleValues.Paragraph, StyleId = "Heading2" };
        styleHeading2.Append(new Name() { Val = "heading 2" });
        styleHeading2.Append(new BasedOn() { Val = "Normal" });
        styleHeading2.Append(new NextParagraphStyle() { Val = "Normal" });
        styleHeading2.Append(new UIPriority() { Val = 9 });
        styleHeading2.Append(new UnhideWhenUsed());
        styleHeading2.Append(new PrimaryStyle());
        styleHeading2.Append(new Rsid() { Val = "00F258AE" });
        styleHeading2.Append(new StyleParagraphProperties(new KeepNext(), new KeepLines(), new SpacingBetweenLines() { Before = "40", After = "0" }, new OutlineLevel() { Val = 1 }));
        styleHeading2.Append(new StyleRunProperties(new RunFonts() { Ascii = "Calibri Light", HighAnsi = "Calibri Light" }, new Color() { Val = "2F5496", ThemeColor = ThemeColorValues.Accent1, ThemeShade = "BF" }, new FontSize() { Val = "26" }, new FontSizeComplexScript() { Val = "26" }));
        root.Append(styleHeading2);

        // Create a new "Normal" style (required for "BasedOn")
        Style styleNormal = new Style() { Type = StyleValues.Paragraph, StyleId = "Normal", Default = true };
        styleNormal.Append(new Name() { Val = "Normal" });
        styleNormal.Append(new PrimaryStyle());
        styleNormal.Append(new Rsid() { Val = "00F258AE" });
        styleNormal.Append(new StyleParagraphProperties(new SpacingBetweenLines() { After = "160", Line = "259", LineRule = LineSpacingRuleValues.Auto }));
        styleNormal.Append(new StyleRunProperties(new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" }, new FontSize() { Val = "22" }, new FontSizeComplexScript() { Val = "22" }));
        root.Append(styleNormal);

        root.Save(part);
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
