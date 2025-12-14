// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;
using DinkTool;
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
        var settings = new ProjectSettings()
        {
            Source = "../../../../../tests/test1/main.ink",
            DestFolder = "./output"
        };
        var env = new ProjectEnvironment(settings);
        env.Init();
        Compiler cp = new Compiler(env);
        Assert.True(cp.Run());
    }
}
