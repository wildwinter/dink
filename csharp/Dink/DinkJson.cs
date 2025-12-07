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

    public static List<DinkScene> ReadScenes(string json)
    {
        return JsonSerializer.Deserialize<List<DinkScene>>(json, DefaultOptions)!;
    }

    // IncludeActionBeatText here is used when the Action beat text should be
    // included in the minimal file e.g. in the Dink toolchain, when Action beats
    // *shouldn't* be localised.
    public static string WriteMinimal(List<DinkScene> scenes, bool includeActionBeatText)
    {
        var exportData = new Dictionary<string, object>();
        foreach (var scene in scenes)
        {
            foreach (var beat in scene.IterateBeats())
            {
                if (beat is DinkAction action)
                {
                    if (includeActionBeatText)
                    {
                        exportData[beat.LineID] = new
                        {
                            Type = "Action",
                            Text = action.Text
                        };
                    }
                }
                else if (beat is DinkLine line)
                {
                    exportData[beat.LineID] = new
                    {
                        Type = "Line",
                        CharacterID = line.CharacterID,
                        Qualifier = line.Qualifier
                    };
                }
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(exportData, options);
    }
}