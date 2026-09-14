// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

using DinkTool;

/// <summary>
/// Command-line overrides for the project file's "googleTTS" block. The key
/// guarantee is that --noTts turns generation off regardless of what the
/// project file (or --tts) says.
///
/// Shares the VCIntegration collection because ProjectEnvironment.Init writes
/// to the console, which other suites redirect globally.
/// </summary>
[Collection("VCIntegration")]
public class GoogleTTSCommandLineTests : IDisposable
{
    private readonly string _dir;

    public GoogleTTSCommandLineTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "dink-tts-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* best effort */ }
    }

    private static GoogleTTSSettings ProjectTTS(bool generate = true, bool replaceExisting = true) => new()
    {
        Generate = generate,
        Authentication = "project-key.json",
        OutputFolder = "Audio/TTS",
        ReplaceExisting = replaceExisting,
    };

    [Fact]
    public void NoTts_TurnsGenerationOff_WhenTheProjectEnablesIt()
    {
        var tts = ProjectTTS(generate: true);
        tts.ApplyCommandLine(tts: false, noTts: true, null, null, false, false);
        Assert.False(tts.Generate);
    }

    [Fact]
    public void NoTts_WinsOverTts_WhenBothAreGiven()
    {
        var tts = ProjectTTS(generate: false);
        tts.ApplyCommandLine(tts: true, noTts: true, null, null, false, false);
        Assert.False(tts.Generate);
    }

    [Fact]
    public void Tts_TurnsGenerationOn_WhenTheProjectDisablesIt()
    {
        var tts = ProjectTTS(generate: false);
        tts.ApplyCommandLine(tts: true, noTts: false, null, null, false, false);
        Assert.True(tts.Generate);
    }

    [Fact]
    public void NoFlags_LeaveTheProjectValuesAlone()
    {
        var tts = ProjectTTS(generate: true, replaceExisting: true);
        tts.ApplyCommandLine(false, false, null, "", false, false);
        Assert.True(tts.Generate);
        Assert.Equal("project-key.json", tts.Authentication);
        Assert.Equal("Audio/TTS", tts.OutputFolder);
        Assert.True(tts.ReplaceExisting);
    }

    [Fact]
    public void PathFlags_OverrideTheProjectPaths()
    {
        var tts = ProjectTTS();
        tts.ApplyCommandLine(false, false, "cli-key.json", "Audio/Temp", false, false);
        Assert.Equal("cli-key.json", tts.Authentication);
        Assert.Equal("Audio/Temp", tts.OutputFolder);
    }

    [Fact]
    public void SkipUnchanged_WinsOverReplaceExisting()
    {
        var both = ProjectTTS(replaceExisting: false);
        both.ApplyCommandLine(false, false, null, null, replaceExisting: true, skipUnchanged: true);
        Assert.False(both.ReplaceExisting);

        var replaceOnly = ProjectTTS(replaceExisting: false);
        replaceOnly.ApplyCommandLine(false, false, null, null, replaceExisting: true, skipUnchanged: false);
        Assert.True(replaceOnly.ReplaceExisting);
    }

    [Fact]
    public void NoTts_LetsAProjectWithAMissingKeyFileStillInitialise()
    {
        // With generation on, a missing authentication file fails the build
        // before compiling. --noTts skips that check, so a machine without the
        // Google key can still build a project that has TTS enabled.
        var source = Path.Combine(_dir, "main.ink");
        File.WriteAllText(source, "Hello.\n-> END\n");

        ProjectSettings MakeSettings() => new()
        {
            Source = source,
            DestFolder = Path.Combine(_dir, "out"),
            GoogleTTS = new GoogleTTSSettings
            {
                Generate = true,
                Authentication = Path.Combine(_dir, "missing-key.json"),
                OutputFolder = Path.Combine(_dir, "tts"),
            },
        };

        Assert.False(new ProjectEnvironment(MakeSettings()).Init());

        var settings = MakeSettings();
        settings.GoogleTTS.ApplyCommandLine(tts: false, noTts: true, null, null, false, false);
        var env = new ProjectEnvironment(settings);
        Assert.True(env.Init());
        Assert.False(env.GoogleTTS.Generate);
    }
}
