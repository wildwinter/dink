// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace DinkCompiler;

using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using Dink;
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

        string rootFilename = Path.GetFileNameWithoutExtension(sourceInkFile);

        // Steps:

        // ----- Process Ink files for string data and IDs -----
        OrderedDictionary inkStrings = new OrderedDictionary();
        if (!ProcessInkStrings(sourceInkFolder, inkStrings))
            return false;

        // ----- Compile to json -----
        List<string> usedInkFiles = new List<string>();
        if (!CompileToJson(sourceInkFile, Path.Combine(destFolder, rootFilename + ".json"), usedInkFiles))
            return false;

        // ----- Parse ink files, extract Dink beats -----
        List<DinkScene> parsedDinkScenes = new List<DinkScene>();
        if (!ParseDinkScenes(usedInkFiles, parsedDinkScenes))
            return false;

        // ---- Remove any action and character references from the localisation -----
        if (!FixLoc(parsedDinkScenes, inkStrings))
            return false;

        // ----- Output Dink -----
        if (!WriteDink(parsedDinkScenes, Path.Combine(destFolder, rootFilename + "-dink.json")))
            return false;

        // ----- Output lines for localisation -----
        if (!WriteLoc(inkStrings, Path.Combine(destFolder, rootFilename + "-strings.json")))
            return false;

        Console.WriteLine("Processing complete.");
        return true;
    }

    private bool ProcessInkStrings(string inkFolder, OrderedDictionary outStrings)
    {
        Console.WriteLine("Processing Ink for IDs and string content... " + inkFolder);
        var localiser = new Localiser(new Localiser.Options()
        {
            folder = inkFolder
        });
        if (!localiser.Run())
        {
            Console.Error.WriteLine("Failed to update Ink IDs.");
            return false;
        }
        foreach (var key in localiser.GetStringKeys())
        {
            outStrings.Add(key, localiser.GetString(key));
        }   
        return true;
    }

    List<string> _compileErrors = new List<string>();

    public class InkFileHandler : Ink.IFileHandler {

        private List<string> _outUsedInkFiles;

        public InkFileHandler(List<string> outUsedInkFiles)
        {
            _outUsedInkFiles = outUsedInkFiles;
        }

        public string ResolveInkFilename (string includeName)
        {
            var workingDir = Directory.GetCurrentDirectory ();
            var fullRootInkPath = Path.Combine (workingDir, includeName);
            return fullRootInkPath;
        }

        public string LoadInkFileContents (string fullFilename)
        {
            _outUsedInkFiles.Add(fullFilename);
        	return File.ReadAllText(fullFilename);
        }
    }

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

    private bool CompileToJson(string sourceInkFile, string destFile, List<string> outUsedInkFiles)
    {
        bool success = true;
        _compileErrors.Clear();
        Console.WriteLine("Compiling Ink to JSON... " + sourceInkFile);

        string cwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(Path.GetDirectoryName(sourceInkFile) ?? Directory.GetCurrentDirectory());

        string inputString = File.ReadAllText(sourceInkFile);
        outUsedInkFiles.Add(sourceInkFile);
        InkFileHandler fileHandler = new InkFileHandler(outUsedInkFiles);

        Ink.Compiler compiler = new Ink.Compiler(inputString, new Ink.Compiler.Options
        {
            sourceFilename = sourceInkFile,
            errorHandler = OnCompileError,
            fileHandler = fileHandler
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
            var jsonStr = story?.ToJson();
            try
            {
                File.WriteAllText(destFile, jsonStr, Encoding.UTF8);
            }
            catch
            {
                Console.WriteLine("Could not write to output file '" + destFile + "'");
                success = false;
            }
        }
        Directory.SetCurrentDirectory(cwd);
        return success;
    }

    bool ParseDinkScenes(List<string> usedInkFiles, List<DinkScene> parsedDinkScenes)
    {
        Console.WriteLine("Parsing Dink scenes...");
        foreach (var inkFile in usedInkFiles)
        {
            Console.WriteLine($"Using Ink file '{inkFile}'");
            var text = File.ReadAllText(inkFile);
            var scenes = DinkParser.ParseInk(text);
            parsedDinkScenes.AddRange(scenes);
        }
        return true;
    }

    bool FixLoc(List<DinkScene> parsedDinkScenes, OrderedDictionary inkStrings)
    {
        Console.WriteLine("Fixing localisation entries...");

        var keysToRemove = new List<string>();

        foreach (var scene in parsedDinkScenes)
        {
            foreach (var beat in scene.Beats)
            {
                if (beat is DinkAction action)
                {
                    if (!string.IsNullOrEmpty(action.LineID))
                    {
                        // We don't use these for normal actions.
                        // Maybe want to re-add for closed captions?
                        keysToRemove.Add(action.LineID);
                        action.LineID = "";
                    }
                }
                else if (beat is DinkLine line)
                {
                    if (!string.IsNullOrEmpty(line.LineID))
                    {
                        inkStrings[line.LineID] = line.Text;
                    }
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            if (inkStrings.Contains(key))
                inkStrings.Remove(key);
        }
        return true;
    }

    bool WriteDink(List<DinkScene> dinkScenes, string destDinkFile)
    {
        Console.WriteLine("Writing dink file: " + destDinkFile);

        try
        {
            string fileContents = DinkJson.WriteScenes(dinkScenes);
            File.WriteAllText(destDinkFile, fileContents, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out Dink JSON file {destDinkFile}: " + ex.Message);
            return false;
        }
        return true;
    }

    bool WriteLoc(OrderedDictionary inkStrings, string destLocFile)
    {
        Console.WriteLine("Writing localisation file: " + destLocFile);

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string fileContents = JsonSerializer.Serialize(inkStrings, options);
            File.WriteAllText(destLocFile, fileContents, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out JSON file {destLocFile}: " + ex.Message);
            return false;
        }
        return true;
    }
}