namespace Dink.Tests;

public class DinkParserTest
{
    [Fact]
    public void ParseID_WithValidID_ShouldReturnID()
    {
        // Arrange
        var line = "some text #id:my_id and some more";

        // Act
        var result = DinkParser.ParseID(line);

        // Assert
        Assert.Equal("my_id", result);
    }

    [Fact]
    public void ParseID_WithNoID_ShouldReturnNull()
    {
        // Arrange
        var line = "some text without an id";

        // Act
        var result = DinkParser.ParseID(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseID_WithMalformedID_ShouldReturnNull()
    {
        // Arrange
        var line = "some text #id: and some more";

        // Act
        var result = DinkParser.ParseID(line);

        // Assert
        Assert.NotEqual("my_id", result);
    }

    [Theory]
    [InlineData(" #tag1 #tag2 ", new[] { "tag1", "tag2" })]
    [InlineData("#tag1", new[] { "tag1" })]
    [InlineData("  #tag_with_underscores  ", new[] { "tag_with_underscores" })]
    [InlineData("#tag1 #tag2 #tag3", new[] { "tag1", "tag2", "tag3" })]
    [InlineData("  #tag_with_underscores  #tag no underscore", new[] { "tag_with_underscores", "tag no underscore"})]
    public void ParseTagLine_WithValidTagLines_ShouldReturnTags(string line, string[] expectedTags)
    {
        // Act
        var result = DinkParser.ParseTagLine(line);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTags.OrderBy(x => x), result.OrderBy(x => x));
    }

    [Theory]
    [InlineData("not a tag line")]
    [InlineData(" # ")]
    [InlineData("")]
    public void ParseTagLine_WithInvalidTagLines_ShouldReturnNull(string line)
    {
        // Act
        var result = DinkParser.ParseTagLine(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseNonDinkLine_WithValidLine_ShouldReturnNonDinkLine()
    {
        // Arrange
        var line = "some text #id:my_id #tag1 #tag2";

        // Act
        var result = DinkParser.ParseNonDinkLine(line, out var ndLine);

        // Assert
        Assert.True(result);
        Assert.NotNull(ndLine);
        Assert.Equal("my_id", ndLine.ID);
        Assert.Equal(new[] { "tag1", "tag2" }, ndLine.Tags);
    }

    [Theory]
    [InlineData("some text without an id")]
    [InlineData("some text #id: #tag1")]
    [InlineData("some text # #tag1")]
    public void ParseNonDinkLine_WithInvalidLine_ShouldReturnFalse(string line)
    {
        // Act
        var result = DinkParser.ParseNonDinkLine(line, out var ndLine);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("A valid action #id:action_id #tag1", "A valid action")]
    [InlineData("- A valid action with dash #id:action_id #tag1", "A valid action with dash")]
    [InlineData("  -   A valid action with dash and spaces #id:action_id #tag1", "A valid action with dash and spaces")]
    public void ParseAction_WithValidLine_ShouldReturnDinkAction(string line, string expectedText)
    {
        // Act
        var result = DinkParser.ParseAction(line);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("action_id", result.LineID);
        Assert.Equal(new[] { "tag1" }, result.Tags);
        Assert.Equal(expectedText, result.Text);
    }

    [Theory]
    [InlineData("not an action")]
    [InlineData("-> NO_TAGS")]
    [InlineData("-> DIVERT #id:action_id #tag1")]
    public void ParseAction_WithInvalidLine_ShouldReturnNull(string line)
    {
        // Act
        var result = DinkParser.ParseAction(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseLine_WithValidLine_ShouldReturnDinkLine()
    {
        // Arrange
        var line = "CHARACTER (qualifier): Hello world #id:line_id #tag1";

        // Act
        var result = DinkParser.ParseLine(line);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CHARACTER", result.CharacterID);
        Assert.Equal("qualifier", result.Qualifier);
        Assert.Equal("Hello world", result.Text);
        Assert.Equal("line_id", result.LineID);
        Assert.Equal(new[] { "tag1" }, result.Tags);
    }

    [Theory]
    [InlineData("not a dink line")]
    [InlineData("CHARACTER: no id")]
    public void ParseLine_WithInvalidLine_ShouldReturnNull(string line)
    {
        // Act
        var result = DinkParser.ParseLine(line);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("* [Option text]", "Option text")]
    [InlineData("+ [Option text]", "Option text")]
    [InlineData("* Option text", "Option text")]
    [InlineData("  *  [  Option text  ]  ", "Option text")]
    public void ParseOption_WithValidOption_ShouldReturnOptionText(string line, string expectedText)
    {
        // Act
        var result = DinkParser.ParseOption(line);

        // Assert
        Assert.Equal(expectedText, result);
    }

    [Theory]
    [InlineData("not an option")]
    [InlineData("[not an option]")]
    public void ParseOption_WithInvalidOption_ShouldReturnNull(string line)
    {
        // Act
        var result = DinkParser.ParseOption(line);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("=== KNOT_NAME ===", "KNOT_NAME")]
    [InlineData("== KNOT_NAME", "KNOT_NAME")]
    public void ParseKnot_WithValidKnot_ShouldReturnKnotName(string line, string expectedName)
    {
        // Act
        var result = DinkParser.ParseKnot(line);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void ParseKnot_WithInvalidKnot_ShouldReturnNull()
    {
        // Arrange
        var line = "= NOT_A_KNOT";

        // Act
        var result = DinkParser.ParseKnot(line);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("= stitch_name", "stitch_name")]
    [InlineData("  =  stitch_name  ", "stitch_name")]
    public void ParseStitch_WithValidStitch_ShouldReturnStitchName(string line, string expectedName)
    {
        // Act
        var result = DinkParser.ParseStitch(line);

        // Assert
        Assert.Equal(expectedName, result);
    }

    [Fact]
    public void ParseStitch_WithInvalidStitch_ShouldReturnNull()
    {
        // Arrange
        var line = "== NOT_A_STITCH";

        // Act
        var result = DinkParser.ParseStitch(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseComment_WithCommentLine_ShouldReturnCommentText()
    {
        // Arrange
        var line = "// this is a comment";

        // Act
        var result = DinkParser.ParseComment(line);

        // Assert
        Assert.Equal("this is a comment", result);
    }

    [Fact]
    public void ParseComment_WithNonCommentLine_ShouldReturnNull()
    {
        // Arrange
        var line = "this is not a comment";

        // Act
        var result = DinkParser.ParseComment(line);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RemoveBlockComments_WithBlockComments_ShouldRemoveThem()
    {
        // Arrange
        var text = "line 1\n/* comment */\nline 3";

        // Act
        var result = DinkParser.RemoveBlockComments(text);

        // Assert
        Assert.Equal("line 1\n\nline 3", result);
    }

    [Fact]
    public void RemoveBlockComments_WithNoBlockComments_ShouldReturnSameText()
    {
        // Arrange
        var text = "line 1\nline 2\nline 3";

        // Act
        var result = DinkParser.RemoveBlockComments(text);

        // Assert
        Assert.Equal(text, result);
    }

    [Fact]
    public void ParseInk_WithEmptyFile_ShouldReturnNoScenes()
    {
        // Arrange
        var text = "";
        var scenes = new List<DinkScene>();
        var nonDinkLines = new List<NonDinkLine>();

        // Act
        var success = DinkParser.ParseInk(text, "test.ink", scenes, nonDinkLines);

        // Assert
        Assert.True(success);
        Assert.Empty(scenes);
        Assert.Empty(nonDinkLines);
    }

    [Fact]
    public void ParseInk_WithSimpleDinkFile_ShouldParseCorrectly()
    {
        // Arrange
        var text = @"
        === KNOT === 
        #dink
        = STITCH
        CHARACTER: Hello world #id:line1
        ";
        var scenes = new List<DinkScene>();
        var nonDinkLines = new List<NonDinkLine>();

        // Act
        var success = DinkParser.ParseInk(text, "test.ink", scenes, nonDinkLines);

        // Assert
        Assert.True(success);
        Assert.Single(scenes);
        var scene = scenes[0];
        Assert.Equal("KNOT", scene.SceneID);
        Assert.Single(scene.Blocks);
        var block = scene.Blocks[0];
        Assert.Equal("STITCH", block.BlockID);
        Assert.Single(block.Snippets);
        var snippet = block.Snippets[0];
        Assert.Single(snippet.Beats);
        var beat = snippet.Beats[0] as DinkLine;
        Assert.NotNull(beat);
        Assert.Equal("CHARACTER", beat.CharacterID);
        Assert.Equal("Hello world", beat.Text);
        Assert.Equal("line1", beat.LineID);
    }
}