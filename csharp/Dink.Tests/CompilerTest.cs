// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;
using System.IO;
using DinkCompiler;

public class ParserTest
{
    private string loadTestFile(string fileName) {
        return File.ReadAllText("../../../../../tests/"+fileName);
    }

    [Fact]
    public void Test1()
    {
        //string source = loadTestFile("Scratch.fountain");
        //string match = loadTestFile("Scratch.txt");

        var options = new CompilerOptions()
        {
            Source = "../../../../../tests/test1/main.ink",
            DestFolder = "../../../../../tmp"
        };
        Compiler cp = new Compiler(options);
        Assert.True(cp.Run());
    }

    [Fact]
    public void TestExpressionLines()
    {
        string source = loadTestFile("expression_lines.ink");
        var scenes = DinkParser.ParseInk(source);

        Assert.NotNull(scenes);
        Assert.Single(scenes); // One scene (test_knot)
        var scene = scenes[0];
        Assert.Single(scene.Blocks); // One block (default inside knot)
        var block = scene.Blocks[0];
        
        // Expected Snippets:
        // 1. "2:" -> Snippet with comment "2"
        // 2. "(calculation):" -> Snippet with comment "(calculation)"
        // 3. "variable>2:" -> Snippet with comment "variable>2"
        // 4. "(myknotname):" -> Snippet with comment "(myknotname)"
        // 5. "FRED: Hello world." -> Added to snippet 4.
        
        // Note: My implementation of StartNewSnippet discards previous snippet if it has no beats.
        // So snippets 1, 2, 3 will be discarded because they have no beats!
        // Only snippet 4 will remain, and it will have comments accumulated?
        // Let's trace again.
        
        // 1. `2:` -> StartNewSnippet. New snippet S1. Comment "2" added to `comments`.
        // 2. `(calculation):` -> StartNewSnippet. S1 has 0 beats. Discarded. New snippet S2. Comment "(calculation)" added to `comments`.
        // 3. `variable>2:` -> StartNewSnippet. S2 has 0 beats. Discarded. New snippet S3. Comment "variable>2" added to `comments`.
        // 4. `(myknotname):` -> StartNewSnippet. S3 has 0 beats. Discarded. New snippet S4. Comment "(myknotname)" added to `comments`.
        // 5. `FRED: Hello world.` -> Beat added to S4. Beat gets ALL comments: "2", "(calculation)", "variable>2", "(myknotname)".
        
        // So we expect TWO snippets.
        // Snippet 1: Contains "FRED: Hello world." and the comments.
        // Snippet 2: Contains "- FRED: Hello world with dash."
        
        Assert.Equal(2, block.Snippets.Count);
        
        // Snippet 1
        var snippet1 = block.Snippets[0];
        Assert.Single(snippet1.Beats);
        var beat1 = snippet1.Beats[0];
        
        var dinkLine1 = beat1 as Dink.DinkLine;
        Assert.NotNull(dinkLine1);
        Assert.Equal("FRED", dinkLine1.CharacterID);
        // Assert.Equal(4, beat.Comments.Count); // Actual count includes source comments
        Assert.Contains("2", beat1.Comments);
        Assert.Contains("(calculation)", beat1.Comments);
        Assert.Contains("variable>2", beat1.Comments);
        Assert.Contains("(myknotname)", beat1.Comments);
        
        // Snippet 2
        var snippet2 = block.Snippets[1];
        Assert.Single(snippet2.Beats);
        var beat2 = snippet2.Beats[0];
        var dinkLine2 = beat2 as Dink.DinkLine;
        Assert.NotNull(dinkLine2);
        Assert.Equal("FRED", dinkLine2.CharacterID);
        Assert.Equal("Hello world with dash.", dinkLine2.Text);
    }
}
