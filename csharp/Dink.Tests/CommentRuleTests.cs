// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

/// <summary>
/// Tests for comment routing rules:
///   - Standalone or inline comment on/above a knot header  → scene.Comments
///   - Standalone or inline comment on/above a stitch header → block.Comments
///   - Inline or near-above comment on a {shuffle:} opener  → snippet.GroupComments
///   - "// text" before or inline on a beat                 → beat.Comments
///   - "- // text" line                                     → snippet.Comments of next snippet
///   - "+ [Option] // text"                                 → snippet.Comments of following snippet
///   - > 1 blank line before knot/stitch/group              → comment dropped
/// </summary>
public class CommentRuleTests
{
    private static (List<DinkScene> scenes, List<NonDinkLine> ndLines) Parse(string ink)
    {
        var scenes = new List<DinkScene>();
        var ndLines = new List<NonDinkLine>();
        DinkParser.ParseInk(ink, "test.ink", scenes, ndLines);
        return (scenes, ndLines);
    }

    // Helper: first scene
    private static DinkScene Scene(string ink) => Parse(ink).scenes[0];

    // -------------------------------------------------------------------------
    // Scene comments
    // -------------------------------------------------------------------------

    [Fact]
    public void SceneComment_StandaloneAboveKnot_AttachesToScene()
    {
        var scene = Scene("""
            // Scene note
            === TestScene
            #dink
            FRED: Hello. #id:t1
            """);
        Assert.Contains("Scene note", scene.Comments);
    }

    [Fact]
    public void SceneComment_InlineOnKnot_AttachesToScene()
    {
        var scene = Scene("""
            === TestScene // Inline scene note
            #dink
            FRED: Hello. #id:t1
            """);
        Assert.Contains("Inline scene note", scene.Comments);
    }

    [Fact]
    public void SceneComment_BothStandaloneAndInline_BothAttach()
    {
        var scene = Scene("""
            // Standalone note
            === TestScene // Inline note
            #dink
            FRED: Hello. #id:t1
            """);
        Assert.Contains("Standalone note", scene.Comments);
        Assert.Contains("Inline note", scene.Comments);
    }

    [Fact]
    public void SceneComment_OneBlanLine_StillAttaches()
    {
        var scene = Scene("""
            // Scene note

            === TestScene
            #dink
            FRED: Hello. #id:t1
            """);
        Assert.Contains("Scene note", scene.Comments);
    }

    [Fact]
    public void SceneComment_TwoBlankLines_Dropped()
    {
        var scene = Scene("""
            // Scene note


            === TestScene
            #dink
            FRED: Hello. #id:t1
            """);
        Assert.DoesNotContain("Scene note", scene.Comments);
    }

    // -------------------------------------------------------------------------
    // Block comments
    // -------------------------------------------------------------------------

    [Fact]
    public void BlockComment_StandaloneAboveStitch_AttachesToBlock()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            // Block note
            = Part1
            FRED: Hello. #id:t1
            """);
        var block = scenes[0].Blocks.First(b => b.BlockID == "Part1");
        Assert.Contains("Block note", block.Comments);
    }

    [Fact]
    public void BlockComment_InlineOnStitch_AttachesToBlock()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            = Part1 // Inline block note
            FRED: Hello. #id:t1
            """);
        var block = scenes[0].Blocks.First(b => b.BlockID == "Part1");
        Assert.Contains("Inline block note", block.Comments);
    }

    [Fact]
    public void BlockComment_OneBlankLine_StillAttaches()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            // Block note

            = Part1
            FRED: Hello. #id:t1
            """);
        var block = scenes[0].Blocks.First(b => b.BlockID == "Part1");
        Assert.Contains("Block note", block.Comments);
    }

    [Fact]
    public void BlockComment_TwoBlankLines_Dropped()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            // Block note


            = Part1
            FRED: Hello. #id:t1
            """);
        var block = scenes[0].Blocks.First(b => b.BlockID == "Part1");
        Assert.DoesNotContain("Block note", block.Comments);
    }

    // -------------------------------------------------------------------------
    // Group comments ({shuffle:} etc.)
    // -------------------------------------------------------------------------

    [Fact]
    public void GroupComment_InlineOnShuffle_AttachesToGroup()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            {shuffle: // Group note
            - FRED: Bark 1. #id:t1
            - FRED: Bark 2. #id:t2
            }
            -> DONE
            """);
        var snippets = scenes[0].Blocks[0].Snippets.Where(s => s.Beats.Count > 0).ToList();
        Assert.All(snippets, s => Assert.Contains("Group note", s.GroupComments));
    }

    [Fact]
    public void GroupComment_StandaloneAboveShuffle_AttachesToGroup()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            // Group note
            {shuffle:
            - FRED: Bark 1. #id:t1
            - FRED: Bark 2. #id:t2
            }
            -> DONE
            """);
        var snippets = scenes[0].Blocks[0].Snippets.Where(s => s.Beats.Count > 0).ToList();
        Assert.All(snippets, s => Assert.Contains("Group note", s.GroupComments));
    }

    [Fact]
    public void GroupComment_TwoBlankLinesAboveShuffle_Dropped()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            // Group note


            {shuffle:
            - FRED: Bark 1. #id:t1
            - FRED: Bark 2. #id:t2
            }
            -> DONE
            """);
        var snippets = scenes[0].Blocks[0].Snippets.Where(s => s.Beats.Count > 0).ToList();
        Assert.All(snippets, s => Assert.DoesNotContain("Group note", s.GroupComments));
    }

    // -------------------------------------------------------------------------
    // Beat (line) comments
    // -------------------------------------------------------------------------

    [Fact]
    public void LineComment_StandaloneAboveBeat_AttachesToLine()
    {
        var scene = Scene("""
            === TestScene
            #dink
            // Voice note
            FRED: Hello. #id:t1
            """);
        var line = scene.IterateLines().First();
        Assert.Contains("Voice note", line.Comments);
    }

    [Fact]
    public void LineComment_InlineOnBeat_AttachesToLine()
    {
        var scene = Scene("""
            === TestScene
            #dink
            FRED: Hello. #id:t1 // Inline voice note
            """);
        var line = scene.IterateLines().First();
        Assert.Contains("Inline voice note", line.Comments);
    }

    [Fact]
    public void LineComment_StandaloneAboveShuffleEntry_AttachesToLine_NotSnippet()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            {shuffle:
            // Voice note
            - FRED: Bark. #id:t1
            }
            -> DONE
            """);
        var snippet = scenes[0].Blocks[0].Snippets.First(s => s.Beats.Count > 0);
        var line = (DinkLine)snippet.Beats[0];
        Assert.Contains("Voice note", line.Comments);
        Assert.DoesNotContain("Voice note", snippet.Comments);
    }

    [Fact]
    public void LineComment_InlineOnShuffleEntry_AttachesToLine_NotSnippet()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            {shuffle:
            - FRED: Bark. #id:t1 // Inline note
            }
            -> DONE
            """);
        var snippet = scenes[0].Blocks[0].Snippets.First(s => s.Beats.Count > 0);
        var line = (DinkLine)snippet.Beats[0];
        Assert.Contains("Inline note", line.Comments);
        Assert.DoesNotContain("Inline note", snippet.Comments);
    }

    [Fact]
    public void LineComment_FloatsAcrossWhitespace_ToNextBeat()
    {
        var scene = Scene("""
            === TestScene
            #dink
            // Note with gap below



            FRED: Hello. #id:t1
            """);
        var line = scene.IterateLines().First();
        Assert.Contains("Note with gap below", line.Comments);
    }

    // -------------------------------------------------------------------------
    // Snippet comments ("- // text" form)
    // -------------------------------------------------------------------------

    [Fact]
    public void SnippetComment_DashSlashForm_AttachesToSnippet()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            {shuffle:
            - // Snippet note
            FRED: Line 1. #id:t1
            FRED: Line 2. #id:t2
            }
            -> DONE
            """);
        var snippet = scenes[0].Blocks[0].Snippets.First(s => s.Beats.Count > 0);
        Assert.Contains("Snippet note", snippet.Comments);
    }

    [Fact]
    public void SnippetComment_DashSlashForm_LinesHaveNoComment()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            {shuffle:
            - // Snippet note
            FRED: Line 1. #id:t1
            FRED: Line 2. #id:t2
            }
            -> DONE
            """);
        var snippet = scenes[0].Blocks[0].Snippets.First(s => s.Beats.Count > 0);
        foreach (var beat in snippet.Beats)
            Assert.DoesNotContain("Snippet note", beat.Comments);
    }

    [Fact]
    public void SnippetComment_DashSlashAtGather_AlsoInjectsMerge()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            * [Option A]
                FRED: Path A. #id:t1
            * [Option B]
                FRED: Path B. #id:t2
            - // Gather note
            FRED: Both paths. #id:t3
            """);
        // The snippet starting at the gather should carry both the user's note and MERGE.
        var gatherSnippet = scenes[0].Blocks[0].Snippets
            .First(s => s.Beats.Any(b => b.LineID == "t3"));
        Assert.Contains("Gather note", gatherSnippet.Comments);
        Assert.Contains("MERGE", gatherSnippet.Comments);
    }

    // -------------------------------------------------------------------------
    // Option inline comments
    // -------------------------------------------------------------------------

    [Fact]
    public void OptionComment_InlineOnPlainOption_AttachesToFollowingSnippet()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            * [Choice] // Option note
            FRED: Response. #id:t1
            -
            FRED: Done. #id:t2
            """);
        var responseSnippet = scenes[0].Blocks[0].Snippets
            .First(s => s.Beats.Any(b => b.LineID == "t1"));
        Assert.Contains("Option note", responseSnippet.Comments);
    }

    [Fact]
    public void OptionComment_InlineOnPlainOption_ContainsOptionLabel()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            * [My Choice] // Option note
            FRED: Response. #id:t1
            -
            FRED: Done. #id:t2
            """);
        var responseSnippet = scenes[0].Blocks[0].Snippets
            .First(s => s.Beats.Any(b => b.LineID == "t1"));
        Assert.True(responseSnippet.Comments.Any(c => c.Contains("My Choice")),
            "OPTION label should be in snippet comments");
    }

    [Fact]
    public void OptionComment_ConditionOnOption_StrippedFromOptionLabel()
    {
        var (scenes, _) = Parse("""
            === TestScene
            #dink
            * {seenBefore} [My Choice] // Option note
            FRED: Response. #id:t1
            -
            FRED: Done. #id:t2
            """);
        var responseSnippet = scenes[0].Blocks[0].Snippets
            .First(s => s.Beats.Any(b => b.LineID == "t1"));
        var optionComment = responseSnippet.Comments.FirstOrDefault(c => c.StartsWith("OPTION"));
        Assert.NotNull(optionComment);
        Assert.Contains("My Choice", optionComment);
        Assert.DoesNotContain("{", optionComment);
        Assert.DoesNotContain("seenBefore", optionComment);
    }
}
