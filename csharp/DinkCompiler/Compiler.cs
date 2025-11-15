// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace DinkCompiler;

using System.Text;
using Dink;
using Ink;
using InkLocaliser;

public class Compiler
{
    public class Options
    {
        // Source ink file.
        public string source = "";

        // Folder to output compiled assets to.
        public string destFolder = "";
    }
    private Options _options;

    public Compiler(Options? options = null)
    {
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

        // ----- Read characters -----
        string charFile = Path.Combine(sourceInkFolder, "characters.json");
        // Character list is optional.
        ReadCharacters(charFile, out Characters? characters);

        // ----- Process Ink files for string data and IDs -----
        if (!ProcessInkStrings(sourceInkFolder, out LocStrings inkStrings))
            return false;

        // ----- Compile to json -----
        if (!CompileToJson(sourceInkFile, Path.Combine(destFolder, rootFilename + ".json"), out List<String> usedInkFiles))
            return false;

        // ----- Parse ink files, extract Dink beats -----
        if (!ParseDinkScenes(usedInkFiles, characters, out List<DinkScene> parsedDinkScenes))
            return false;

        // ---- Remove any action and character references from the localisation -----
        if (!FixLoc(parsedDinkScenes, inkStrings))
            return false;

        // ---- Pull out anything that should go to the recording booth -----
        if (!BuildVoiceLines(parsedDinkScenes, out VoiceLines voiceLines))
            return false;

        // ----- Output Voice Lines -----
        if (!voiceLines.WriteToExcel(rootFilename, characters, Path.Combine(destFolder, rootFilename + "-voice.xlsx")))
            return false;

        // ----- Output Dink Structure -----
        if (!WriteStructuredDink(parsedDinkScenes, Path.Combine(destFolder, rootFilename + "-dink-structure.json")))
            return false;

        // ----- Output Dink Minimal for runtime -----
        if (!WriteMinimalDink(parsedDinkScenes, Path.Combine(destFolder, rootFilename + "-dink-min.json")))
            return false;

        // ----- Output lines minimal for runtime -----
        if (!WriteMinimalStrings(inkStrings, Path.Combine(destFolder, rootFilename + "-strings-min.json")))
            return false;

        // ----- Output lines for localisation (Excel) -----
        if (!inkStrings.WriteToExcel(rootFilename, Path.Combine(destFolder, rootFilename + "-strings.xlsx")))
            return false;

        Console.WriteLine("Processing complete.");
        return true;
    }

    private bool ReadCharacters(string charFile, out Characters? outCharacters)
    {
        if (File.Exists(charFile))
        {
            string fileText = File.ReadAllText(charFile);
            outCharacters = Characters.FromJson(fileText);
            Console.WriteLine($"Read {charFile}.");
            return true;
        }
        Console.WriteLine($"{charFile} not found - won't check characters.");
        outCharacters = null;
        return false;
    }

    private bool ProcessInkStrings(string inkFolder, out LocStrings inkStrings)
    {
        inkStrings = new LocStrings();

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
            LocEntry entry = new LocEntry
            {
                ID = key,
                Text = localiser.GetString(key),
                Speaker = "",
                Comments = new List<string>()
            };

            inkStrings.Set(entry);
        }
        return true;
    }

    List<string> _compileErrors = new List<string>();

    public class InkFileHandler : Ink.IFileHandler
    {

        private List<string> _outUsedInkFiles;

        public InkFileHandler(List<string> outUsedInkFiles)
        {
            _outUsedInkFiles = outUsedInkFiles;
        }

        public string ResolveInkFilename(string includeName)
        {
            var workingDir = Directory.GetCurrentDirectory();
            var fullRootInkPath = Path.Combine(workingDir, includeName);
            return fullRootInkPath;
        }

        public string LoadInkFileContents(string fullFilename)
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
                _compileErrors.Add("Author: " + message);
                break;

            case ErrorType.Warning:
                _compileErrors.Add("Warning: " + message);
                break;

            case ErrorType.Error:
                _compileErrors.Add("Error: " + message);
                break;
        }
    }

    private bool CompileToJson(string sourceInkFile, string destFile, out List<string> usedInkFiles)
    {
        usedInkFiles = new List<string>();
                
        bool success = true;
        _compileErrors.Clear();
        Console.WriteLine("Compiling Ink to JSON... " + sourceInkFile);

        string cwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(Path.GetDirectoryName(sourceInkFile) ?? Directory.GetCurrentDirectory());

        string inputString = File.ReadAllText(sourceInkFile);
        usedInkFiles.Add(sourceInkFile);
        InkFileHandler fileHandler = new InkFileHandler(usedInkFiles);

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

    bool ParseDinkScenes(List<string> usedInkFiles, Characters? characters, out List<DinkScene> parsedDinkScenes)
    {
        parsedDinkScenes = new List<DinkScene>();

        Console.WriteLine("Parsing Dink scenes...");
        foreach (var inkFile in usedInkFiles)
        {
            Console.WriteLine($"Using Ink file '{inkFile}'");
            var text = File.ReadAllText(inkFile);
            var scenes = DinkParser.ParseInk(text);

            if (characters!=null)
            {
                foreach(var scene in scenes)
                {
                    foreach (var snippet in scene.Snippets)
                    {
                        foreach(var beat in snippet.Beats)
                        {
                            if (beat is DinkLine line)
                            {
                                if (!characters.Has(line.CharacterID))
                                {
                                    if (snippet.SnippetID.Length == 0)
                                        Console.Error.WriteLine($"Error in file {inkFile}, scene {scene.SceneID}, line {line.LineID} - character '{line.CharacterID}' not in the characters file.:");
                                    else
                                        Console.Error.WriteLine($"Error in file {inkFile}, scene {scene.SceneID}, snippet {snippet.SnippetID}, line {line.LineID} - character '{line.CharacterID}' not in the characters file.:");
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            
            parsedDinkScenes.AddRange(scenes);
        }
        return true;
    }

    bool FixLoc(List<DinkScene> parsedDinkScenes, LocStrings inkStrings)
    {
        Console.WriteLine("Fixing localisation entries...");

        var keysToRemove = new List<string>();

        foreach (var scene in parsedDinkScenes)
        {
            foreach (var snippet in scene.Snippets)
            {
                foreach (var beat in snippet.Beats)
                {
                    if (beat is DinkAction action)
                    {
                        LocEntry entry = new LocEntry()
                        {
                            ID = action.LineID,
                            Text = action.Text,
                            Comments = action.GetComments(["LOC", "VO"]),
                            Speaker = ""
                        };
                    }
                    else if (beat is DinkLine line)
                    {
                        LocEntry entry = new LocEntry()
                        {
                            ID = line.LineID,
                            Text = line.Text,
                            Comments = line.GetComments(["LOC", "VO"]),
                            Speaker = line.CharacterID
                        };
                        inkStrings.Set(entry);
                    }
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            inkStrings.Remove(key);
        }
        return true;
    }

    bool BuildVoiceLines(List<DinkScene> dinkScenes, out VoiceLines voiceLines)
    {
        voiceLines = new VoiceLines();
        Console.WriteLine("Extracting voice lines...");

        foreach (var scene in dinkScenes)
        {
            foreach (var snippet in scene.Snippets)
            {
                foreach (var beat in snippet.Beats)
                {
                    if (beat is DinkLine line)
                    {
                        VoiceEntry entry = new VoiceEntry()
                        {
                            ID = line.LineID,
                            Character = line.CharacterID,
                            Qualifier = line.Qualifier,
                            Line = line.Text,
                            Direction = line.Direction,
                            Comments = line.GetComments(["VO"]),
                            Tags = line.GetTags(["a"])
                        };
                        voiceLines.Set(entry);
                    }
                }
            }
        }

        return true;
    }

    bool WriteStructuredDink(List<DinkScene> dinkScenes, string destDinkFile)
    {
        Console.WriteLine("Writing structured dink file: " + destDinkFile);

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

    bool WriteMinimalDink(List<DinkScene> dinkScenes, string destDinkFile)
    {
        Console.WriteLine("Writing minimal dink file: " + destDinkFile);

        try
        {
            string fileContents = DinkJson.WriteMinimal(dinkScenes);
            File.WriteAllText(destDinkFile, fileContents, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out Dink JSON file {destDinkFile}: " + ex.Message);
            return false;
        }
        return true;
    }

    bool WriteMinimalStrings(LocStrings inkStrings, string destStringsFile)
    {
        Console.WriteLine("Writing strings file: " + destStringsFile);

        try
        {
            string fileContents = inkStrings.WriteMinimal();
            File.WriteAllText(destStringsFile, fileContents, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out JSON file {destStringsFile}: " + ex.Message);
            return false;
        }
        return true;
    }
}