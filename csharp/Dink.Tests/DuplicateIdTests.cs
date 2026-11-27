// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

using System.Text;
using DinkCompiler;
using DinkTool;

/// <summary>
/// A line ID used on two lines (a line copied with its ID) must fail the build,
/// naming every place it is used. Otherwise everything keyed by ID keeps one
/// line's text and silently drops the other's from the recording script,
/// localisation and runtime strings.
/// </summary>
// Shares the VCIntegration collection: both redirect the global Console.
[Collection("VCIntegration")]
public class DuplicateIdTests : IDisposable
{
    private readonly string _dir;

    public DuplicateIdTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "dink-dupeid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    private ProjectSettings SetupProject(string scene17Line)
    {
        var sourceDir = Path.Combine(_dir, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "main.ink"),
            "INCLUDE s03.ink\nINCLUDE s17.ink\n-> Scene03\n");
        File.WriteAllText(Path.Combine(sourceDir, "s03.ink"),
            "== Scene03\n#dink\nUSEC: Ward? Ward? #id:s03_Scene03_B0XO\nUSEC: Oh, good. #id:s03_Scene03_1GER\n-> Scene17\n");
        File.WriteAllText(Path.Combine(sourceDir, "s17.ink"),
            "== Scene17\n#dink\n" + scene17Line + "\n-> DONE\n");
        File.WriteAllText(Path.Combine(sourceDir, "characters.json"),
            "[{\"ID\":\"USEC\",\"Actor\":\"\"}]");
        return new ProjectSettings
        {
            Source = Path.Combine(sourceDir, "main.ink"),
            DestFolder = Path.Combine(_dir, "output"),
            OutputRecordingScript = true,
            IgnoreWritingStatus = true,
        };
    }

    private static (bool ok, string errors) Build(ProjectSettings settings)
    {
        var oldOut = Console.Out;
        var oldErr = Console.Error;
        var err = new StringWriter(new StringBuilder());
        Console.SetOut(new StringWriter(new StringBuilder()));
        Console.SetError(err);
        try
        {
            var env = new ProjectEnvironment(settings);
            env.Init();
            bool ok = new Compiler(env).Run();
            return (ok, err.ToString());
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
        }
    }

    [Fact]
    public void DuplicateId_FailsTheBuild_AndNamesBothLines()
    {
        var settings = SetupProject("USEC: Glad I got to you. #id:s03_Scene03_B0XO");

        var (ok, errors) = Build(settings);

        Assert.False(ok);
        Assert.Contains("s03_Scene03_B0XO", errors);
        Assert.Contains("s03.ink:3", errors);
        Assert.Contains("s17.ink:3", errors);
    }

    [Fact]
    public void DuplicateId_StillFails_WhenScenesComeFromTheCache()
    {
        var settings = SetupProject("USEC: Glad I got to you. #id:s03_Scene03_B0XO");
        Build(settings);

        // Unchanged source: the second build loads its scenes from the cache.
        var (ok, errors) = Build(settings);

        Assert.False(ok);
        Assert.Contains("s03_Scene03_B0XO", errors);
    }

    [Fact]
    public void UniqueIds_Build()
    {
        var settings = SetupProject("USEC: Glad I got to you. #id:s17_Scene17_LX2J");
        Assert.True(Build(settings).ok);
    }
}
