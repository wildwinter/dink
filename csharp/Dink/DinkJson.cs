// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas
using System.Text.Json;

namespace Dink;

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
        return JsonSerializer.Serialize(scenes, DefaultOptions);
    }

    // IncludeActionBeatText here is used when the Action beat text should be
    // included in the minimal file e.g. in the Dink toolchain, when Action beats
    // *shouldn't* be localised.
    public static string WriteMinimal(List<DinkScene> scenes, bool includeActionBeatText)
    {
        var options = new JsonSerializerOptions { WriteIndented = false };
        var lines = new List<string>();

        foreach (var scene in scenes)
        {
            foreach (var block in scene.Blocks)
            {
                foreach (var snippet in block.Snippets)
                {
                    foreach (var beat in snippet.Beats)
                    {
                        object obj;
                        if (beat is DinkAction action)
                        {
                            if (includeActionBeatText)
                            {
                                obj = new
                                {
                                    LineID = action.LineID,
                                    BeatType = "Action",
                                    Type = action.Type,
                                    Text = action.Text
                                };
                            }
                            else 
                            {
                                obj = new
                                {
                                    LineID = action.LineID,
                                    BeatType = "Action",
                                    Type = action.Type
                                };
                            }
                            lines.Add(JsonSerializer.Serialize(obj, options));
                        }
                        else if (beat is DinkLine line)
                        {
                            obj = new
                            {
                                LineID = line.LineID,
                                BeatType = "Line",
                                CharacterID = line.CharacterID,
                                Qualifier = line.Qualifier
                            };
                            lines.Add(JsonSerializer.Serialize(obj, options));
                        }
                    }
                }
            }
        }

        return "[\n"+string.Join(",\n", lines)+"\n]";
    }
}