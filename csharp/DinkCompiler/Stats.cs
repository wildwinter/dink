namespace DinkCompiler;
using Dink;
using ClosedXML.Excel;

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

                WriteSceneSummary(rootName, dinkScenes, nonDinkLines, writingStatuses, audioStatuses,
                    workbook);

                if (characters != null)
                {
                    WriteCastSummary(rootName, voiceLines, writingStatuses, audioStatuses, characters, workbook);
                }

                WriteLineStats(rootName, inkStrings, writingStatuses, audioStatuses, workbook);

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

    private static void WriteSceneSummary(string rootName, List<DinkScene> dinkScenes, List<NonDinkLine> nonDinkLines, WritingStatuses writingStatuses, AudioStatuses audioStatuses, XLWorkbook workbook)
    {
        int writingStatusDefCount = writingStatuses.GetDefinitions().Count;
        int audioStatusDefCount = audioStatuses.GetDefinitions().Count;

        // ==========================
        //  Scenes

        int row = 1;

        var worksheet = workbook.Worksheets.Add("Scenes - " + rootName);
        ExcelUtils.FormatSheet(worksheet);
        row = 1;

        int wsColStart = 2;
        int wsColEnd = wsColStart + writingStatusDefCount;

        int asColStart = wsColEnd + 1;
        int asColEnd = asColStart + audioStatusDefCount;
        int originCol = asColEnd + 1;
        int lineEnd = originCol;

        worksheet.Cell(row, 1).Value = "Scene ID";

        int col = wsColStart;
        foreach (var def in writingStatuses.GetDefinitions())
        {
            worksheet.Cell(row, col).Value = "Writing\n" + def.Status;
            col++;
        }

        worksheet.Cell(row, wsColEnd).Value = "Writing\nTotal";

        col = asColStart;
        foreach (var def in audioStatuses.GetDefinitions())
        {
            worksheet.Cell(row, col).Value = "Audio\n" + def.Status;
            col++;
        }

        worksheet.Cell(row, asColEnd).Value = "Audio\nTotal";

        worksheet.Cell(row, originCol).Value = "Origin";

        ExcelUtils.FormatHeaderLine(worksheet.Cell(row, 1).AsRange());
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        row++;

        // Scene Writing State
        // Scene Recording State
        List<List<int>> wsLineCounts = new();
        List<List<bool>> wsLineEstimates = new();
        foreach (var scene in dinkScenes)
        {
            worksheet.Cell(row, 1).Value = scene.SceneID;
            ExcelUtils.FormatStatLine(worksheet.Cell(row, 1).AsRange());
            col = wsColEnd;

            GetSceneWritingCols(writingStatuses, scene, out List<int> wsCounts, out List<bool> wsEstimates);
            wsLineCounts.Add(wsCounts);
            wsLineEstimates.Add(wsEstimates);
            col = wsColStart;
            int wsIndex = 0;
            foreach (var def in audioStatuses.GetDefinitions())
            {
                worksheet.Cell(row, col).Value = wsCounts[wsIndex].ToString() + (wsEstimates[wsIndex] ? "?" : "");
                if (wsIndex > 0)
                {
                    if (wsCounts[wsIndex] > 0 && !string.IsNullOrEmpty(def.Color))
                        worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + def.Color);
                }
                wsIndex++;
                col++;
            }
            worksheet.Cell(row, wsColEnd).Value = wsCounts[wsIndex].ToString() + (wsEstimates[wsIndex] ? "?" : "");
            worksheet.Cell(row, wsColEnd).Style.Font.Bold = true;

            col = asColStart;
            foreach (var def in audioStatuses.GetDefinitions())
            {
                int count = audioStatuses.GetSceneTagCount(scene, def.Status);
                worksheet.Cell(row, col).Value = count;
                if (count > 0 && !string.IsNullOrEmpty(def.Color))
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + def.Color);
                col++;
            }
            worksheet.Cell(row, asColEnd).Value = audioStatuses.GetSceneTagCount(scene);
            worksheet.Cell(row, asColEnd).Style.Font.Bold = true;
            worksheet.Cell(row, originCol).Value = scene.Origin.ToString();
            row++;
        }

        worksheet.Cell(row, 1).Value = "Non-Dink";
        ExcelUtils.FormatStatLine(worksheet.Cell(row, 1).AsRange());
        worksheet.Range(row, 1, row, wsColEnd).Style.Font.Italic = true;

        col = wsColStart;
        List<int> wsNdCounts = new();
        List<bool> wsNdEstimates = new();
        foreach (var def in writingStatuses.GetDefinitions())
        {
            int count = writingStatuses.GetNonDinkTagCount(nonDinkLines, def.WsTag);
            worksheet.Cell(row, col).Value = count;
            wsNdCounts.Add(count);
            wsNdEstimates.Add(false);
            col++;
        }
        int ndCountTotal = writingStatuses.GetNonDinkTagCount(nonDinkLines);
        worksheet.Cell(row, wsColEnd).Value = ndCountTotal;
        worksheet.Cell(row, wsColEnd).Style.Font.Bold = true;
        wsNdCounts.Add(ndCountTotal);
        wsNdEstimates.Add(false);
        wsLineCounts.Add(wsNdCounts);
        wsLineEstimates.Add(wsNdEstimates);

        for (col = asColStart; col <= asColEnd; col++)
        {
            worksheet.Cell(row, col).Value = "-";
            worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.DarkGray;
        }

        row++;

        worksheet.Cell(row, 1).Value = "Totals";
        ExcelUtils.FormatHeaderLine(worksheet.Cell(row, 1).AsRange());
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        col = wsColStart;
        var wsCountTotals = wsLineCounts.Aggregate((prev, next) =>
             prev.Zip(next, (a, b) => a + b).ToList());
        var wsEstimateTotals = wsLineEstimates.Aggregate((prev, next) =>
             prev.Zip(next, (a, b) => a || b).ToList());

        for (int i = 0; i < wsCountTotals.Count; i++)
        {
            worksheet.Cell(row, col).Value = wsCountTotals[i].ToString() + (wsEstimateTotals[i] ? "?" : "");
            col++;
        }

        col = asColStart;
        foreach (var def in audioStatuses.GetDefinitions())
        {
            int count = audioStatuses.GetStatusCount(def.Status);
            worksheet.Cell(row, col).Value = count;
            col++;
        }
        worksheet.Cell(row, asColEnd).Value = audioStatuses.GetCount();
        worksheet.Range(row, 1, row, asColEnd).Style.Font.Bold = true;

        var table = worksheet.Range(1, 1, row, lineEnd).CreateTable();
        ExcelUtils.FormatTableSheet(worksheet, table, 2, false);

        IXLRange range = table.Range(1, wsColStart, row, wsColEnd);
        range.FirstColumn().Style.Border.LeftBorder = XLBorderStyleValues.Thick;
        range.LastColumn().Style.Border.RightBorder = XLBorderStyleValues.Thick;
        range = table.Range(1, wsColStart, row, asColEnd);
        range.LastColumn().Style.Border.RightBorder = XLBorderStyleValues.Thick;
        worksheet.FirstColumn().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        range = worksheet.Range(1, originCol, row, lineEnd);
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ExcelUtils.AdjustSheet(worksheet);
    }

    private static void GetSceneWritingCols(WritingStatuses writingStatuses, DinkScene scene, out List<int> counts, out List<bool> estimates)
    {
        int estimate = writingStatuses.GetSceneEstimate(scene);
        counts = new List<int>();
        estimates = new List<bool>();
        bool validCount = false;
        foreach(var def in writingStatuses.GetDefinitions())
        {
            int count = writingStatuses.GetSceneTagCount(scene,def.WsTag);
            counts.Add(count);
            estimates.Add(false);
            if (count>0 && !def.Estimate)
                validCount = true;
        }
        bool estimateMade = false;
        if (!validCount && estimate>0)
        {
            for (var i=writingStatuses.GetDefinitions().Count-1;i>=0;i--)
            {
                var def = writingStatuses.GetDefinitions()[i];
                if (estimateMade)
                {
                    counts[i]=0;
                }
                else if (def.Estimate && estimate>0)
                {
                    estimateMade = true;
                    counts[i]=estimate;
                    estimates[i]=true;
                }
            }
        }
        int total = writingStatuses.GetSceneTagCount(scene);
        if (estimateMade)
            total = estimate;
        counts.Add(total);
        estimates.Add(estimateMade);
    }

    private static void WriteCastSummary(string rootName, VoiceLines voiceLines, WritingStatuses writingStatuses, AudioStatuses audioStatuses, Characters characters, XLWorkbook workbook)
    {
        // ============================================================
        // -- Actors
        // Lines Recorded Per Character / Actor
        // Lines To Be Recorded Per Character / Actor
        IXLWorksheet worksheet = workbook.Worksheets.Add("Cast - " + rootName);
        ExcelUtils.FormatSheet(worksheet);
        int row = 1;

        worksheet.Cell(row, 1).Value = "Character";
        worksheet.Cell(row, 2).Value = "Actor";
        worksheet.Cell(row, 3).Value = "In Draft";
        worksheet.Cell(row, 4).Value = "Ready To Record";
        worksheet.Cell(row, 5).Value = "Recorded";
        worksheet.Cell(row, 6).Value = "Total";

        row++;

        foreach (var character in characters.OrderedEntries)
        {
            worksheet.Cell(row, 1).Value = character.ID;
            worksheet.Cell(row, 2).Value = character.Actor;

            List<string> characterLines = voiceLines.GetByCharacter(character.ID);
            int total = characterLines.Count;
            int recorded = audioStatuses.CountRecorded(characterLines);
            int readyToRecord = audioStatuses.CountReadyToRecord(writingStatuses, characterLines);
            int inDraft = audioStatuses.CountInDraft(writingStatuses, characterLines);

            worksheet.Cell(row, 3).Value = inDraft;
            worksheet.Cell(row, 4).Value = readyToRecord;
            worksheet.Cell(row, 5).Value = recorded;
            worksheet.Cell(row, 6).Value = total;
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            row++;
        }

        row--;
        IXLTable table = worksheet.Range(1, 1, row, 6).CreateTable();
        ExcelUtils.FormatStatLine(table.FirstColumn().AsRange());

        ExcelUtils.FormatTableSheet(worksheet, table);
        worksheet.FirstColumn().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ExcelUtils.AdjustSheet(worksheet);
    }

    private static void WriteLineStats(string rootName, LocStrings inkStrings, 
        WritingStatuses writingStatuses, AudioStatuses audioStatuses, XLWorkbook workbook)
    {
        int writingStatusDefCount = writingStatuses.GetDefinitions().Count;
        int audioStatusDefCount = audioStatuses.GetDefinitions().Count;
        // ============================================================
        // -- Line Status
        // Each line, writing status, recording status

        IXLWorksheet worksheet = workbook.Worksheets.Add("Lines - " + rootName);
        ExcelUtils.FormatSheet(worksheet);
        int row = 1;

        int wsColStart = 2;
        int wsColEnd = wsColStart + writingStatusDefCount - 1;

        int asColStart = wsColEnd + 1;
        int asColEnd = asColStart + audioStatusDefCount - 1;

        int originCol = asColEnd + 1;
        int textCol = asColEnd + 2;
        int lineEnd = textCol;

        worksheet.Cell(row, 1).Value = "Line ID";

        int col = wsColStart;
        foreach (var def in writingStatuses.GetDefinitions())
        {
            worksheet.Cell(row, col).Value = "Writing\n" + def.Status;
            col++;
        }

        col = asColStart;
        foreach (var def in audioStatuses.GetDefinitions())
        {
            worksheet.Cell(row, col).Value = "Audio\n" + def.Status;
            col++;
        }

        ExcelUtils.FormatHeaderLine(worksheet.Cell(row, 1).AsRange());
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        worksheet.Cell(row, textCol).Value = "Text";
        worksheet.Cell(row, originCol).Value = "Origin";
        ExcelUtils.FormatHeaderLine(worksheet.Range(row, textCol, row, originCol));

        row++;

        var lines = inkStrings.OrderedEntries.ToList();

        foreach (var line in lines)
        {
            worksheet.Cell(row, 1).Value = line.ID;
            ExcelUtils.FormatStatLine(worksheet.Cell(row, 1).AsRange());
            col = wsColStart;
            var writingStatus = writingStatuses.GetStatus(line.ID);
            foreach (var def in writingStatuses.GetDefinitions())
            {
                if (writingStatus.Status == def.Status)
                {
                    worksheet.Cell(row, col).Value = "X";
                    if (!string.IsNullOrEmpty(def.Color))
                        worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + def.Color);
                }
                col++;
            }
            col = asColStart;
            var audioStatus = audioStatuses.GetStatus(line.ID);
            foreach (var def in audioStatuses.GetDefinitions())
            {
                if (!line.IsDink)
                {
                    worksheet.Cell(row, col).Value = "-";
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.DarkGray;
                    col++;
                    continue;
                }
                if (audioStatus.Status == def.Status)
                {
                    worksheet.Cell(row, col).Value = "X";
                    if (!string.IsNullOrEmpty(def.Color))
                        worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + def.Color);
                }
                col++;
            }

            worksheet.Cell(row, originCol).Value = line.Origin.ToString();
            worksheet.Cell(row, textCol).Value = line.Text;

            row++;
        }

        row--;

        IXLTable table = worksheet.Range(1, 1, row, lineEnd).CreateTable();
        ExcelUtils.FormatTableSheet(worksheet, table, 2, false);
        worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        IXLRange range = worksheet.Range(1, wsColStart, row, wsColEnd);
        range.FirstColumn().Style.Border.LeftBorder = XLBorderStyleValues.Thick;
        range.LastColumn().Style.Border.RightBorder = XLBorderStyleValues.Thick;
        range = worksheet.Range(1, asColStart, row, asColEnd);
        range.LastColumn().Style.Border.RightBorder = XLBorderStyleValues.Thick;
        worksheet.FirstColumn().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        range = worksheet.Range(1, originCol, row, lineEnd);
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        ExcelUtils.AdjustSheet(worksheet);
    }
}