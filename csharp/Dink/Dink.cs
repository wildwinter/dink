// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Dink;

public struct DinkOrigin
{
    public string SourceFilePath {get;set;}
    public int LineNum {get;set;}

    public override string ToString()
    {
        if (string.IsNullOrEmpty(SourceFilePath))
            return "";
        return $"{SourceFilePath}:{LineNum}";
    }
}

public abstract class DinkBase
{
    public DinkOrigin Origin {get;set;}= new DinkOrigin();

    public List<string> Comments { get; set; } = new List<string>();

    public List<string> Tags { get; set; } = new List<string>();

    // Looks for comments that e.g. start with VO: OR have no prefix
    public List<string> GetCommentsFor(List<string> prefixes)
    {
        return GetEntriesWithPrefixes(Comments, prefixes);
    }

    private static bool IsPrefixed(List<string> prefixes, string text)
    {
        return prefixes.Any(prefix => 
            text.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
        );
    }
    protected static List<string> GetEntriesWithPrefixes(List<string> entries, List<string> prefixes, bool trim = true)
    {
        var result = new List<string>();
        foreach (var entry in entries)
        {
            var match = Regex.Match(entry, @"^([a-zA-Z0-9]+):\s*(.*)$");
            if (match.Success)
            {
                if (prefixes.Contains("*")||IsPrefixed(prefixes, match.Groups[1].Value))
                {
                    if (trim)
                        result.Add(match.Groups[2].Value.Trim());
                    else
                        result.Add(entry);
                }
            }
            // No prefix, send it if "?" is in the list.
            else if (prefixes.Contains("?")||prefixes.Contains("*"))
            {
                result.Add(entry.Trim());
            }
        }
        return result;
    }
    
    public List<string> GetTagsFor(List<string> prefixes) 
    {
        return GetEntriesWithPrefixes(Tags, prefixes, false);
    }
}
    
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(DinkAction), typeDiscriminator: "Action")]
[JsonDerivedType(typeof(DinkLine), typeDiscriminator: "Line")]
public class DinkBeat : DinkBase
{
    public string LineID { get; set; } = string.Empty;
    public int Group { get; set; } = 0;

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
    public string Text { get; set; } = string.Empty;
    public override string ToString() =>
        $"Content: '{Text}'" + base.ToString();
}

// Equivalent of an Ink flow fragment
public class DinkSnippet : DinkBase
{
    // Comments collected from braces { } that enclose this snippet
    public List<string> BraceComments { get; set; } = new List<string>();
    public List<string> GetBraceCommentsFor(List<string> prefixes)
    {
        return GetEntriesWithPrefixes(BraceComments, prefixes);
    }
    
    public string SnippetID { get; set; } = string.Empty;
    public List<DinkBeat> Beats { get; set; } = new List<DinkBeat>();
    public override string ToString() => $"Snippet: '{SnippetID}'";
}

public class DinkBlock : DinkBase
{
    public string BlockID { get; set; } = string.Empty;
    public List<DinkSnippet> Snippets { get; set; } = new List<DinkSnippet>();
    public override string ToString() => $"Block: '{BlockID}'";
}

public class DinkScene : DinkBase
{
    public string SceneID { get; set; } = string.Empty;
    public List<DinkBlock> Blocks { get; set; } = new List<DinkBlock>();

    public IEnumerable<DinkBeat> IterateBeats()
    {
        foreach (var block in Blocks)
        {
            foreach (var snippet in block.Snippets)
            {
                foreach (var beat in snippet.Beats)
                {
                    yield return beat;
                }
            }
        }
    }

    public IEnumerable<DinkLine> IterateLines()
    {
        foreach (var block in Blocks)
        {
            foreach (var snippet in block.Snippets)
            {
                foreach (var beat in snippet.Beats)
                {
                    if (beat is DinkLine line)
                    {
                        yield return line;
                    }
                }
            }
        }
    }

    public override string ToString() => $"Scene: '{SceneID}'";
}