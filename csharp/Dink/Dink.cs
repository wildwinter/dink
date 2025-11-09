// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink;

public class DinkBeat
{
    public string LineID { get; set; } = string.Empty;
}

public class DinkAction : DinkBeat
{
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();

    public override string ToString() =>
        $"Type: '{Type}', Content: '{Content}', Tags: [{string.Join(", ", Tags)}], LineID: {LineID}";
}

public class DinkLine : DinkBeat
{
    public string CharacterID { get; set; } = string.Empty;
    public string Qualifier { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();

    public override string ToString() => 
        $"Char: '{CharacterID}', Qualifier: '{Qualifier}', Direction: '{Direction}', Content: '{Content}', Tags: [{string.Join(", ", Tags)}], LineID: {LineID}";
}

public class DinkScene
{
    public List<DinkBeat> Beats { get; set; } = new List<DinkBeat>();
}