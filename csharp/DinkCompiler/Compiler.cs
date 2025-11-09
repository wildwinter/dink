// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace DinkCompiler;

using Ink;
using InkLocaliser;

public class Compiler
{
    public class Options {
        // Source ink file.
        public string source = "";

        // Folder to output compiled assets to.
        public string destFolder = "";
    }
    private Options _options;

    public Compiler(Options? options = null) {
        _options = options ?? new Options();
    }
    public bool Run()
    {
        string sourceInkFile = _options.source;
        if (String.IsNullOrWhiteSpace(sourceInkFile))
        {
            Console.Error.WriteLine("No Ink file specified.");
            return false;
        }

        sourceInkFile = Path.GetFullPath(sourceInkFile);
        Console.WriteLine($"Using source ink file: '{sourceInkFile}'");

        if (Path.GetExtension(sourceInkFile).ToLower() != ".ink" || !File.Exists(sourceInkFile))
        {
            Console.Error.WriteLine("Ink file name invalid or file missing.");
            return false;
        }

        string sourceInkFolder = Path.GetDirectoryName(sourceInkFile) ?? "";

        string destFolder = _options.destFolder;
        if (String.IsNullOrWhiteSpace(destFolder))
            destFolder = Environment.CurrentDirectory;
        destFolder = Path.GetFullPath(destFolder);
        if (!Directory.Exists(destFolder))
            Directory.CreateDirectory(destFolder);

        Console.WriteLine($"Using destination folder: '{destFolder}'");

        // Steps:

        // ----- Make sure Ink files have IDs -----
        if (!EnsureInkHasIDs(sourceInkFolder))
            return false;

        // ----- Compile to json -----
        if (!CompileToJson(sourceInkFile, destFolder))
            return false;

        // Parse ink files, extract Dink beats
        // Output Dink beats, and lines for localisation, and comments

        Console.WriteLine("IDs added complete.");
        return true;
    }

    private bool EnsureInkHasIDs(string inkFolder)
    {
        Console.WriteLine("Ensuring Ink files have IDs... " + inkFolder);
        var localiser = new Localiser(new Localiser.Options()
        {
            folder = inkFolder
        });
        if (!localiser.Run())
        {
            Console.Error.WriteLine("Failed to update Ink IDs.");
            return false;
        }
        return true;
    }

    List<string> _compileErrors = new List<string>();

    void OnCompileError(string message, ErrorType errorType)
    {
        switch (errorType)
        {
            case ErrorType.Author:
                _compileErrors.Add("Author: "+message);
                break;

            case ErrorType.Warning:
                _compileErrors.Add("Warning: "+message);
                break;

            case ErrorType.Error:
                _compileErrors.Add("Error: "+message);
                break;
        }
    }

    private bool CompileToJson(string sourceInkFile, string destFolder)
    {
        bool success = true;
        _compileErrors.Clear();
        Console.WriteLine("Compiling Ink to JSON... " + sourceInkFile);

        string outputFile = Path.Combine(destFolder, Path.ChangeExtension(Path.GetFileName(sourceInkFile), ".json"));
        string cwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(Path.GetDirectoryName(sourceInkFile));

        string inputString = File.ReadAllText(sourceInkFile);

        Ink.Compiler compiler = new Ink.Compiler(inputString, new Ink.Compiler.Options
        {
            sourceFilename = sourceInkFile,
            errorHandler = OnCompileError
        });
        Ink.Runtime.Story story = compiler.Compile();
        success = !(story == null || _compileErrors.Count > 0);
        if (!success)
        {
            Console.WriteLine("Compilation failed with errors:");
            foreach (var err in _compileErrors)
                Console.WriteLine("  " + err);
        }
        else
        {
            Console.WriteLine("Compilation succeeded.");
            var jsonStr = story.ToJson();
            try
            {
                File.WriteAllText(outputFile, jsonStr, new System.Text.UTF8Encoding(false));

            }
            catch
            {
                Console.WriteLine("Could not write to output file '" + outputFile + "'");
                success = false;
            }
        }
        Directory.SetCurrentDirectory(cwd);
        return success;
    }
}