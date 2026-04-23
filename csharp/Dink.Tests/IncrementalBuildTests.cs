// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

using System.Text;
using DinkCompiler;
using DinkTool;

// ---------------------------------------------------------------------------
// DinkCache unit tests
// ---------------------------------------------------------------------------

public class DinkCacheTests : IDisposable
{
    private readonly string _dir = VCTestHelpers.MakeTempDir();

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            VCTestHelpers.DeleteDirectory(_dir);
    }

    private DinkCache Cache() => DinkCache.ForProject(_dir);

    [Fact]
    public void IsCurrent_Miss_ReturnsFalse_ThenHitReturnsTrue()
    {
        var cache = Cache();
        Assert.False(cache.IsCurrent("key", "abc123"));
        Assert.True(cache.IsCurrent("key", "abc123"));
    }

    [Fact]
    public void IsCurrent_ChangedHash_ReturnsFalse()
    {
        var cache = Cache();
        cache.IsCurrent("key", "hash1");
        Assert.True(cache.IsCurrent("key", "hash1"));
        Assert.False(cache.IsCurrent("key", "hash2"));
        Assert.True(cache.IsCurrent("key", "hash2"));
    }

    [Fact]
    public void SaveLoad_Roundtrip()
    {
        var cache = Cache();
        cache.Save("mykey", "hello world");
        Assert.Equal("hello world", cache.Load("mykey"));
    }

    [Fact]
    public void Load_MissingKey_ReturnsNull()
    {
        Assert.Null(Cache().Load("does-not-exist"));
    }

    [Fact]
    public void HashFiles_SameContent_SameHash()
    {
        var f1 = Path.Combine(_dir, "a.txt");
        var f2 = Path.Combine(_dir, "b.txt");
        File.WriteAllText(f1, "hello");
        File.WriteAllText(f2, "world");

        var h1 = DinkCache.HashFiles(new[] { f1, f2 });
        var h2 = DinkCache.HashFiles(new[] { f1, f2 });
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void HashFiles_DifferentContent_DifferentHash()
    {
        var f1 = Path.Combine(_dir, "a.txt");
        var f2 = Path.Combine(_dir, "b.txt");
        File.WriteAllText(f1, "hello");
        File.WriteAllText(f2, "world");

        var hBefore = DinkCache.HashFiles(new[] { f1 });
        File.WriteAllText(f1, "changed");
        var hAfter = DinkCache.HashFiles(new[] { f1 });
        Assert.NotEqual(hBefore, hAfter);
    }

    [Fact]
    public void HashFiles_OrderIndependent()
    {
        var f1 = Path.Combine(_dir, "a.txt");
        var f2 = Path.Combine(_dir, "b.txt");
        File.WriteAllText(f1, "hello");
        File.WriteAllText(f2, "world");

        Assert.Equal(
            DinkCache.HashFiles(new[] { f1, f2 }),
            DinkCache.HashFiles(new[] { f2, f1 }));
    }

    [Fact]
    public void HashString_SameContent_SameHash()
    {
        Assert.Equal(DinkCache.HashString("foo"), DinkCache.HashString("foo"));
    }

    [Fact]
    public void HashString_DifferentContent_DifferentHash()
    {
        Assert.NotEqual(DinkCache.HashString("foo"), DinkCache.HashString("bar"));
    }

    [Fact]
    public void HashStrings_SameContent_SameHash()
    {
        Assert.Equal(
            DinkCache.HashStrings(new[] { "alpha", "beta" }),
            DinkCache.HashStrings(new[] { "alpha", "beta" }));
    }

    [Fact]
    public void HashStrings_DifferentContent_DifferentHash()
    {
        Assert.NotEqual(
            DinkCache.HashStrings(new[] { "alpha", "beta" }),
            DinkCache.HashStrings(new[] { "alpha", "CHANGED" }));
    }

    [Fact]
    public void HashStrings_OrderMatters()
    {
        Assert.NotEqual(
            DinkCache.HashStrings(new[] { "alpha", "beta" }),
            DinkCache.HashStrings(new[] { "beta", "alpha" }));
    }
}

// ---------------------------------------------------------------------------
// Two-level incremental hash integration tests
// ---------------------------------------------------------------------------

/// <summary>
/// Tests that the compiler correctly skips Ink.Compiler and/or ParseDinkScenes
/// based on which level of the two-level hash has changed.
/// </summary>
[Collection("VCIntegration")]
public class IncrementalBuildTests : IDisposable
{
    private readonly string _dir = VCTestHelpers.MakeTempDir();

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            VCTestHelpers.DeleteDirectory(_dir);
    }

    /// <summary>Copies test1 source files into a temp subdirectory and returns the settings.</summary>
    private ProjectSettings SetupProject()
    {
        string getThisDir([System.Runtime.CompilerServices.CallerFilePath] string p = "") =>
            Path.GetDirectoryName(p)!;
        var testDataDir = Path.GetFullPath(Path.Combine(getThisDir(), "../../tests/test1"));
        var sourceDir = Path.Combine(_dir, "source");
        var outputDir = Path.Combine(_dir, "output");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(outputDir);

        foreach (var file in Directory.GetFiles(testDataDir, "*.ink"))
            File.Copy(file, Path.Combine(sourceDir, Path.GetFileName(file)), overwrite: true);
        var charFile = Path.Combine(testDataDir, "characters.json");
        if (File.Exists(charFile))
            File.Copy(charFile, Path.Combine(sourceDir, "characters.json"), overwrite: true);

        return new ProjectSettings
        {
            Source = Path.Combine(sourceDir, "main.ink"),
            DestFolder = outputDir,
            OutputDinkStructure = true,
            OutputRecordingScript = true,
            IgnoreWritingStatus = true,
        };
    }

    /// <summary>Runs the compiler and captures its stdout.</summary>
    private (bool success, string output) Build(ProjectSettings settings)
    {
        var oldOut = Console.Out;
        var sb = new StringBuilder();
        Console.SetOut(new StringWriter(sb));
        bool success;
        try
        {
            var env = new ProjectEnvironment(settings);
            env.Init();
            success = new Compiler(env).Run();
        }
        finally
        {
            Console.SetOut(oldOut);
        }
        return (success, sb.ToString());
    }

    [Fact]
    public void ColdBuild_RunsCompileAndParse()
    {
        var settings = SetupProject();
        var (success, output) = Build(settings);

        Assert.True(success);
        Assert.Contains("Compiling Ink to JSON", output);
        Assert.Contains("Parsing Dink scenes", output);
        Assert.DoesNotContain("Ink source unchanged", output);
        Assert.DoesNotContain("Ink structure unchanged", output);
    }

    [Fact]
    public void UnchangedRebuild_LoadsFromCache_SkipsCompileAndParse()
    {
        var settings = SetupProject();
        Build(settings); // warm cache

        var (success, output) = Build(settings);

        Assert.True(success);
        Assert.Contains("Ink source unchanged", output);
        Assert.DoesNotContain("Compiling Ink to JSON", output);
        Assert.DoesNotContain("Parsing Dink scenes", output);
    }

    [Fact]
    public void TextOnlyChange_SkipsCompile_RunsParse()
    {
        var settings = SetupProject();
        Build(settings); // warm cache; InkLocaliser assigns IDs on first pass

        // Read back the (potentially ID-assigned) source so our replacement is safe.
        var mainInk = settings.Source!;
        var content = File.ReadAllText(mainInk);

        // Change the dialogue text for main_TestScene_16U4 while keeping its ID.
        // The stripped hash replaces the text with the ID key, so strippedHash is unchanged.
        var modified = content.Replace(
            "LAURA: This is a line I am saying.",
            "LAURA: This is a completely different line.");
        Assert.NotEqual(content, modified); // sanity: the source text was found

        File.WriteAllText(mainInk, modified);

        var (success, output) = Build(settings);

        Assert.True(success);
        Assert.Contains("Ink structure unchanged", output);  // compile skipped
        Assert.Contains("Parsing Dink scenes", output);       // parse still ran
        Assert.DoesNotContain("Compiling Ink to JSON", output);
        Assert.DoesNotContain("Ink source unchanged", output);
    }

    [Fact]
    public void StructuralChange_RunsBothCompileAndParse()
    {
        var settings = SetupProject();
        Build(settings); // warm cache

        // Insert a new dialogue line without an ID into the #dink TestScene block.
        // InkLocaliser will assign it a new ID, changing the stripped content.
        var mainInk = settings.Source!;
        var content = File.ReadAllText(mainInk);
        var modified = content.Replace(
            "FRED: Glad that's over with!",
            "FRED: A brand new structural line.\nFRED: Glad that's over with!");
        Assert.NotEqual(content, modified);

        File.WriteAllText(mainInk, modified);

        var (success, output) = Build(settings);

        Assert.True(success);
        Assert.Contains("Compiling Ink to JSON", output);
        Assert.Contains("Parsing Dink scenes", output);
        Assert.DoesNotContain("Ink source unchanged", output);
        Assert.DoesNotContain("Ink structure unchanged", output);
    }
}
