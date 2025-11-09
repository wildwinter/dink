// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace DinkCompiler.Tests;
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

        var options = new Compiler.Options()
        {
            source = "../../../../../tests/test1/main.ink",
            destFolder = "../../../../../tmp"
        };
        Compiler cp = new Compiler(options);
        Assert.True(cp.Run());
    }
}
