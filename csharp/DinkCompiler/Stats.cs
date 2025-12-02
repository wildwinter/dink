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
            string destStatsFile)
    {
        Console.WriteLine("Writing stats file: " + destStatsFile);
        try
        {
            using (var workbook = new XLWorkbook())
            {
                int row=1;

                // -- Scenes

                var worksheet = workbook.Worksheets.Add("Scenes - " + rootName);
                ExcelUtils.FormatSheet(worksheet);
                row = 1;

                worksheet.Cell(1,1).Value=" ";

                int writingStatusDefCount = writingStatuses.GetDefinitions().Count;
                
                int wsColStart = 2;
                int wsColEnd = wsColStart+writingStatusDefCount;

                worksheet.Cell(row,wsColStart).Value = "Writing Status";
                worksheet.Range(1,wsColStart,1,wsColEnd).Merge();

                int audioStatusDefCount = audioStatuses.GetDefinitions().Count;
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
                var scenes = dinkScenes.ToList();
                scenes.Sort((a, b) => 
                {
                    return a.SceneID.CompareTo(b.SceneID);
                });

                foreach(var scene in scenes)
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

                // -- Actors
                // Lines Recorded Per Character / Actor
                // Lines To Be Recorded Per Character / Actor

                // -- Line Status
                // Each line, writing status, recording status

                
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