// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

using System.Text;
using ClosedXML.Excel;
using DinkCompiler;
using DinkTool;

/// <summary>
/// End-to-end tests for the re-record feature: a line flagged in rerecord.json
/// that is actually "Recorded" should appear as "Re-record" in the recording
/// script and be counted separately in the cast stats - but only when it is
/// genuinely recorded (a flag on a not-yet-recorded line is ignored).
/// </summary>
// Shares the VCIntegration collection so it doesn't run in parallel with the
// other console-capturing suite (IncrementalBuildTests) - both redirect the
// global Console.Out, which races under xUnit's default class parallelism.
[Collection("VCIntegration")]
public class ReRecordTests : IDisposable
{
    private readonly string _dir;
    // A real dink dialogue line from the test1 fixture (FRED).
    private const string RecordedLineId = "scene1_Scene1_Part1_S494";

    public ReRecordTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "dink-rerecord-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    private static string ThisDir([System.Runtime.CompilerServices.CallerFilePath] string p = "") =>
        Path.GetDirectoryName(p)!;

    /// <summary>
    /// Lays out a temp project: test1 ink + characters, a "Recorded" audio
    /// folder containing an audio file for RecordedLineId, and optionally a
    /// rerecord.json. Returns configured ProjectSettings.
    /// </summary>
    private ProjectSettings SetupProject(bool flagLineForReRecord)
    {
        var testDataDir = Path.GetFullPath(Path.Combine(ThisDir(), "../../tests/test1"));
        var sourceDir = Path.Combine(_dir, "source");
        var outputDir = Path.Combine(_dir, "output");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);

        foreach (var file in Directory.GetFiles(testDataDir, "*.ink"))
            File.Copy(file, Path.Combine(sourceDir, Path.GetFileName(file)), overwrite: true);
        File.Copy(Path.Combine(testDataDir, "characters.json"),
                  Path.Combine(sourceDir, "characters.json"), overwrite: true);

        // A "Recorded" audio folder containing a file named after the line.
        var recordedFolder = Path.Combine(sourceDir, "Audio", "Recorded");
        Directory.CreateDirectory(recordedFolder);
        File.WriteAllBytes(Path.Combine(recordedFolder, RecordedLineId + ".wav"), new byte[] { 0 });

        if (flagLineForReRecord)
            File.WriteAllText(Path.Combine(sourceDir, "rerecord.json"),
                              "[ \"" + RecordedLineId + "\" ]");

        return new ProjectSettings
        {
            Source = Path.Combine(sourceDir, "main.ink"),
            DestFolder = outputDir,
            OutputRecordingScript = true,
            OutputStats = true,
            IgnoreWritingStatus = true,
            AudioStatus = new List<AudioStatusDefinition>
            {
                new AudioStatusDefinition { Status = "Recorded", Folder = "Audio/Recorded", Recorded = true, Color = "FFFF33" }
            }
        };
    }

    private (bool success, ProjectEnvironment env) Build(ProjectSettings settings)
    {
        var oldOut = Console.Out;
        Console.SetOut(new StringWriter(new StringBuilder()));
        try
        {
            var env = new ProjectEnvironment(settings);
            env.Init();
            bool ok = new Compiler(env).Run();
            return (ok, env);
        }
        finally
        {
            Console.SetOut(oldOut);
        }
    }

    // Read the AudioStatus cell for a given line ID from the recording-script xlsx.
    private static string? ReadRecordingScriptStatus(string xlsxPath, string lineId)
    {
        using var wb = new XLWorkbook(xlsxPath);
        var ws = wb.Worksheets.First();
        int idCol = FindHeaderColumn(ws, "ID");
        int statusCol = FindHeaderColumn(ws, "AudioStatus");
        for (int row = 2; row <= ws.LastRowUsed()!.RowNumber(); row++)
        {
            if (ws.Cell(row, idCol).GetString() == lineId)
                return ws.Cell(row, statusCol).GetString();
        }
        return null;
    }

    private static int FindHeaderColumn(IXLWorksheet ws, string header)
    {
        var headerRow = ws.Row(1);
        int last = ws.LastColumnUsed()!.ColumnNumber();
        for (int col = 1; col <= last; col++)
            if (headerRow.Cell(col).GetString() == header)
                return col;
        throw new Xunit.Sdk.XunitException($"Header '{header}' not found in sheet '{ws.Name}'.");
    }

    [Fact]
    public void RecordedLine_WithoutFlag_ShowsRecorded()
    {
        var (ok, env) = Build(SetupProject(flagLineForReRecord: false));
        Assert.True(ok);
        Assert.Equal("Recorded", ReadRecordingScriptStatus(env.DestRecordingScriptFile, RecordedLineId));
    }

    [Fact]
    public void RecordedLine_WithFlag_ShowsReRecord()
    {
        var (ok, env) = Build(SetupProject(flagLineForReRecord: true));
        Assert.True(ok);
        Assert.Equal("Re-record", ReadRecordingScriptStatus(env.DestRecordingScriptFile, RecordedLineId));
    }

    [Fact]
    public void CastStats_WithFlag_HasReRecordColumnAndCount()
    {
        var (ok, env) = Build(SetupProject(flagLineForReRecord: true));
        Assert.True(ok);

        using var wb = new XLWorkbook(env.DestStatsFile);
        var cast = wb.Worksheets.First(w => w.Name.StartsWith("Cast"));
        int reRecordCol = FindHeaderColumn(cast, "Re-record");

        // FRED speaks the flagged line; expect a Re-record count of at least 1
        // and, for that row, Recorded should not also count it.
        int fredRow = -1;
        for (int row = 2; row <= cast.LastRowUsed()!.RowNumber(); row++)
            if (cast.Cell(row, 1).GetString() == "FRED") { fredRow = row; break; }

        Assert.True(fredRow > 0, "FRED row not found in Cast sheet");
        Assert.Equal(1, cast.Cell(fredRow, reRecordCol).GetValue<int>());
    }
}
