// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

public class SceneCharactersTests
{
    private static DinkLine Line(string characterId) =>
        new DinkLine { CharacterID = characterId, Text = "..." };

    private static DinkBlock Block(string blockId, params DinkBeat[] beats)
    {
        var snippet = new DinkSnippet { SnippetID = blockId + "_s0" };
        snippet.Beats.AddRange(beats);
        var block = new DinkBlock { BlockID = blockId };
        block.Snippets.Add(snippet);
        return block;
    }

    private static DinkScene SceneWith(params DinkBlock[] blocks)
    {
        var scene = new DinkScene { SceneID = "scene1" };
        scene.Blocks.AddRange(blocks);
        return scene;
    }

    [Fact]
    public void Characters_CollectsDistinctSpeakersAcrossKnotAndStitches_InFirstAppearanceOrder()
    {
        var scene = SceneWith(
            Block("", Line("FRED"), Line("BOB"), Line("FRED")),      // knot body
            Block("stitch_a", Line("BOB"), Line("ALICE"))            // a stitch
        );

        Assert.Equal(new[] { "FRED", "BOB", "ALICE" }, scene.Characters);
    }

    [Fact]
    public void Characters_IgnoresActionBeatsAndEmptyCharacterIds()
    {
        var scene = SceneWith(
            Block("",
                new DinkAction { Text = "A door slams." },          // no character
                Line(""),                                           // empty id, skipped
                Line("FRED"))
        );

        Assert.Equal(new[] { "FRED" }, scene.Characters);
    }

    [Fact]
    public void Characters_IsEmptyForASceneWithNoSpokenLines()
    {
        var scene = SceneWith(Block("", new DinkAction { Text = "Silence." }));

        Assert.Empty(scene.Characters);
    }

    [Fact]
    public void Characters_SerializesIntoTheStructuredSceneJson()
    {
        var scene = SceneWith(Block("", Line("FRED"), Line("BOB")));

        var json = DinkJson.WriteScene(scene);

        Assert.Contains("\"Characters\"", json);
        Assert.Contains("FRED", json);
        Assert.Contains("BOB", json);
    }

    [Fact]
    public void Characters_RoundTripsThroughStructuredJson_RecomputedFromLines()
    {
        // Characters is get-only, so ReadScene ignores it on load and the getter
        // recomputes from the deserialized blocks. This is what keeps the cached
        // -dink-structure.json consistent across incremental builds.
        var scene = SceneWith(Block("", Line("FRED"), Line("BOB"), Line("FRED")));

        var restored = DinkJson.ReadScene(DinkJson.WriteScene(scene));

        Assert.Equal(new[] { "FRED", "BOB" }, restored.Characters);
    }
}
