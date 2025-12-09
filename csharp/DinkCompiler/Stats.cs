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
            // Optimization: Disable event tracking for faster massive writes
            using (var workbook = new XLWorkbook())
            {
                WriteSceneSummary(rootName, dinkScenes, nonDinkLines, writingStatuses, audioStatuses,
                    workbook);

                if (characters != null)
                {
                    WriteCastSummary(rootName, voiceLines, writingStatuses, audioStatuses, characters, workbook);
                }

                WriteLineStats(rootName, inkStrings, voiceLines, writingStatuses, audioStatuses, workbook);

                workbook.SaveAs(destStatsFile);

                ExcelUtils.SuppressNumberStoredAsTextWarning(destStatsFile, "Scenes - " + rootName);
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
        // Cache definitions to avoid repeated getter calls
        var wsDefs = writingStatuses.GetDefinitions();
        var asDefs = audioStatuses.GetDefinitions();
        int writingStatusDefCount = wsDefs.Count;
        int audioStatusDefCount = asDefs.Count;

        // Cache colors
        var wsColors = wsDefs.Select(d => !string.IsNullOrEmpty(d.Color) ? XLColor.FromHtml("#" + d.Color) : null).ToList();
        var asColors = asDefs.Select(d => !string.IsNullOrEmpty(d.Color) ? XLColor.FromHtml("#" + d.Color) : null).ToList();

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
        foreach (var def in wsDefs)
        {
            worksheet.Cell(row, col).Value = "Writing\n" + def.Status;
            col++;
        }

        worksheet.Cell(row, wsColEnd).Value = "Writing\nTotal";

        col = asColStart;
        foreach (var def in asDefs)
        {
            worksheet.Cell(row, col).Value = "Audio\n" + def.Status;
            col++;
        }

        worksheet.Cell(row, asColEnd).Value = "Audio\nTotal";
        worksheet.Cell(row, originCol).Value = "Origin";

        ExcelUtils.FormatHeaderLine(worksheet.Cell(row, 1).AsRange());
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        row++;

        // Initialise Totals arrays
        int[] totalWsCounts = new int[writingStatusDefCount + 1]; // +1 for the total column
        bool[] totalWsEstimates = new bool[writingStatusDefCount + 1];
        int[] totalAsCounts = new int[audioStatusDefCount];

        foreach (var scene in dinkScenes)
        {
            worksheet.Cell(row, 1).Value = scene.SceneID;
            ExcelUtils.FormatStatLine(worksheet.Cell(row, 1).AsRange());

            // Writing Statuses
            GetSceneWritingCols(writingStatuses, scene, out List<int> wsCounts, out List<bool> wsEstimates);
            
            col = wsColStart;
            for (int i = 0; i < wsCounts.Count; i++)
            {
                // Update totals
                totalWsCounts[i] += wsCounts[i];
                if (wsEstimates[i]) totalWsEstimates[i] = true;

                // Write Cell
                string cellText = wsCounts[i].ToString() + (wsEstimates[i] ? "?" : "");
                worksheet.Cell(row, col).Value = cellText;

                // Apply Color (skip total column which is the last index)
                if (i < wsColors.Count && wsCounts[i] > 0 && wsColors[i] != null)
                {
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = wsColors[i];
                }
                col++;
            }
            
            worksheet.Cell(row, wsColEnd).Style.Font.Bold = true;

            // Audio Statuses
            col = asColStart;
            for(int i = 0; i < asDefs.Count; i++)
            {
                var def = asDefs[i];
                int count = audioStatuses.GetSceneTagCount(scene, def.Status);
                totalAsCounts[i] += count;

                worksheet.Cell(row, col).Value = count;
                if (count > 0 && asColors[i] != null)
                    worksheet.Cell(row, col).Style.Fill.BackgroundColor = asColors[i];
                col++;
            }

            worksheet.Cell(row, asColEnd).Value = audioStatuses.GetSceneTagCount(scene);
            worksheet.Cell(row, asColEnd).Style.Font.Bold = true;
            worksheet.Cell(row, originCol).Value = scene.Origin.ToString();
            row++;
        }

        // Non-Dink Logic
        worksheet.Cell(row, 1).Value = "Non-Dink";
        ExcelUtils.FormatStatLine(worksheet.Cell(row, 1).AsRange());
        worksheet.Range(row, 1, row, wsColEnd).Style.Font.Italic = true;

        col = wsColStart;
        int ndCountTotal = 0;
        for (int i = 0; i < wsDefs.Count; i++)
        {
            int count = writingStatuses.GetNonDinkTagCount(nonDinkLines, wsDefs[i].WsTag);
            worksheet.Cell(row, col).Value = count;
            
            // Add to totals (using index i, same as main loop)
            totalWsCounts[i] += count;
            ndCountTotal += count;
            col++;
        }
        
        // Non-Dink Total Column
        worksheet.Cell(row, wsColEnd).Value = ndCountTotal;
        worksheet.Cell(row, wsColEnd).Style.Font.Bold = true;
        totalWsCounts[wsDefs.Count] += ndCountTotal; // Add to the Grand Total accumulator

        for (col = asColStart; col <= asColEnd; col++)
        {
            worksheet.Cell(row, col).Value = "-";
            worksheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.DarkGray;
        }

        row++;

        // ==========================
        // Totals Row
        worksheet.Cell(row, 1).Value = "Totals";
        ExcelUtils.FormatHeaderLine(worksheet.Cell(row, 1).AsRange());
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        col = wsColStart;
        for (int i = 0; i < totalWsCounts.Length; i++)
        {
            worksheet.Cell(row, col).Value = totalWsCounts[i].ToString() + (totalWsEstimates[i] ? "?" : "");
            col++;
        }

        col = asColStart;
        for(int i = 0; i < totalAsCounts.Length; i++)
        {
            worksheet.Cell(row, col).Value = totalAsCounts[i];
            col++;
        }
        
        // Grand total of audio
        worksheet.Cell(row, asColEnd).Value = audioStatuses.GetCount();
        worksheet.Range(row, 1, row, asColEnd).Style.Font.Bold = true;

        // Formatting
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
        var definitions = writingStatuses.GetDefinitions();
        
        counts = new List<int>(definitions.Count + 1);
        estimates = new List<bool>(definitions.Count + 1);
        
        bool validCount = false;
        
        // First pass: collect actual counts
        foreach(var def in definitions)
        {
            int count = writingStatuses.GetSceneTagCount(scene, def.WsTag);
            counts.Add(count);
            estimates.Add(false);
            if (count > 0 && !def.Estimate)
                validCount = true;
        }
        
        // Handle Estimates
        bool estimateMade = false;
        if (!validCount && estimate > 0)
        {
            for (var i = definitions.Count - 1; i >= 0; i--)
            {
                var def = definitions[i];
                if (estimateMade)
                {
                    counts[i] = 0;
                }
                else if (def.Estimate && estimate>0)
                {
                    estimateMade = true;
                    counts[i] = estimate;
                    estimates[i] = true;
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
        VoiceLines voiceLines, WritingStatuses writingStatuses, 
        AudioStatuses audioStatuses, XLWorkbook workbook)
    {
        var wsDefs = writingStatuses.GetDefinitions();
        var asDefs = audioStatuses.GetDefinitions();
        
        int writingStatusDefCount = wsDefs.Count;
        int audioStatusDefCount = asDefs.Count;

        var wsStatusToColOffset = new Dictionary<string, int>();
        var wsStatusColor = new Dictionary<string, XLColor>();
        for (int i = 0; i < wsDefs.Count; i++) {
            wsStatusToColOffset[wsDefs[i].Status] = i;
            if (!string.IsNullOrEmpty(wsDefs[i].Color))
                wsStatusColor[wsDefs[i].Status] = XLColor.FromHtml("#" + wsDefs[i].Color);
        }

        var asStatusToColOffset = new Dictionary<string, int>();
        var asStatusColor = new Dictionary<string, XLColor>();
        for (int i = 0; i < asDefs.Count; i++) {
            asStatusToColOffset[asDefs[i].Status] = i;
            if (!string.IsNullOrEmpty(asDefs[i].Color))
                asStatusColor[asDefs[i].Status] = XLColor.FromHtml("#" + asDefs[i].Color);
        }

        IXLWorksheet worksheet = workbook.Worksheets.Add("Lines - " + rootName);
        ExcelUtils.FormatSheet(worksheet);
        int row = 1;

        int wsColStart = 2;
        int wsColEnd = wsColStart + writingStatusDefCount - 1;

        int asColStart = wsColEnd + 1;
        int asColEnd = asColStart + audioStatusDefCount - 1;

        int originCol = asColEnd + 1;
        int charCol = asColEnd + 2;
        int textCol = asColEnd + 3;
        int lineEnd = textCol;

        worksheet.Cell(row, 1).Value = "Line ID";

        int col = wsColStart;
        foreach (var def in wsDefs)
        {
            worksheet.Cell(row, col).Value = "Writing\n" + def.Status;
            col++;
        }

        col = asColStart;
        foreach (var def in asDefs)
        {
            worksheet.Cell(row, col).Value = "Audio\n" + def.Status;
            col++;
        }

        ExcelUtils.FormatHeaderLine(worksheet.Cell(row, 1).AsRange());
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        worksheet.Cell(row, originCol).Value = "Origin";
        worksheet.Cell(row, charCol).Value = "Character";
        worksheet.Cell(row, textCol).Value = "Text";

        ExcelUtils.FormatHeaderLine(worksheet.Range(row, originCol, row, textCol));

        row++;

        var lines = inkStrings.OrderedEntries.ToList();

        foreach (var line in lines)
        {
            worksheet.Cell(row, 1).Value = line.ID;
            ExcelUtils.FormatStatLine(worksheet.Cell(row, 1).AsRange());

            // Handle Writing Status
            var wStatus = writingStatuses.GetStatus(line.ID);
            if (wsStatusToColOffset.TryGetValue(wStatus.Status, out int wOffset))
            {
                int targetCol = wsColStart + wOffset;
                worksheet.Cell(row, targetCol).Value = "X";
                if (wsStatusColor.TryGetValue(wStatus.Status, out XLColor? color))
                {
                    worksheet.Cell(row, targetCol).Style.Fill.BackgroundColor = color;
                }
            }

            // Handle Audio Status
            if (!line.IsDink)
            {
                // Bulk paint the non-dink area for this row
                worksheet.Range(row, asColStart, row, asColEnd).Value = "-";
                worksheet.Range(row, asColStart, row, asColEnd).Style.Fill.BackgroundColor = XLColor.DarkGray;
            }
            else
            {
                var aStatus = audioStatuses.GetStatus(line.ID);
                if (asStatusToColOffset.TryGetValue(aStatus.Status, out int aOffset))
                {
                    int targetCol = asColStart + aOffset;
                    worksheet.Cell(row, targetCol).Value = "X";
                    if (asStatusColor.TryGetValue(aStatus.Status, out XLColor? color))
                    {
                        worksheet.Cell(row, targetCol).Style.Fill.BackgroundColor = color;
                    }
                }
            }

            // Metadata
            worksheet.Cell(row, originCol).Value = line.Origin.ToString();
            
            bool hasCharacter = false;
            if (line.IsDink)
            {
                VoiceEntry? entry = voiceLines.GetEntry(line.ID);
                if (entry != null)
                {
                    worksheet.Cell(row, charCol).Value = entry?.Character;
                    hasCharacter = true;
                }
            }
            if (!hasCharacter)
            {
                worksheet.Cell(row, charCol).Value = "-";
                worksheet.Cell(row, charCol).Style.Fill.BackgroundColor = XLColor.DarkGray;
            }
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