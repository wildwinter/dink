// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dink;

[JsonDerivedType(typeof(DinkAction), typeDiscriminator: "action")]
[JsonDerivedType(typeof(DinkLine), typeDiscriminator: "line")]
public class DinkBeat
{
    public string LineID { get; set; } = string.Empty;
    public List<string> Comments { get; set; } = new List<string>();

    public List<string> Tags { get; set; } = new List<string>();

    // Looks for comments that e.g. start with VO:
    public List<string> GetComments(params string[] prefixes) 
    {
        // The logic remains the same, as an array implements IEnumerable<string>
        return Comments
            .Where(comment => prefixes.Any(prefix => 
                comment.StartsWith(prefix + ":"))) 
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

public class DinkSnippet
{
    public string SnippetID { get; set; } = string.Empty;
    public List<DinkBeat> Beats { get; set; } = new List<DinkBeat>();

    public override string ToString() => $"Snippet: '{SnippetID}'";
}

public class DinkScene
{
    public string SceneID { get; set; } = string.Empty;
    public List<DinkSnippet> Snippets { get; set; } = new List<DinkSnippet>();

    public override string ToString() => $"Scene: '{SceneID}'";
}

public static class DinkJson
{
    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static string WriteScene(DinkScene scene)
    {
        return JsonSerializer.Serialize(scene, DefaultOptions);
    }

    public static DinkScene ReadScene(string json)
    {
        return JsonSerializer.Deserialize<DinkScene>(json, DefaultOptions)!;
    }

    public static string WriteScenes(List<DinkScene> scenes)
    {
        // Use JsonSerializer.Serialize with the list as the type parameter.
        return JsonSerializer.Serialize(scenes, DefaultOptions);
    }

    public static List<DinkScene> ReadScenes(string json)
    {
        return JsonSerializer.Deserialize<List<DinkScene>>(json, DefaultOptions)!;
    }
}