// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace DinkCompiler;

using System.Text;
using Dink;
using Ink;
using InkLocaliser;
using DinkTool;

public class Compiler
{
    private ProjectEnvironment _env;

    public Compiler(ProjectSettings? settings = null)
    {
        _env = new ProjectEnvironment(settings ?? new ProjectSettings());
    }
    public bool Run()
    {
        if (!_env.Init())
            return false;

        // Steps:

        // ----- Read characters -----
        string? charFile = _env.FindFileInSource("characters.json");
        // Character list is optional.
        ReadCharacters(charFile, out Characters? characters);

        // ----- Process Ink files for string data and IDs -----
        if (!ProcessInkStrings(_env.SourceInkFolder, out LocStrings inkStrings))
            return false;

        // ----- Compile to json -----
        if (!CompileToJson(_env.SourceInkFile, _env.DestCompiledInkFile, out List<String> usedInkFiles))
            return false;

        // ----- Parse ink files, extract Dink beats -----
        if (!ParseDinkScenes(usedInkFiles, characters, out List<DinkScene> dinkScenes, out List<NonDinkLine> nonDinkLines))
            return false;

        // ---- Remove any action and character references from the localisation -----
        if (!FixLoc(dinkScenes, nonDinkLines, inkStrings))
            return false;

        // ---- Build writing statuses for lines. This might affect localisation and recording -----
        var writingStatuses = new WritingStatuses(_env);
        if (!writingStatuses.Build(dinkScenes, nonDinkLines, inkStrings))
            return false;

        // ----- Build voice lines -----
        if (!BuildVoiceLines(dinkScenes, out VoiceLines voiceLines))
            return false;

        // ----- Create TTS audio if desired -----
        if (_env.GoogleTTS.Generate)
        {
            if (characters==null)
            {
                Console.Error.WriteLine("Request to generate Google TTS but character file doesn't exist.");
                return false;
            }
            GoogleTTS tts = new GoogleTTS(characters, _env.GoogleTTS);
            if (!tts.Generate(voiceLines))
                return false;
        }

        // ----- Gather voice line statuses -----
        var audioStatuses = new AudioStatuses(_env);
        if (!audioStatuses.Build(voiceLines))
            return false;

        // ----- Output Voice Lines -----
        if (_env.OutputRecordingScript)
        {
            if (!WriteRecordingScript(voiceLines, writingStatuses, audioStatuses, characters, _env.DestRecordingScriptFile))
                return false;
        }

        // ----- Output Dink Structure -----
        if (_env.OutputDinkStructure)
        {
            if (!WriteStructuredDink(dinkScenes, _env.DestDinkStructureFile))
                return false;
        }

        // ----- Output Dink Minimal for runtime -----
        if (!WriteMinimalDink(dinkScenes, _env.DestDinkFile))
            return false;

        // ----- Output lines minimal for runtime -----
        if (!WriteMinimalStrings(inkStrings, _env.DestRuntimeStringsFile))
            return false;

        // ----- Output lines for localisation (Excel) -----
        if (_env.OutputLocalization)
        {
            if (!WriteLocalizationFile(inkStrings, writingStatuses, _env.DestLocFile))
                return false;
        }

        // ----- Output general stats (Excel) -----
        if (_env.OutputStats)
        {
            if (!Stats.WriteExcelFile(_env.RootFilename, dinkScenes, nonDinkLines, 
                        inkStrings, voiceLines, writingStatuses, audioStatuses,
                        characters,
                        _env.DestStatsFile))
                return false;
        }

        Console.WriteLine("Processing complete.");
        return true;
    }

    private bool ReadCharacters(string? charFile, out Characters? outCharacters)
    {
        if (charFile!=null && File.Exists(charFile))
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
                Comments = new List<string>(),
                Origin = new DinkOrigin(),
                IsDink = false
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

    private void OnCompileError(string message, ErrorType errorType)
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

    private bool ParseDinkScenes(List<string> usedInkFiles, Characters? characters, out List<DinkScene> dinkScenes, out List<NonDinkLine> ndLines)
    {
        ndLines = new List<NonDinkLine>();
        dinkScenes = new List<DinkScene>();

        Console.WriteLine("Parsing Dink scenes...");
        foreach (var inkFile in usedInkFiles)
        {
            Console.WriteLine($"Using Ink file '{inkFile}'");
            var text = File.ReadAllText(inkFile);
            var inkFileRelativeToProject = Path.GetRelativePath(_env.ProjectFolder, inkFile);
            var scenes = DinkParser.ParseInk(text, inkFileRelativeToProject, ndLines);

            if (characters!=null)
            {
                foreach(var scene in scenes)
                {
                    foreach (var line in scene.IterateLines()) 
                    {
                        if (!characters.Has(line.CharacterID))
                        {
                            Console.Error.WriteLine($"Error in file {line.Origin.ToString()}, line {line.LineID} - character '{line.CharacterID}' not in the characters file.:");
                            return false;
                        }
                    }
                }
            }
            
            dinkScenes.AddRange(scenes);
        }
        return true;
    }

    private bool FixLoc(List<DinkScene> dinkScenes, List<NonDinkLine> ndLines, LocStrings inkStrings)
    {
        Console.WriteLine("Fixing localisation entries...");

        foreach (var scene in dinkScenes)
        {
            foreach( var block in scene.Blocks)
            {
                foreach (var snippet in block.Snippets)
                {
                    foreach (var beat in snippet.Beats)
                    {
                        if (beat is DinkAction action)
                        {
                            if (_env.LocActionBeats) {
                                // Include action beat in the string table.
                                LocEntry entry = new LocEntry()
                                {
                                    ID = action.LineID,
                                    Text = action.Text,
                                    Comments = action.GetCommentsFor(_env.GetCommentFilters("loc")),
                                    Speaker = "",
                                    Origin = action.Origin,
                                    IsDink = true
                                };
                                inkStrings.Set(entry);
                            }
                            else
                            {
                                // Remove action beats from the string table.
                                inkStrings.Remove(action.LineID);
                            }
                        }
                        else if (beat is DinkLine line)
                        {
                            LocEntry entry = new LocEntry()
                            {
                                ID = line.LineID,
                                Text = line.Text,
                                Comments = line.GetCommentsFor(_env.GetCommentFilters("loc")),
                                Speaker = line.CharacterID,
                                Origin = line.Origin,
                                IsDink = true
                            };
                            inkStrings.Set(entry);
                        }
                    }
                }
            }
        }

        foreach (var ndLine in ndLines)
        {
            inkStrings.SetNonDink(ndLine.ID, ndLine.Origin);
        }
        return true;
    }

    private bool BuildVoiceLines(List<DinkScene> dinkScenes, out VoiceLines voiceLines)
    {
        voiceLines = new VoiceLines();
        Console.WriteLine("Extracting voice lines...");

        foreach (var scene in dinkScenes)
        {
            foreach (var block in scene.Blocks)
            {
                int groupIndex = 0;
                int groupNum = 0;

                Dictionary<int, int> groupSizes = new Dictionary<int, int>();
                foreach (var snippet in block.Snippets)
                {
                    if (snippet.Beats.Count==0)
                        continue;
                    
                    foreach (var beat in snippet.Beats)
                    {
                        if (beat is DinkLine line)
                        {
                            if (beat.Group!=0)
                            {
                                if (groupSizes.ContainsKey(beat.Group))
                                    groupSizes[beat.Group]++;
                                else
                                    groupSizes[beat.Group]=1;
                                break;
                            }
                        }
                    }
                }

                foreach (var snippet in block.Snippets)
                { 
                    int lineIndex = 0;
                    foreach (var beat in snippet.Beats)
                    {
                        if (beat is DinkLine line)
                        {
                            lineIndex++;

                            if (beat.Group!=0)
                            {
                                if (beat.Group!=groupNum)
                                {
                                    groupIndex = 0;
                                    groupNum = beat.Group;
                                }
                                if (lineIndex==1) {
                                    groupIndex++;
                                }
                            }
                            else
                            {
                                groupNum = 0;
                            }

                            VoiceEntry entry = new VoiceEntry()
                            {
                                ID = line.LineID,
                                BlockID = scene.SceneID+(block.BlockID!="" ? "_"+block.BlockID : ""),
                                Character = line.CharacterID,
                                Qualifier = line.Qualifier,
                                Line = line.Text,
                                Direction = line.Direction,
                                SnippetID = snippet.SnippetID,
                                GroupIndicator = "",
                                BraceComments = new List<string>(),
                                SnippetComments = snippet.GetCommentsFor(_env.GetCommentFilters("record")),
                                Comments = line.GetCommentsFor(_env.GetCommentFilters("record")),
                                Tags = line.GetTagsFor(_env.GetTagFilters("record"))
                            };

                            if (lineIndex>1)
                            {
                                entry.SnippetComments.Clear();
                            }
                            
                            if (groupIndex==1)
                            {
                                entry.BraceComments = snippet.GetBraceCommentsFor(_env.GetCommentFilters("record"));
                            }

                            if (groupNum>0)
                            {
                                if (lineIndex==1)
                                    entry.GroupIndicator = $"({groupIndex}/{groupSizes[beat.Group]})";
                                else 
                                    entry.GroupIndicator = "(...)";
                            }

                            voiceLines.Set(entry);
                        }
                    }
                }
            }
        }

        return true;
    }

    private bool WriteStructuredDink(List<DinkScene> dinkScenes, string destDinkFile)
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

    private bool WriteMinimalDink(List<DinkScene> dinkScenes, string destDinkFile)
    {
        Console.WriteLine("Writing minimal dink file: " + destDinkFile);

        try
        {
            string fileContents = DinkJson.WriteMinimal(dinkScenes, !_env.LocActionBeats);
            File.WriteAllText(destDinkFile, fileContents, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out Dink JSON file {destDinkFile}: " + ex.Message);
            return false;
        }
        return true;
    }

    private bool WriteMinimalStrings(LocStrings inkStrings, string destStringsFile)
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

    private bool WriteRecordingScript(VoiceLines voiceLines, WritingStatuses writingStatuses, AudioStatuses audioStatuses, Characters? characters, string destRecordingFile)
    {       
        Console.WriteLine("Writing recording script file: " + destRecordingFile);
        if (!voiceLines.WriteToExcel(_env.RootFilename, characters, writingStatuses, _env.IgnoreWritingStatus, audioStatuses, destRecordingFile))
            return false;
        return true;
    }

    private bool WriteLocalizationFile(LocStrings inkStrings, WritingStatuses writingStatuses, string destLocFile)
    {
        if (!inkStrings.WriteToExcel(_env.RootFilename, writingStatuses, _env.IgnoreWritingStatus, destLocFile))
            return false;
        return true;
    }
}