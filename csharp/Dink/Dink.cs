// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Dink;

class Helper
{
    // Looks for comments that e.g. start with VO: OR have no prefix
    public static List<string> GetCommentsFor(List<string> Comments, string[] prefixes)
    {
        var result = new List<string>();
        foreach (var comment in Comments)
        {
            var match = Regex.Match(comment, @"^([a-zA-Z0-9]+):\s*(.*)$");
            if (match.Success)
            {
                if (prefixes.Contains(match.Groups[1].Value))
                {
                    result.Add(match.Groups[2].Value.Trim());
                }
            }
            else
            {
                result.Add(comment.Trim());
            }
        }
        return result;
    }
}
    
[JsonPolymorphic(TypeDiscriminatorPropertyName = "BeatType")]
[JsonDerivedType(typeof(DinkAction), typeDiscriminator: "Action")]
[JsonDerivedType(typeof(DinkLine), typeDiscriminator: "Line")]
public class DinkBeat
{
    public string LineID { get; set; } = string.Empty;
    public int Group { get; set; } = 0;
    public List<string> Comments { get; set; } = new List<string>();

    public List<string> Tags { get; set; } = new List<string>();

    // Looks for comments that e.g. start with VO:
    public List<string> GetCommentsFor(string[] prefixes)
    {
        return Helper.GetCommentsFor(Comments, prefixes);
    }
    
    public List<string> GetTags(params string[] prefixes) 
    {
        return Tags
            .Where(tag => prefixes.Any(prefix => 
                tag.StartsWith(prefix + ":"))) 
            .ToList();
    }

    public override string ToString() =>
        $", Tags: [{string.Join(", ", Tags)}], LineID: {LineID}, Comments: [{string.Join(",", Comments)}]";
}

public class DinkLine : DinkBeat
{
    public string CharacterID { get; set; } = string.Empty;
    // optional e.g. O.S., V.O. - a variation of the character
    public string Qualifier { get; set; } = string.Empty;
    // optional e.g. loudly
    public string Direction { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public override string ToString() => 
        $"Char: '{CharacterID}', Qualifier: '{Qualifier}', Direction: '{Direction}', Content: '{Text}'"+base.ToString();
}

public class DinkAction : DinkBeat
{
    // optional e.g. SFX, AUDIO
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public override string ToString() =>
        $"Type: '{Type}', Content: '{Text}'" + base.ToString();
}

// Equivalent of an Ink flow fragment
public class DinkSnippet
{
    public List<string> Comments { get; set; } = new List<string>();

    // Looks for comments that e.g. start with VO: OR have no prefix
    public List<string> GetCommentsFor(string[] prefixes)
    {
        return Helper.GetCommentsFor(Comments, prefixes);
    }
    
    public string SnippetID { get; set; } = string.Empty;
    public List<DinkBeat> Beats { get; set; } = new List<DinkBeat>();
    public override string ToString() => $"Snippet: '{SnippetID}'";
}

public class DinkBlock
{
    public string BlockID { get; set; } = string.Empty;
    public List<string> Comments { get; set; } = new List<string>();
    public List<DinkSnippet> Snippets { get; set; } = new List<DinkSnippet>();
    public override string ToString() => $"Block: '{BlockID}'";
}

public class DinkScene
{
    public string SceneID { get; set; } = string.Empty;
    public List<DinkBlock> Blocks { get; set; } = new List<DinkBlock>();
    public override string ToString() => $"Scene: '{SceneID}'";
}