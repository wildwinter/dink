// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

using System.Text;
using DinkCompiler;
using DinkTool;

/// <summary>
/// Ink is compiled with countAllVisits on (inklecate -c), so a knot that the
/// script never reads a visit count for still records its visits at runtime.
///
/// Shares the VCIntegration collection so it doesn't run in parallel with the
/// other console-capturing suites (both redirect the global Console.Out).
/// </summary>
[Collection("VCIntegration")]
public class CountAllVisitsTests : IDisposable
{
    private readonly string _dir;

    public CountAllVisitsTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "dink-visits-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    [Fact]
    public void VisitsAreCountedForKnotsTheScriptNeverReads()
    {
        var sourceDir = Path.Combine(_dir, "source");
        var outputDir = Path.Combine(_dir, "output");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);

        File.WriteAllText(Path.Combine(sourceDir, "main.ink"),
            "-> unread\n" +
            "=== unread\n" +
            "A line.\n" +
            "-> END\n");

        var settings = new ProjectSettings
        {
            Source = Path.Combine(sourceDir, "main.ink"),
            DestFolder = outputDir,
            IgnoreWritingStatus = true,
        };

        var oldOut = Console.Out;
        var oldErr = Console.Error;
        Console.SetOut(new StringWriter(new StringBuilder()));
        Console.SetError(new StringWriter(new StringBuilder()));
        ProjectEnvironment env;
        bool ok;
        try
        {
            env = new ProjectEnvironment(settings);
            ok = env.Init() && new Compiler(env).Run();
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
        }
        Assert.True(ok);

        var story = new Ink.Runtime.Story(File.ReadAllText(env.DestCompiledInkFile));
        story.ContinueMaximally();
        Assert.Equal(1, story.state.VisitCountAtPathString("unread"));
    }
}
