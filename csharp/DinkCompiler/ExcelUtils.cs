namespace DinkCompiler;

using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Linq;
using System.Collections.Generic;

public class ExcelUtils 
{
    public static string? FindColumnByHeading(IXLWorksheet worksheet, string headingText)
    {
        var firstRow = worksheet.Row(1);
        int lastColumn = worksheet.LastCellUsed()?.Address.ColumnNumber ?? 1;
        for (int col = 1; col <= lastColumn; col++)
        {
            var cell = firstRow.Cell(col);
            
            if (cell.TryGetValue<string>(out string cellValue) && 
                !string.IsNullOrWhiteSpace(cellValue) &&
                cellValue==headingText)
            {
                return cell.Address.ColumnLetter;
            }
        }
        return null; 
    }

    public static void FormatSheet(IXLWorksheet worksheet, bool text=true)
    {
        worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        if (text)
            worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        else
            worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    public static void AdjustSheet(IXLWorksheet worksheet)
    {
        worksheet.ColumnsUsed().AdjustToContents();
        worksheet.RowsUsed().AdjustToContents();
    }

    public static void FormatTableSheet(IXLWorksheet worksheet, IXLTable table, int freeze=1, bool text=true)
    {
        FormatSheet(worksheet, text);
        worksheet.SheetView.FreezeRows(freeze);
        table.ShowAutoFilter = true;
        FormatHeaderLine(table.FirstRow().AsRange());
    }

    public static void FormatStatBlock(IXLRange range)
    {
        FormatStatLine(range.FirstColumn().AsRange());
        FormatHeaderLine(range.FirstRow().AsRange());
        range.LastRow().Style.Font.Bold = true;
    }

    public static void FormatStatLine(IXLRange range)
    {
        range.Style.Fill.BackgroundColor = XLColor.LightGreen;
        range.Style.Font.Bold = false;
        range.Style.Font.FontColor = XLColor.Black;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    public static void FormatHeaderLine(IXLRange range)
    {
        range.Style.Fill.BackgroundColor = XLColor.DarkGreen;
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    public static void SuppressNumberStoredAsTextWarning(string filePath, string sheetName)
    {
        using (var doc = SpreadsheetDocument.Open(filePath, true))
        {
            var workbookPart = doc.WorkbookPart;
            if (workbookPart==null)
                return;

            var sheet = workbookPart.Workbook.Descendants<Sheet>()
                .FirstOrDefault(s => s.Name == sheetName);

            if (sheet == null) return;

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var worksheet = worksheetPart.Worksheet;

            // Check if IgnoredErrors already exists
            if (worksheet.GetFirstChild<IgnoredErrors>() == null)
            {
                var ignoredErrors = new IgnoredErrors();

                var elementsThatMustComeAfter = new HashSet<string>
                {
                    "smartTags",
                    "drawing",               // Charts/Shapes
                    "legacyDrawing",         // Comments often use this
                    "legacyDrawingHF",       // Header/Footer images
                    "picture",
                    "oleObjects",
                    "controls",
                    "webPublishItems",
                    "tableParts",            // Excel Tables
                    "extLst"                 // Future extensions
                };

                OpenXmlElement? refElement = null;

                // Find the first child element that matches our list
                foreach (var child in worksheet.Elements())
                {
                    if (elementsThatMustComeAfter.Contains(child.LocalName))
                    {
                        refElement = child;
                        break;
                    }
                }

                if (refElement != null)
                {
                    worksheet.InsertBefore(ignoredErrors, refElement);
                }
                else
                {
                    // If no drawings/tables exist, safe to append to the end 
                    // (This places it after SheetData, MergeCells, PageSetup, etc.)
                    worksheet.Append(ignoredErrors);
                }

                // Create the rule to ignore the error
                var ignoredError = new IgnoredError()
                {
                    NumberStoredAsText = true,
                    // A1 to the last possible cell in Excel
                    SequenceOfReferences = new ListValue<StringValue>() { InnerText = "A1:XFD1048576" }
                };

                ignoredErrors.Append(ignoredError);
                worksheet.Save();
            }
        }
    }
}