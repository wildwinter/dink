// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

/// <summary>
/// Snippet IDs are derived from the snippet's text so they survive rebuilds,
/// but they must still be unique within a block: a snippet ID marks a run of
/// lines, and duplicates merge unrelated snippets in the exporters and at
/// runtime. Identical text is legitimate, e.g. four "*crying*" barks in one
/// shuffle.
/// </summary>
public class SnippetIDTests
{
    private static List<DinkScene> Parse(string ink, List<DinkScene>? oldScenes = null)
    {
        var scenes = new List<DinkScene>();
        var ndLines = new List<NonDinkLine>();
        Assert.True(DinkParser.ParseInk(ink, "test.ink", scenes, ndLines, oldScenes));
        return scenes;
    }

    private static List<string> SnippetIDs(List<DinkScene> scenes) =>
        scenes.SelectMany(s => s.Blocks).SelectMany(b => b.Snippets).Select(s => s.SnippetID).ToList();

    private const string FourIdentical = """
        #dink
        -> idler_cry

        == idler_cry
        #type:chatter
        {shuffle:
        - IDLER: *crying* #id:idler_idler_cry_GUZ5
        - IDLER: *crying* #id:idler_idler_cry_58M2
        - IDLER: *crying* #id:idler_idler_cry_L0G7
        - IDLER: *crying* #id:idler_idler_cry_FHPJ
        }
        -> END
        """;

    private const string FourDifferent = """
        #dink
        -> idler_cry

        == idler_cry
        #type:chatter
        {shuffle:
        - IDLER: *crying* #id:idler_idler_cry_GUZ5
        - IDLER: *sobbing* #id:idler_idler_cry_58M2
        - IDLER: *sniffing* #id:idler_idler_cry_L0G7
        - IDLER: *crying quietly* #id:idler_idler_cry_FHPJ
        }
        -> END
        """;

    [Fact]
    public void SnippetsWithIdenticalText_GetDistinctIDs()
    {
        var ids = SnippetIDs(Parse(FourIdentical));
        Assert.Equal(4, ids.Count);
        Assert.Equal(4, ids.Distinct().Count());
    }

    [Fact]
    public void SnippetsWithIdenticalText_KeepTheirOwnLineIDs()
    {
        // Guards against "fixing" the IDs by merging the snippets.
        var snippets = Parse(FourIdentical).SelectMany(s => s.Blocks).SelectMany(b => b.Snippets).ToList();
        Assert.Equal(
            new[] { "idler_idler_cry_GUZ5", "idler_idler_cry_58M2", "idler_idler_cry_L0G7", "idler_idler_cry_FHPJ" },
            snippets.Select(s => s.Beats.Single().LineID));
    }

    [Fact]
    public void SnippetsWithDifferentText_AreUnaffected()
    {
        var ids = SnippetIDs(Parse(FourDifferent));
        Assert.Equal(4, ids.Distinct().Count());
    }

    [Fact]
    public void IDsAreDeterministicAcrossParses()
    {
        Assert.Equal(SnippetIDs(Parse(FourIdentical)), SnippetIDs(Parse(FourIdentical)));
        Assert.Equal(SnippetIDs(Parse(FourDifferent)), SnippetIDs(Parse(FourDifferent)));
    }

    [Fact]
    public void AnUncollidedIDIsStillThePlainTextHash()
    {
        // Pins the scheme: without a collision the ID is unchanged from before
        // disambiguation existed, so existing projects don't churn.
        Assert.Equal("wKwoeG", SnippetIDs(Parse(FourDifferent))[0]);
        Assert.Equal("wKwoeG", SnippetIDs(Parse(FourIdentical))[0]);
    }

    [Fact]
    public void DuplicateIDsInAPreviousStructureFileAreHealed()
    {
        // A structure file written before this fix has the same ID four times.
        // Re-parsing against it must not carry the duplicates forward.
        var oldBlock = new DinkBlock { BlockID = "" };
        foreach (var lineId in new[] { "idler_idler_cry_GUZ5", "idler_idler_cry_58M2", "idler_idler_cry_L0G7", "idler_idler_cry_FHPJ" })
        {
            var snippet = new DinkSnippet { SnippetID = "wKwoeG" };
            snippet.Beats.Add(new DinkLine { LineID = lineId, Text = "*crying*", CharacterID = "IDLER" });
            oldBlock.Snippets.Add(snippet);
        }
        var oldScene = new DinkScene { SceneID = "idler_cry" };
        oldScene.Blocks.Add(oldBlock);

        var ids = SnippetIDs(Parse(FourIdentical, new List<DinkScene> { oldScene }));
        Assert.Equal(4, ids.Distinct().Count());
    }
}
