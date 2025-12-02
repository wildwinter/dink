namespace DinkCompiler;
using Dink;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;

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
        try
        {
            using (var workbook = new XLWorkbook())
            {
                int row=1;

                var worksheet = workbook.Worksheets.Add("Summary - " + rootName);

                // -- Overall

                // Total Words

                // Total Lines
                int totalLines = inkStrings.Count;
                worksheet.Cell(row,1).Value="Total Lines";
                worksheet.Cell(row,2).Value=totalLines;
                row++;

                // Lines at Each Writing Status
                foreach (var statusDef in writingStatuses.GetDefinitions())
                {
                    int statusCount = writingStatuses.GetTagCount(statusDef.WsTag);
                    worksheet.Cell(row,1).Value=$"Lines at Writing Status {statusDef.Status}";
                    worksheet.Cell(row,2).Value=statusCount;
                    row++;
                }

                row++;

                // Total Dialogue Lines
                int totalVoiceLines = voiceLines.Count;
                worksheet.Cell(row,1).Value="Total Dialogue Lines";
                worksheet.Cell(row,2).Value=totalVoiceLines;
                row++;
                // Lines at Each Recording Status
                foreach (var statusDef in audioStatuses.GetDefinitions())
                {
                    int statusCount = audioStatuses.GetStatusCount(statusDef.Status);
                    worksheet.Cell(row,1).Value=$"Lines at Audio Status {statusDef.Status}";
                    worksheet.Cell(row,2).Value=statusCount;
                    row++;
                }

                // -- Scenes
                
                // Scene Writing State
                // Scene Recording State

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