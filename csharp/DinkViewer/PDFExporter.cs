using Dink;
using DinkTool;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DinkViewer;

public static class PDFExporter
{
    public static bool ExportToPdf(string jsonContent, ProjectEnvironment env, ViewerSettings settings)
    {
        // Set license to Community to avoid watermark (if using version < 2023.10 this might not be needed or different, but good practice for newer)
        // Note: For commercial use, a license key is required.
        QuestPDF.Settings.License = LicenseType.Community;

        string destFile = Path.Combine(settings.DestFolder, env.RootFilename + "-viewer.pdf");
        var scenes = DinkJson.ReadScenes(jsonContent);

        if (!GeneratePdfDoc(scenes, env.RootFilename, destFile))
            return false;

        Console.WriteLine($"Wrote {destFile}");

        if (!settings.Silent)
        {
            Console.WriteLine($"Opening PDF document...");
            SystemUtils.OpenUsingDefaultApp(destFile);
        }

        return true;
    }

    private static bool GeneratePdfDoc(List<DinkScene> scenes, string rootName, string destFile)
    {
        try
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(1, Unit.Inch);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Roboto"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(0);

                        // Title
                        column.Item().AlignCenter().Text($"Dink Script: {rootName}").Bold().FontSize(16);
                        
                        bool wasLastOperationASeparator = false;
                        bool isFirstScene = true;

                        foreach (var scene in scenes)
                        {
                            if (!isFirstScene)
                            {
                                if (!wasLastOperationASeparator) 
                                { 
                                    column.Item().PageBreak(); 
                                    wasLastOperationASeparator = true; 
                                }
                            }

                            // Scene Header
                            float before = isFirstScene ? 0 : 12;
                            isFirstScene = false;
                            
                            column.Item().PaddingTop(before).Text(string.IsNullOrEmpty(scene.SceneID) ? "Scene" : scene.SceneID).Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            wasLastOperationASeparator = false;

                            if (scene.Comments != null && scene.Comments.Count > 0)
                            {
                                AddComments(column, scene.Comments, 0);
                                wasLastOperationASeparator = false;
                            }

                            foreach (var block in scene.Blocks)
                            {
                                if (!wasLastOperationASeparator) { AddBlockSeparator(column); wasLastOperationASeparator = true; }

                                if (!string.IsNullOrEmpty(block.BlockID))
                                {
                                    column.Item().Text(block.BlockID).Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                                    wasLastOperationASeparator = false;
                                }

                                if (block.Comments != null && block.Comments.Count > 0)
                                {
                                    AddComments(column, block.Comments, 0);
                                    wasLastOperationASeparator = false;
                                }

                                var groupedSnippets = block.Snippets.GroupBy(s => s.Group > 0 ? s.Group.ToString() : s.SnippetID).ToList();

                                foreach (var group in groupedSnippets)
                                {
                                    if (!wasLastOperationASeparator) { AddGroupSeparator(column); wasLastOperationASeparator = true; }

                                    if (group.Count() > 1)
                                    {
                                        var first = group.First();
                                        column.Item().PaddingLeft(0.5f, Unit.Inch).Text($"Group ({first.GroupCount} snippets)").Italic();
                                        wasLastOperationASeparator = false;

                                        if (first.GroupComments != null)
                                        {
                                            AddComments(column, first.GroupComments, 0.5f);
                                            wasLastOperationASeparator = false;
                                        }
                                    }

                                    foreach (var snippet in group)
                                    {
                                        if (!wasLastOperationASeparator) { AddSnippetSeparator(column); wasLastOperationASeparator = true; }

                                        if (snippet.Comments != null && snippet.Comments.Count > 0)
                                        {
                                            AddComments(column, snippet.Comments, 0.5f);
                                            wasLastOperationASeparator = false;
                                        }

                                        foreach (var beat in snippet.Beats)
                                        {
                                            if (beat.Comments != null && beat.Comments.Count > 0)
                                            {
                                                AddComments(column, beat.Comments, 1.0f);
                                                wasLastOperationASeparator = false;
                                            }

                                            if (beat is DinkLine line)
                                            {
                                                // Character Name
                                                column.Item().PaddingLeft(2.5f, Unit.Inch)
                                                    .Text(line.CharacterID + (!string.IsNullOrEmpty(line.Qualifier) ? $" ({line.Qualifier})" : ""))
                                                    .FontFamily("Courier New");
                                                wasLastOperationASeparator = false;

                                                // Direction
                                                if (!string.IsNullOrEmpty(line.Direction))
                                                {
                                                    column.Item().PaddingLeft(2.0f, Unit.Inch)
                                                        .Text($"({line.Direction})")
                                                        .Italic().FontFamily("Courier New");
                                                    wasLastOperationASeparator = false;
                                                }

                                                // Dialogue
                                                column.Item().PaddingBottom(6).PaddingLeft(1.5f, Unit.Inch)
                                                    .Text(line.Text)
                                                    .FontFamily("Courier New");

                                                wasLastOperationASeparator = false;
                                            }
                                            else if (beat is DinkAction action)
                                            {
                                                column.Item().PaddingBottom(6).PaddingLeft(0.5f, Unit.Inch)
                                                    .Text(action.Text)
                                                    .FontFamily("Courier New");
                                                wasLastOperationASeparator = false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
                });
            })
            .GeneratePdf(destFile);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error generating PDF doc: {e.Message}");
            return false;
        }

        return true;
    }

    private static void AddComments(ColumnDescriptor column, List<string>? comments, float indentInches)
    {
        if (comments != null && comments.Count > 0)
        {
            foreach (var comment in comments)
            {
                column.Item().PaddingLeft(indentInches, Unit.Inch)
                    .Text($"// {comment}")
                    .Italic().FontColor(Colors.Grey.Medium);
            }
        }
    }

    private static void AddBlockSeparator(ColumnDescriptor column)
    {
        column.Item().PaddingBottom(12).LineHorizontal(1).LineColor(Colors.Grey.Darken2); // 12pt approx 240 twips
    }

    private static void AddGroupSeparator(ColumnDescriptor column)
    {
        column.Item().PaddingBottom(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
    }

    private static void AddSnippetSeparator(ColumnDescriptor column)
    {
        // Dotted line simulation or just a lighter line
        column.Item().PaddingBottom(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1); 
    }
}
