// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

using DinkCompiler;

/// <summary>
/// Grammatical gender on characters:
///   - "Male"/"Female"/"Neuter" map to the M/F/N codes used in the
///     localisation spreadsheet column.
///   - Anything else - empty, "Non-specified", typos, null, unknown
///     characters - yields blank rather than guessing.
///   - Casing and stray whitespace from hand-edited characters.json are
///     tolerated and normalised.
/// </summary>
public class CharacterGenderTests
{
    [Theory]
    [InlineData("Male", "M")]
    [InlineData("Female", "F")]
    [InlineData("Neuter", "N")]
    public void GenderCode_MapsTheKnownValues(string gender, string expected)
    {
        Assert.Equal(expected, Characters.GenderCode(gender));
    }

    [Theory]
    [InlineData("male", "M")]
    [InlineData("FEMALE", "F")]
    [InlineData("  neuter  ", "N")]
    public void GenderCode_IsCaseAndWhitespaceInsensitive(string gender, string expected)
    {
        Assert.Equal(expected, Characters.GenderCode(gender));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Non-specified")]
    [InlineData("Nonsense")]
    [InlineData(null)]
    public void GenderCode_IsBlankForAnythingUnrecognised(string? gender)
    {
        Assert.Equal("", Characters.GenderCode(gender));
    }

    [Theory]
    [InlineData("male", "Male")]
    [InlineData("FEMALE", "Female")]
    [InlineData("neuter", "Neuter")]
    [InlineData("Non-specified", "")]
    [InlineData(null, "")]
    public void GenderName_NormalisesToCanonicalCasing(string? gender, string expected)
    {
        Assert.Equal(expected, Characters.GenderName(gender));
    }

    [Fact]
    public void GenderLookup_ByCharacterId_UsesUppercasedIds()
    {
        // FromJson uppercases IDs, so a lowercase id in the file still resolves.
        var chars = Characters.FromJson(
            """[{"ID":"bob","Actor":"Sam","Gender":"Female"}]""");

        Assert.Equal("F", chars.GenderCodeFor("BOB"));
        Assert.Equal("Female", chars.GenderNameFor("BOB"));
    }

    [Fact]
    public void GenderLookup_ForUnknownCharacter_IsBlank()
    {
        var chars = Characters.FromJson("""[{"ID":"BOB","Gender":"Male"}]""");

        Assert.Equal("", chars.GenderCodeFor("NOBODY"));
        Assert.Equal("", chars.GenderNameFor("NOBODY"));
    }

    [Fact]
    public void GenderLookup_ForCharacterWithNoGender_IsBlank()
    {
        var chars = Characters.FromJson("""[{"ID":"GHOST","Actor":"Pat"}]""");

        Assert.Equal("", chars.GenderCodeFor("GHOST"));
        Assert.Equal("", chars.GenderNameFor("GHOST"));
    }

    [Fact]
    public void UnknownJsonFields_AreIgnored()
    {
        // Dinky also writes a Notes field, which the compiler does not model.
        // A note containing "//" must not upset deserialization either.
        var chars = Characters.FromJson(
            """[{"ID":"BOB","Actor":"Sam","Gender":"Male","Notes":"see http://wiki/bob"}]""");

        Assert.Equal("M", chars.GenderCodeFor("BOB"));
        Assert.Equal("Sam", chars.Get("BOB")!.Value.Actor);
    }
}
