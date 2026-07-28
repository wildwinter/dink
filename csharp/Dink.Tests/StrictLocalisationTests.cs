// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

using System.Text;
using DinkCompiler;
using DinkTool;

/// <summary>
/// A line split into multiple chunks by inline logic - "Test {count} Again" -
/// cannot be localised as a single string. Strict (the default) fails the
/// compile; lenient warns and skips the line so the rest still compiles.
///
/// Shares the VCIntegration collection so it doesn't run in parallel with the
/// other console-capturing suites (both redirect the global Console.Out).
/// </summary>
[Collection("VCIntegration")]
public class StrictLocalisationTests : IDisposable
{
    private readonly string _dir;

    public StrictLocalisationTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "dink-strict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    private ProjectSettings SetupProject(bool strict)
    {
        var sourceDir = Path.Combine(_dir, "source");
        var outputDir = Path.Combine(_dir, "output");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);

        File.WriteAllText(Path.Combine(sourceDir, "main.ink"),
            "VAR count = 5\n" +
            "A normal line.\n" +
            "Test {count} Again\n" +
            "-> END\n");

        return new ProjectSettings
        {
            Source = Path.Combine(sourceDir, "main.ink"),
            DestFolder = outputDir,
            OutputLocalization = true,
            IgnoreWritingStatus = true,
            Strict = strict,
        };
    }

    private (bool ok, ProjectEnvironment env) Build(ProjectSettings settings)
    {
        var oldOut = Console.Out;
        var oldErr = Console.Error;
        Console.SetOut(new StringWriter(new StringBuilder()));
        Console.SetError(new StringWriter(new StringBuilder()));
        try
        {
            var env = new ProjectEnvironment(settings);
            if (!env.Init()) return (false, env);
            return (new Compiler(env).Run(), env);
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
        }
    }

    [Fact]
    public void Strict_FailsToCompileASplitLine()
    {
        Assert.False(Build(SetupProject(strict: true)).ok);
    }

    [Fact]
    public void Lenient_CompilesASplitLine()
    {
        Assert.True(Build(SetupProject(strict: false)).ok);
    }

    [Fact]
    public void Strict_IsTheDefault()
    {
        // A fresh ProjectSettings (no Strict specified) behaves like strict.
        Assert.True(new ProjectSettings().Strict);
        Assert.False(Build(SetupProject(strict: true)).ok);
    }

    // The key correctness guarantee: with text-stripping ON (the default), a
    // skipped/unlocalised line has no ID, so the stripper leaves it verbatim in
    // the compiled Ink. A normal, localised line does get stripped to its ID.
    [Fact]
    public void Lenient_LeavesTheSkippedLineInTheCompiledInk()
    {
        var (ok, env) = Build(SetupProject(strict: false));
        Assert.True(ok);

        string compiled = File.ReadAllText(env.DestCompiledInkFile);

        // The split line was skipped (no ID), so its literal text survives.
        Assert.Contains("Test", compiled);
        Assert.Contains("Again", compiled);

        // The ordinary line was localised, so its text is stripped (replaced by
        // its ID) and no longer appears verbatim in the compiled Ink.
        Assert.DoesNotContain("A normal line.", compiled);
    }
}
