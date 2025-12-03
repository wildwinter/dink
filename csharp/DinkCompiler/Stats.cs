namespace DinkCompiler;
using Dink;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.ExtendedProperties;

class Stats
{
    public static bool WriteExcelFile(string rootName,
            List<DinkScene> dinkScenes, 
            List<NonDinkLine> nonDinkLines, 
            LocStrings inkStrings, VoiceLines voiceLines, 
            WritingStatuses writingStatuses, 
            AudioStatuses audioStatuses,
            Characters? characters,
            string destStatsFile)
    {
        Console.WriteLine("Writing stats file: " + destStatsFile);
        try
        {
            using (var workbook = new XLWorkbook())
            {
                int writingStatusDefCount = writingStatuses.GetDefinitions().Count; 
                int audioStatusDefCount = audioStatuses.GetDefinitions().Count;

                {
                    // ==========================
                    //  Scenes

                    int row=1;

                    var worksheet = workbook.Worksheets.Add("Scenes - " + rootName);
                    ExcelUtils.FormatSheet(worksheet);
                    row = 1;

                    worksheet.Cell(1,1).Value=" ";

                    int wsColStart = 2;
                    int wsColEnd = wsColStart+writingStatusDefCount;

                    worksheet.Cell(row,wsColStart).Value = "Writing Status";
                    worksheet.Range(1,wsColStart,1,wsColEnd).Merge();

                    int asColStart = wsColEnd+1;
                    int asColEnd = asColStart+audioStatusDefCount;

                    worksheet.Cell(row,asColStart).Value = "Audio Status";
                    worksheet.Range(1,asColStart,1,asColEnd).Merge();

                    row++;
                    worksheet.Cell(row,1).Value = "Scene";
                    
                    int col=wsColEnd-1;
                    foreach (var def in writingStatuses.GetDefinitions())
                    {
                        worksheet.Cell(row, col).Value = def.Status;
                        if (!string.IsNullOrEmpty(def.Color))
                            worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#"+def.Color);
                        col--;
                    }

                    worksheet.Cell(row, wsColEnd).Value="Total";
                    worksheet.Cell(row, wsColEnd).Style.Font.Bold = true;

                    col = asColEnd-1;
                    foreach (var def in audioStatuses.GetDefinitions())
                    {
                        worksheet.Cell(row, col).Value = def.Status;
                        if (!string.IsNullOrEmpty(def.Color))
                            worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#"+def.Color);
                        col--;
                    }
                    
                    worksheet.Cell(row, asColEnd).Value="Total";
                    worksheet.Cell(row, asColEnd).Style.Font.Bold = true;

                    ExcelUtils.FormatHeaderLine(worksheet.Cell(row,1).AsRange());
                    worksheet.Cell(row,1).Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Right;
                    
                    row++;

                    // Scene Writing State
                    // Scene Recording State
                    foreach(var scene in dinkScenes)
                    {
                        worksheet.Cell(row,1).Value = scene.SceneID;
                        ExcelUtils.FormatStatLine(worksheet.Cell(row,1).AsRange());
                        col=wsColEnd-1;
                        foreach (var def in writingStatuses.GetDefinitions())
                        {
                            int count = writingStatuses.GetSceneTagCount(scene, def.WsTag);
                            worksheet.Cell(row, col).Value = count;
                            col--;
                        }
                        worksheet.Cell(row,wsColEnd).Value = writingStatuses.GetSceneTagCount(scene);
                        worksheet.Cell(row,wsColEnd).Style.Font.Bold = true;
                        col=asColEnd-1;
                        foreach (var def in audioStatuses.GetDefinitions())
                        {
                            int count = audioStatuses.GetSceneTagCount(scene, def.Status);
                            worksheet.Cell(row, col).Value = count;
                            col--;
                        }
                        worksheet.Cell(row,asColEnd).Value = audioStatuses.GetSceneTagCount(scene);
                        worksheet.Cell(row,asColEnd).Style.Font.Bold = true;
                        row++;
                    }

                    worksheet.Cell(row,1).Value = "Non-Dink";
                    ExcelUtils.FormatStatLine(worksheet.Cell(row,1).AsRange());
                    worksheet.Range(row,1,row,wsColEnd).Style.Font.Italic = true;

                    col=wsColEnd-1;
                    foreach (var def in writingStatuses.GetDefinitions())
                    {
                        int count = writingStatuses.GetNonDinkTagCount(nonDinkLines, def.WsTag);
                        worksheet.Cell(row, col).Value = count;
                        col--;
                    }
                    worksheet.Cell(row,wsColEnd).Value = writingStatuses.GetNonDinkTagCount(nonDinkLines);
                    worksheet.Cell(row,wsColEnd).Style.Font.Bold = true;
                    row++;

                    worksheet.Cell(row,1).Value = "Totals";
                    ExcelUtils.FormatHeaderLine(worksheet.Cell(row,1).AsRange());
                    worksheet.Cell(row,1).Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Right;
    
                    col=wsColEnd-1;
                    foreach (var def in writingStatuses.GetDefinitions())
                    {
                        int count = writingStatuses.GetTagCount(def.WsTag);
                        worksheet.Cell(row, col).Value = count;
                        col--;
                    }
                    worksheet.Cell(row,wsColEnd).Value = writingStatuses.GetCount();
                    col=asColEnd-1;
                    foreach (var def in audioStatuses.GetDefinitions())
                    {
                        int count = audioStatuses.GetStatusCount(def.Status);
                        worksheet.Cell(row, col).Value = count;
                        col--;
                    }
                    worksheet.Cell(row,asColEnd).Value = audioStatuses.GetCount();
                    worksheet.Range(row,1,row, asColEnd).Style.Font.Bold = true;

                    var table = worksheet.Range(1,1,row,asColEnd).CreateTable("SceneTable");
                    ExcelUtils.FormatTableSheet(worksheet, table, 2, false);
                    
                    IXLRange range = table.Range(1,wsColStart,row,wsColEnd);
                    range.FirstColumn().Style.Border.LeftBorder = XLBorderStyleValues.Thick;
                    range.LastColumn().Style.Border.RightBorder = XLBorderStyleValues.Thick;
                    worksheet.FirstColumn().Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Right;

                    ExcelUtils.AdjustSheet(worksheet);
                }

                if (characters!=null)
                {
                    // ============================================================
                    // -- Actors
                    // Lines Recorded Per Character / Actor
                    // Lines To Be Recorded Per Character / Actor
                    IXLWorksheet worksheet = workbook.Worksheets.Add("Cast - " + rootName);
                    ExcelUtils.FormatSheet(worksheet);
                    int row = 1;

                    worksheet.Cell(row, 1).Value="Character";
                    worksheet.Cell(row, 2).Value="Actor";
                    worksheet.Cell(row, 3).Value="In Draft";
                    worksheet.Cell(row, 4).Value="Ready To Record";
                    worksheet.Cell(row, 5).Value="Recorded";
                    worksheet.Cell(row, 6).Value="Total";

                    row++;

                    foreach(var character in characters.OrderedEntries)
                    {
                        worksheet.Cell(row, 1).Value=character.ID;
                        worksheet.Cell(row, 2).Value=character.Actor;

                        List<string> characterLines = voiceLines.GetByCharacter(character.ID);
                        int total = characterLines.Count;
                        int recorded = audioStatuses.CountRecorded(characterLines);
                        int readyToRecord = audioStatuses.CountReadyToRecord(writingStatuses,characterLines);
                        int inDraft = audioStatuses.CountInDraft(writingStatuses,characterLines);

                        worksheet.Cell(row, 3).Value = inDraft;
                        worksheet.Cell(row, 4).Value = readyToRecord;
                        worksheet.Cell(row, 5).Value = recorded;
                        worksheet.Cell(row, 6).Value = total;
                        worksheet.Cell(row, 6).Style.Font.Bold = true;
                        row++;
                    }

                    row--;
                    IXLTable table = worksheet.Range(1,1,row,6).CreateTable("CastTable");
                    ExcelUtils.FormatStatLine(table.FirstColumn().AsRange());

                    ExcelUtils.FormatTableSheet(worksheet, table);
                    worksheet.FirstColumn().Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Right;

                    ExcelUtils.AdjustSheet(worksheet);
                }
            
                {
                    // ============================================================
                    // -- Line Status
                    // Each line, writing status, recording status

                    IXLWorksheet worksheet = workbook.Worksheets.Add("Lines - " + rootName);
                    ExcelUtils.FormatSheet(worksheet);
                    int row = 1;

                    worksheet.Cell(1,1).Value=" ";

                    int wsColStart = 2;
                    int wsColEnd = wsColStart+writingStatusDefCount-1;

                    worksheet.Cell(row,wsColStart).Value = "Writing Status";
                    worksheet.Range(1,wsColStart,1,wsColEnd).Merge();

                    int asColStart = wsColEnd+1;
                    int asColEnd = asColStart+audioStatusDefCount-1;

                    worksheet.Cell(row,asColStart).Value = "Audio Status";
                    worksheet.Range(1,asColStart,1,asColEnd).Merge();

                    row++;
                    worksheet.Cell(row,1).Value = "Line";
                    
                    int col=wsColEnd;
                    foreach (var def in writingStatuses.GetDefinitions())
                    {
                        worksheet.Cell(row, col).Value = def.Status;
                        if (!string.IsNullOrEmpty(def.Color))
                            worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#"+def.Color);
                        col--;
                    }

                    col = asColEnd;
                    foreach (var def in audioStatuses.GetDefinitions())
                    {
                        worksheet.Cell(row, col).Value = def.Status;
                        if (!string.IsNullOrEmpty(def.Color))
                            worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#"+def.Color);
                        col--;
                    }

                    ExcelUtils.FormatHeaderLine(worksheet.Cell(row,1).AsRange());
                    worksheet.Cell(row,1).Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Right;
                    
                    row++;

                    var lines = inkStrings.OrderedEntries.ToList();

                    foreach(var line in lines)
                    {
                        worksheet.Cell(row,1).Value = line.ID;
                        ExcelUtils.FormatStatLine(worksheet.Cell(row,1).AsRange());
                        col=wsColEnd;
                        var writingStatus = writingStatuses.GetStatus(line.ID);
                        foreach (var def in writingStatuses.GetDefinitions())
                        {
                            if (writingStatus.Status == def.Status)
                            {
                                worksheet.Cell(row, col).Value = "X";
                                if (!string.IsNullOrEmpty(def.Color))
                                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#"+def.Color);
                            }
                            col--;
                        }
                        col=asColEnd;
                        var audioStatus = audioStatuses.GetStatus(line.ID);
                        foreach (var def in audioStatuses.GetDefinitions())
                        {
                            if (audioStatus.Status == def.Status)
                            {
                                worksheet.Cell(row, col).Value = "X";
                                if (!string.IsNullOrEmpty(def.Color))
                                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#"+def.Color);
                            }
                            col--;
                        }
                        row++;
                    }

                    row--;

                    IXLTable table = worksheet.Range(1,1,row,asColEnd).CreateTable("LineTable");
                    ExcelUtils.FormatTableSheet(worksheet, table, 2, false);
                    worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    
                    IXLRange range = table.Range(1,wsColStart,row,wsColEnd);
                    range.FirstColumn().Style.Border.LeftBorder = XLBorderStyleValues.Thick;
                    range.LastColumn().Style.Border.RightBorder = XLBorderStyleValues.Thick;
                    worksheet.FirstColumn().Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Right;

                    ExcelUtils.AdjustSheet(worksheet);
                }

                // ===================================
                workbook.SaveAs(destStatsFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out stats Excel file {destStatsFile}: " + ex.Message);
            return false;
        }
        return true;
    }
}