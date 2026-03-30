// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

using System.Diagnostics;
using DinkCompiler;
using DinkTool;
using SimpleVCLib;
using Xunit;

namespace Dink.Tests;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

internal static class VCTestHelpers
{
    public static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static bool SvnAvailable()
    {
        static int ExitCode(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                using var p = Process.Start(psi)!;
                p.WaitForExit();
                return p.ExitCode;
            }
            catch { return -1; }
        }
        return ExitCode("svn", "--version --quiet") == 0 &&
               ExitCode("svnadmin", "--version --quiet") == 0;
    }

    /// <summary>
    /// Initialise a git repo, configure identity, and make an initial commit.
    /// </summary>
    public static void InitGitRepo(string dir)
    {
        static void Run(string args, string cwd)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = cwd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
        }
        Run("init", dir);
        Run("config user.email test@example.com", dir);
        Run("config user.name \"Test User\"", dir);
        File.WriteAllText(Path.Combine(dir, "README.md"), "test");
        Run("add README.md", dir);
        Run("commit -m init", dir);
    }

    /// <summary>
    /// Create a temp SVN repository and working copy with one initial commit.
    /// Returns the working-copy path.
    /// </summary>
    public static string InitSvnRepo()
    {
        static (int exitCode, string output) Run(string exe, string args, string? cwd = null)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                WorkingDirectory = cwd ?? Path.GetTempPath(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi)!;
            var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit();
            return (p.ExitCode, output);
        }

        var repoDir = MakeTempDir();
        var wcDir   = MakeTempDir();

        Run("svnadmin", $"create \"{repoDir}\"");
        Run("svn", $"checkout \"file://{repoDir}\" \"{wcDir}\"");
        File.WriteAllText(Path.Combine(wcDir, "initial.txt"), "initial");
        Run("svn", "add initial.txt", wcDir);
        Run("svn", "commit -m initial --username test --no-auth-cache", wcDir);

        return wcDir;
    }

    /// <summary>
    /// Copies the test1 ink project into <paramref name="sourceDir"/>, runs the compiler
    /// with outputs going to <paramref name="outputDir"/>, and returns the environment
    /// and compilation result.
    /// </summary>
    /// <param name="sourceDir">Where ink source files are copied (must be inside a VC repo).</param>
    /// <param name="outputDir">Where compiled outputs are written (must also be inside the repo).</param>
    /// <param name="enablePoOutput">
    /// Set false for SVN tests — plain <c>svn add</c> doesn't handle new subdirectories,
    /// so PO output (which creates a <c>po/</c> subfolder) is skipped.
    /// </param>
    public static (ProjectEnvironment env, bool success) PrepareAndRun(
        string sourceDir, string outputDir, bool enablePoOutput = true)
    {
        string getThisDir([System.Runtime.CompilerServices.CallerFilePath] string p = "") => Path.GetDirectoryName(p)!;
        var testDataDir = Path.GetFullPath(Path.Combine(getThisDir(), "../../tests/test1"));

        // Copy all ink source files and the characters file into the source directory.
        foreach (var file in Directory.GetFiles(testDataDir, "*.ink"))
            File.Copy(file, Path.Combine(sourceDir, Path.GetFileName(file)), overwrite: true);
        var charFile = Path.Combine(testDataDir, "characters.json");
        if (File.Exists(charFile))
            File.Copy(charFile, Path.Combine(sourceDir, "characters.json"), overwrite: true);

        var settings = new ProjectSettings
        {
            Source         = Path.Combine(sourceDir, "main.ink"),
            DestFolder     = outputDir,
            OutputDinkStructure  = true,
            OutputOrigins        = true,
            OutputPot            = true,
            OutputRecordingScript = true,
            IgnoreWritingStatus  = true,
        };

        if (enablePoOutput)
        {
            settings.PoDir  = Path.Combine(outputDir, "po");
            settings.PoLangs = new List<string> { "fr", "de" };
        }

        var env = new ProjectEnvironment(settings);
        env.Init();

        bool success = new Compiler(env).Run();
        return (env, success);
    }

    /// <returns>Short git status prefix for the file, e.g. "A " or "M ".</returns>
    public static string GitStatus(string filePath, string repoDir)
    {
        var psi = new ProcessStartInfo("git", $"status --short \"{filePath}\"")
        {
            WorkingDirectory = repoDir,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return output;
    }

    /// <returns>SVN status string for the file (first character is the status code).</returns>
    public static string SvnStatus(string filePath, string wcDir)
    {
        var psi = new ProcessStartInfo("svn", $"status \"{filePath}\"")
        {
            WorkingDirectory = wcDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        return output;
    }
}

// ---------------------------------------------------------------------------
// Git integration tests
// ---------------------------------------------------------------------------

/// <summary>
/// Verifies that all Dink compiler output files are written AND staged in git
/// when SimpleVCLib is active. The compiler is run once in the constructor;
/// each [Fact] asserts a different output file.
/// </summary>
/// <remarks>
/// Placed in the "VCIntegration" collection alongside <see cref="SvnVCIntegrationTests"/>
/// so xUnit runs the two classes sequentially — VCLib.SetProvider is global state.
/// </remarks>
[Collection("VCIntegration")]
public class GitVCIntegrationTests : IDisposable
{
    private readonly string _repoDir = VCTestHelpers.MakeTempDir();
    private readonly ProjectEnvironment _env;
    private readonly bool _compiled;

    public GitVCIntegrationTests()
    {
        VCTestHelpers.InitGitRepo(_repoDir);
        VCLib.SetProvider(new GitProvider());
        var outputDir = Path.Combine(_repoDir, "output");
        (_env, _compiled) = VCTestHelpers.PrepareAndRun(_repoDir, outputDir);
    }

    public void Dispose()
    {
        VCLib.ClearProvider();
        if (Directory.Exists(_repoDir))
            Directory.Delete(_repoDir, recursive: true);
    }

    [Fact]
    public void CompiledInkJson_IsStagedInGit()
    {
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env.DestCompiledInkFile));
        Assert.StartsWith("A", VCTestHelpers.GitStatus(_env.DestCompiledInkFile, _repoDir));
    }

    [Fact]
    public void MinimalDinkJson_IsStagedInGit()
    {
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env.DestDinkFile));
        Assert.StartsWith("A", VCTestHelpers.GitStatus(_env.DestDinkFile, _repoDir));
    }

    [Fact]
    public void RuntimeStringsJson_IsStagedInGit()
    {
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env.DestRuntimeStringsFile));
        Assert.StartsWith("A", VCTestHelpers.GitStatus(_env.DestRuntimeStringsFile, _repoDir));
    }

    [Fact]
    public void DinkStructureJson_IsStagedInGit()
    {
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env.DestDinkStructureFile));
        Assert.StartsWith("A", VCTestHelpers.GitStatus(_env.DestDinkStructureFile, _repoDir));
    }

    [Fact]
    public void OriginsJson_IsStagedInGit()
    {
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env.DestOriginsFile));
        Assert.StartsWith("A", VCTestHelpers.GitStatus(_env.DestOriginsFile, _repoDir));
    }

    [Fact]
    public void PotFile_IsStagedInGit()
    {
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env.DestPotFile));
        Assert.StartsWith("A", VCTestHelpers.GitStatus(_env.DestPotFile, _repoDir));
    }

    [Fact]
    public void PoFiles_AreStagedInGit()
    {
        Assert.True(_compiled, "Compiler.Run() should succeed");
        foreach (var lang in new[] { "fr", "de" })
        {
            var poFile = Path.Combine(_env.PoDir, $"main-{lang}.po");
            Assert.True(File.Exists(poFile), $"Expected PO file for '{lang}' to exist");
            Assert.StartsWith("A", VCTestHelpers.GitStatus(poFile, _repoDir));
        }
    }

    [Fact]
    public void RecordingScriptXlsx_IsStagedInGit()
    {
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env.DestRecordingScriptFile));
        Assert.StartsWith("A", VCTestHelpers.GitStatus(_env.DestRecordingScriptFile, _repoDir));
    }

    [Fact]
    public void AutoDetect_GitRepo_WritesAndStages()
    {
        // ClearProvider so VCLib auto-detects from the file path.
        VCLib.ClearProvider();

        var dir = VCTestHelpers.MakeTempDir();
        try
        {
            VCTestHelpers.InitGitRepo(dir);
            var outputDir = Path.Combine(dir, "output");
            var (env, success) = VCTestHelpers.PrepareAndRun(dir, outputDir);

            Assert.True(success, "Compiler.Run() should succeed");
            Assert.StartsWith("A", VCTestHelpers.GitStatus(env.DestCompiledInkFile, dir));
            Assert.StartsWith("A", VCTestHelpers.GitStatus(env.DestDinkFile, dir));
        }
        finally
        {
            VCLib.SetProvider(new GitProvider()); // Restore for Dispose.
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}

// ---------------------------------------------------------------------------
// SVN integration tests
// Skipped automatically when svn/svnadmin are not installed.
// Note: plain `svn add` does not create missing parent directories, so output
// files are written flat into the working-copy root rather than a subdirectory,
// and PO output (which creates a po/ subfolder) is disabled.
// ---------------------------------------------------------------------------

[Collection("VCIntegration")]
public class SvnVCIntegrationTests : IDisposable
{
    private readonly bool _available;
    private readonly string _wcDir;
    private readonly ProjectEnvironment? _env;
    private readonly bool _compiled;

    public SvnVCIntegrationTests()
    {
        _available = VCTestHelpers.SvnAvailable();
        if (!_available)
        {
            _wcDir = string.Empty;
            return;
        }

        _wcDir = VCTestHelpers.InitSvnRepo();
        VCLib.SetProvider(new SvnProvider());
        // Output goes flat into the wc root so svn add works without --parents.
        (_env, _compiled) = VCTestHelpers.PrepareAndRun(_wcDir, _wcDir, enablePoOutput: false);
    }

    public void Dispose()
    {
        VCLib.ClearProvider();
        if (_wcDir != string.Empty && Directory.Exists(_wcDir))
            Directory.Delete(_wcDir, recursive: true);
    }

    [Fact]
    public void CompiledInkJson_IsAddedInSvn()
    {
        if (!_available) return;
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env!.DestCompiledInkFile));
        Assert.StartsWith("A", VCTestHelpers.SvnStatus(_env!.DestCompiledInkFile, _wcDir));
    }

    [Fact]
    public void MinimalDinkJson_IsAddedInSvn()
    {
        if (!_available) return;
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env!.DestDinkFile));
        Assert.StartsWith("A", VCTestHelpers.SvnStatus(_env!.DestDinkFile, _wcDir));
    }

    [Fact]
    public void RuntimeStringsJson_IsAddedInSvn()
    {
        if (!_available) return;
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env!.DestRuntimeStringsFile));
        Assert.StartsWith("A", VCTestHelpers.SvnStatus(_env!.DestRuntimeStringsFile, _wcDir));
    }

    [Fact]
    public void PotFile_IsAddedInSvn()
    {
        if (!_available) return;
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env!.DestPotFile));
        Assert.StartsWith("A", VCTestHelpers.SvnStatus(_env!.DestPotFile, _wcDir));
    }

    [Fact]
    public void RecordingScriptXlsx_IsAddedInSvn()
    {
        if (!_available) return;
        Assert.True(_compiled, "Compiler.Run() should succeed");
        Assert.True(File.Exists(_env!.DestRecordingScriptFile));
        Assert.StartsWith("A", VCTestHelpers.SvnStatus(_env!.DestRecordingScriptFile, _wcDir));
    }

    [Fact]
    public void AutoDetect_SvnWorkingCopy_WritesAndAdds()
    {
        if (!_available) return;
        VCLib.ClearProvider(); // Let VCLib auto-detect SVN.

        var wcDir = VCTestHelpers.InitSvnRepo();
        try
        {
            var (env, success) = VCTestHelpers.PrepareAndRun(wcDir, wcDir, enablePoOutput: false);

            Assert.True(success, "Compiler.Run() should succeed");
            Assert.StartsWith("A", VCTestHelpers.SvnStatus(env.DestCompiledInkFile, wcDir));
            Assert.StartsWith("A", VCTestHelpers.SvnStatus(env.DestDinkFile, wcDir));
        }
        finally
        {
            VCLib.SetProvider(new SvnProvider()); // Restore for Dispose.
            if (Directory.Exists(wcDir))
                Directory.Delete(wcDir, recursive: true);
        }
    }
}
