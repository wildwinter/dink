// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace DinkCompiler;

using System.Text;
using System.Text.Json;
using Dink;
using Ink;
using SimpleVCLib;
using InkLocaliser;
using DinkTool;

public class Compiler
{
    private ProjectEnvironment _env;

    public List<string> UsedInkFiles { get; private set; } = new List<string>();

    public Compiler(ProjectEnvironment env)
    {
        _env = env;
    }
    public bool Run()
    {
        var cache = DinkCache.ForProject(_env.ProjectFolder);

        // ----- Process Ink files for string data and IDs -----
        if (!ProcessInkStrings(_env.SourceInkFile, out LocStrings inkStrings,
            out List<string> usedInkFiles, out Dictionary<string, Localiser.Origin> origins))
            return false;
        UsedInkFiles = usedInkFiles;

        // ----- Read characters -----
        string? charFile = _env.FindFileInSource("characters.json");
        ReadCharacters(charFile, out Characters? characters);

        // ----- Read re-record list (line IDs flagged to be re-recorded) -----
        string? reRecordFile = _env.FindFileInSource("rerecord.json");
        var reRecordIds = ReadReRecordIds(reRecordFile);

        // ----- Compile + Parse - two-level incremental cache -----
        // strippedHash: hash of ink content after text-stripping (what Ink.Compiler
        //   sees).  Changing only dialogue text leaves this unchanged → skip compile.
        // rawHash: hash of raw file bytes (what DinkParser sees).  Any file change
        //   invalidates the parse cache.  rawCurrent ⟹ strippedCurrent.
        var rawHash = DinkCache.HashFiles(usedInkFiles);
        bool rawCurrent = cache.IsCurrent("ink-raw", rawHash);
        bool strippedCurrent = rawCurrent
            || cache.IsCurrent("ink-stripped", ComputeStrippedInkHash(usedInkFiles, inkStrings));

        List<DinkScene> dinkScenes;
        List<NonDinkLine> nonDinkLines;

        if (rawCurrent
            && File.Exists(_env.DestCompiledInkFile)
            && cache.Load("scenes") is string cachedScenes
            && cache.Load("ndlines") is string cachedNdLines)
        {
            Console.WriteLine("Ink source unchanged - loading from cache.");
            dinkScenes = DinkJson.ReadScenes(cachedScenes);
            nonDinkLines = JsonSerializer.Deserialize<List<NonDinkLine>>(cachedNdLines) ?? new();
        }
        else
        {
            // Need previousScenes only when re-parsing, to preserve snippet IDs.
            List<DinkScene>? previousScenes = null;
            if (File.Exists(_env.DestDinkStructureFile))
                previousScenes = DinkJson.ReadScenes(File.ReadAllText(_env.DestDinkStructureFile));

            if (!strippedCurrent || !File.Exists(_env.DestCompiledInkFile))
            {
                if (!CompileToJson(_env.SourceInkFile, inkStrings, !_env.NoStrip, _env.DestCompiledInkFile))
                    return false;
            }
            else
                Console.WriteLine("Ink structure unchanged - skipping compilation.");

            if (!ParseDinkScenes(usedInkFiles, characters, previousScenes,
                    out dinkScenes, out nonDinkLines))
                return false;

            cache.Save("scenes", DinkJson.WriteScenes(dinkScenes));
            cache.Save("ndlines", JsonSerializer.Serialize(nonDinkLines));
        }

        // ---- Remove any action and character references from the localisation -----
        if (!FixLoc(dinkScenes, nonDinkLines, inkStrings))
            return false;

        // ---- Build writing statuses for lines -----
        var writingStatuses = new WritingStatuses(_env);
        if (!writingStatuses.Build(dinkScenes, nonDinkLines, inkStrings))
            return false;

        // ----- Build voice lines -----
        if (!BuildVoiceLines(dinkScenes, out VoiceLines voiceLines))
            return false;

        // ----- Create TTS audio if desired -----
        if (_env.GoogleTTS.Generate)
        {
            if (characters == null)
            {
                Console.Error.WriteLine("Request to generate Google TTS but character file doesn't exist.");
                return false;
            }
            if (!new GoogleTTS(characters, _env.GoogleTTS).Generate(voiceLines))
                return false;
        }

        // ----- Gather voice line statuses -----
        var audioStatuses = new AudioStatuses(_env);
        if (!audioStatuses.Build(voiceLines))
            return false;
        // Apply the re-record flags before any consumer (including the
        // incremental-build hashes below) reads GetStatus.
        audioStatuses.SetReRecordIds(reRecordIds);

        // ----- Output Voice Lines -----
        if (_env.OutputRecordingScript)
        {
            // Skip regenerating the Excel when neither the voice lines nor the
            // audio statuses have changed since the last build.  Serialize the
            // full VoiceEntry so that changes to comments, tags, snippet
            // comments, brace comments, and group indicator are all captured.
            var scriptHash = DinkCache.HashString(
                JsonSerializer.Serialize(voiceLines.OrderedEntries
                    .Select(v => new {
                        v.ID, v.Character, v.Line, v.Qualifier, v.Direction,
                        v.SnippetID, v.GroupIndicator,
                        v.Comments, v.SnippetComments, v.BraceComments,
                        v.SceneComments, v.BlockComments, v.Tags,
                        AudioStatus = audioStatuses.GetStatus(v.ID).Status
                    }).ToList())
            );
            if (!cache.IsCurrent("recording-script", scriptHash))
            {
                if (!WriteRecordingScript(voiceLines, writingStatuses, audioStatuses, characters, _env.DestRecordingScriptFile))
                    return false;
            }
            else
                Console.WriteLine("Recording script unchanged - skipping.");
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
            if (!WriteLocalizationFile(inkStrings, characters, writingStatuses, _env.DestLocFile))
                return false;
        }

        // ----- Output POT file -----
        if (_env.OutputPot)
        {
            if (!WritePotFile(inkStrings, characters, writingStatuses, _env.DestPotFile))
                return false;
        }

        // ----- Output PO files -----
        if (_env.OutputPo)
        {
            if (!WritePoFiles(inkStrings, characters, writingStatuses, _env.PoDir, _env.PoLangs))
                return false;
        }

        // ----- Output general stats (Excel) -----
        if (_env.OutputStats)
        {
            // Skip when scenes, voice lines and statuses are all unchanged.
            var statsHash = DinkCache.HashString(
                DinkJson.WriteScenes(dinkScenes) +
                string.Join("|", voiceLines.OrderedEntries.Select(v =>
                    $"{v.ID}:{writingStatuses.GetStatus(v.ID).Status}:{audioStatuses.GetStatus(v.ID).Status}"))
            );
            if (!cache.IsCurrent("stats", statsHash))
            {
                if (!Stats.WriteExcelFile(_env.RootFilename, dinkScenes, nonDinkLines,
                            inkStrings, voiceLines, writingStatuses, audioStatuses,
                            characters, _env.DestStatsFile))
                    return false;
            }
            else
                Console.WriteLine("Stats unchanged - skipping.");
        }

        // ----- Output origins (JSON) -----
        if (_env.OutputOrigins)
        {
            if (!WriteOrigins(origins, _env.DestOriginsFile))
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

    // Read rerecord.json - a plain JSON array of line IDs the writer has flagged
    // for re-recording. Missing file is normal (returns empty). A malformed file
    // is logged and ignored rather than failing the build.
    private HashSet<string> ReadReRecordIds(string? reRecordFile)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (reRecordFile == null || !File.Exists(reRecordFile))
            return ids;

        try
        {
            string fileText = File.ReadAllText(reRecordFile);
            var list = JsonSerializer.Deserialize<List<string>>(fileText, new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
            if (list != null)
            {
                foreach (var id in list)
                    if (!string.IsNullOrWhiteSpace(id))
                        ids.Add(id.Trim());
            }
            Console.WriteLine($"Read {reRecordFile} ({ids.Count} re-record ID(s)).");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Warning: could not read {reRecordFile}: {e.Message}. Ignoring re-record list.");
        }
        return ids;
    }

    private bool ProcessInkStrings(string inkFile, out LocStrings inkStrings, 
        out List<string> usedInkFiles, out Dictionary<string, Localiser.Origin> origins)
    {
        inkStrings = new LocStrings();
        usedInkFiles = new();
        origins = new();

        Console.WriteLine("Processing Ink for IDs and string content... " + inkFile);
        var localiser = new Localiser(new Localiser.Options()
        {
            file = inkFile
        });
        if (!localiser.Run())
        {
            usedInkFiles = localiser.UsedInkFiles;
            Console.Error.WriteLine("Failed to update Ink IDs.");
            return false;
        }
        usedInkFiles = localiser.UsedInkFiles;
        origins = localiser.LineOrigins;
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

    private bool CompileToJson(string sourceInkFile, LocStrings inkStrings, 
        bool stripText, string destFile)
    {
        bool success = true;
        _compileErrors.Clear();
        Console.WriteLine("Compiling Ink to JSON... " + sourceInkFile);

        string cwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(Path.GetDirectoryName(sourceInkFile) ?? Directory.GetCurrentDirectory());

        var fileHandler = new InkFileHandler(inkStrings, stripText);
        string inputString = fileHandler.LoadInkFileContents(sourceInkFile);

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
            var writeResult = VCLib.WriteTextFile(destFile, jsonStr ?? "", Encoding.UTF8);
            if (!writeResult.Success)
            {
                Console.WriteLine($"Could not write to output file '{destFile}': {writeResult.Message}");
                success = false;
            }
        }
        Directory.SetCurrentDirectory(cwd);
        return success;
    }

    private bool ParseDinkScenes(List<string> usedInkFiles, Characters? characters,
        List<DinkScene>? previousParsedScenes,
        out List<DinkScene> dinkScenes, out List<NonDinkLine> ndLines)
    {
        ndLines = new List<NonDinkLine>();
        dinkScenes = new List<DinkScene>();

        Console.WriteLine("Parsing Dink scenes...");
        foreach (var inkFile in usedInkFiles)
        {
            Console.WriteLine($"Using Ink file '{inkFile}'");
            var text = File.ReadAllText(inkFile);
            var inkFileRelativeToProject = Path.GetRelativePath(_env.ProjectFolder, inkFile);
            var scenes = new List<DinkScene>();
            if (!DinkParser.ParseInk(text, inkFileRelativeToProject, 
                    scenes, ndLines, previousParsedScenes))
            {
                Console.Error.WriteLine("Failed to parse Dink in file: " + inkFile);
                return false;
            }

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

        var locFilters = _env.GetCommentFilters("loc");

        foreach (var scene in dinkScenes)
        {
            var sceneComments = scene.GetCommentsFor(locFilters);
            foreach (var block in scene.Blocks)
            {
                var blockComments = block.GetCommentsFor(locFilters);
                foreach (var snippet in block.Snippets)
                {
                    var groupComments = snippet.GetGroupCommentsFor(locFilters);
                    var snippetComments = snippet.GetCommentsFor(locFilters);
                    foreach (var beat in snippet.Beats)
                    {
                        if (beat is DinkAction action)
                        {
                            if (_env.LocActions) {
                                // Include action beat in the string table.
                                LocEntry entry = new LocEntry()
                                {
                                    ID = action.LineID,
                                    Text = action.Text,
                                    Comments = CombineLocComments(sceneComments, blockComments,
                                                                  groupComments, snippetComments,
                                                                  action.GetCommentsFor(locFilters)),
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
                                Comments = CombineLocComments(sceneComments, blockComments,
                                                              groupComments, snippetComments,
                                                              line.GetCommentsFor(locFilters)),
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

    private static List<string> CombineLocComments(params List<string>[] sources)
    {
        var combined = new List<string>();
        foreach (var src in sources)
            combined.AddRange(src);
        return combined;
    }

    private bool BuildVoiceLines(List<DinkScene> dinkScenes, out VoiceLines voiceLines)
    {
        voiceLines = new VoiceLines();
        Console.WriteLine("Extracting voice lines...");

        var recordFilters = _env.GetCommentFilters("record");

        foreach (var scene in dinkScenes)
        {
            bool sceneFirstLine = true;
            foreach (var block in scene.Blocks)
            {
                bool blockFirstLine = true;
                foreach (var snippet in block.Snippets)
                {
                    string groupIndicator = "";
                    if (snippet.Group!=0)
                    {
                        groupIndicator = $"({snippet.GroupIndex}/{snippet.GroupCount})";
                    }

                    int lineIndex = 0;
                    foreach (var line in snippet.IterateLines())
                    {
                        lineIndex++;
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
                            SceneComments = sceneFirstLine ? scene.GetCommentsFor(recordFilters) : new List<string>(),
                            BlockComments = blockFirstLine ? block.GetCommentsFor(recordFilters) : new List<string>(),
                            BraceComments = new List<string>(),
                            SnippetComments = snippet.GetCommentsFor(recordFilters),
                            Comments = line.GetCommentsFor(recordFilters),
                            Tags = line.GetTagsFor(_env.GetTagFilters("record"))
                        };

                        if (lineIndex>1)
                        {
                            entry.SnippetComments.Clear();
                        }

                        if (snippet.GroupIndex==1)
                        {
                            entry.BraceComments = snippet.GetGroupCommentsFor(recordFilters);
                        }

                        if (snippet.Group>0)
                        {
                            if (lineIndex==1)
                                entry.GroupIndicator = groupIndicator;
                            else
                                entry.GroupIndicator = "(...)";
                        }

                        sceneFirstLine = false;
                        blockFirstLine = false;

                        voiceLines.Set(entry);
                    }
                }
            }
        }

        return true;
    }

    private bool WriteStructuredDink(List<DinkScene> dinkScenes, string destDinkFile)
    {
        Console.WriteLine("Writing structured dink file: " + destDinkFile);

        string fileContents = DinkJson.WriteScenes(dinkScenes);
        var result = VCLib.WriteTextFile(destDinkFile, fileContents, Encoding.UTF8);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Error writing out Dink JSON file {destDinkFile}: {result.Message}");
            return false;
        }
        return true;
    }

    private bool WriteMinimalDink(List<DinkScene> dinkScenes, string destDinkFile)
    {
        Console.WriteLine("Writing minimal dink file: " + destDinkFile);

        string fileContents = DinkJson.WriteMinimal(dinkScenes, !_env.LocActions);
        var result = VCLib.WriteTextFile(destDinkFile, fileContents, Encoding.UTF8);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Error writing out Dink JSON file {destDinkFile}: {result.Message}");
            return false;
        }
        return true;
    }

    private bool WriteMinimalStrings(LocStrings inkStrings, string destStringsFile)
    {
        Console.WriteLine("Writing strings file: " + destStringsFile);

        string fileContents = inkStrings.WriteMinimal();
        var result = VCLib.WriteTextFile(destStringsFile, fileContents, Encoding.UTF8);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Error writing out JSON file {destStringsFile}: {result.Message}");
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

    private bool WriteLocalizationFile(LocStrings inkStrings, Characters? characters, WritingStatuses writingStatuses, string destLocFile)
    {
        if (!inkStrings.WriteToExcel(_env.RootFilename, characters, writingStatuses, _env.IgnoreWritingStatus, destLocFile))
            return false;
        return true;
    }

    private bool WritePotFile(LocStrings inkStrings, Characters? characters, WritingStatuses writingStatuses, string destPotFile)
    {
        if (!inkStrings.WriteToPot(_env.RootFilename, characters, writingStatuses, _env.IgnoreWritingStatus, destPotFile))
            return false;
        return true;
    }

    private bool WritePoFiles(LocStrings inkStrings, Characters? characters, WritingStatuses writingStatuses, string poDir, List<string> langs)
    {
        if (!Directory.Exists(poDir))
            Directory.CreateDirectory(poDir);

        foreach (var lang in langs)
        {
            string poFile = Path.Combine(poDir, _env.RootFilename + "-" + lang + ".po");
            if (!inkStrings.WriteToPo(_env.RootFilename, characters, writingStatuses, _env.IgnoreWritingStatus, lang, poFile))
                return false;
        }
        return true;
    }

    private bool WriteOrigins(Dictionary<string, Localiser.Origin> origins, string destOriginsFile)
    {
        Console.WriteLine("Writing origins file: " + destOriginsFile);

        var options = new JsonSerializerOptions { WriteIndented = true };
        string fileContents = JsonSerializer.Serialize(origins, options);
        var result = VCLib.WriteTextFile(destOriginsFile, fileContents, Encoding.UTF8);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Error writing out origins JSON file {destOriginsFile}: {result.Message}");
            return false;
        }
        return true;
    }

    private string ComputeStrippedInkHash(List<string> inkFiles, LocStrings inkStrings)
    {
        var cwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(Path.GetDirectoryName(_env.SourceInkFile) ?? cwd);
        try
        {
            var handler = new InkFileHandler(inkStrings, !_env.NoStrip);
            return DinkCache.HashStrings(
                inkFiles.OrderBy(f => f, StringComparer.Ordinal)
                        .Select(f => handler.LoadInkFileContents(f)));
        }
        finally
        {
            Directory.SetCurrentDirectory(cwd);
        }
    }

    public class InkFileHandler : IFileHandler
    {
        private LocStrings _strings;
        private bool _stripText;

        public InkFileHandler(LocStrings locStrings, bool stripText)
        {
            _strings = locStrings;
            _stripText = stripText;
        }

        public string ResolveInkFilename(string includeName)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), includeName);
        }

        public string LoadInkFileContents(string fullFilename)
        {
            string fileText = File.ReadAllText(fullFilename);
            if (!_stripText)
                return fileText;

            // If stripping of text is required, we don't
            // return the actual text of the file, we strip
            // it of text first.

            // In case someone's commented out lines with IDs.
            fileText = DinkParser.RemoveBlockComments(fileText);

            List<string> lines = DinkParser.SplitTextIntoLines(fileText);
            for(int i=0;i<lines.Count;i++)
            {
                string line = lines[i].Trim();

                // Does the line have an ID?
                string? id = DinkParser.ParseID(line);
                if (id==null)
                    continue;

                // If so, look for the text already extracted for that ID in the line...
                string? text = _strings.GetText(id);
                if (text==null)
                    continue;

                // and replace it with just the readable ID.
                lines[i] = line.Replace(text,id);
            }
            fileText = string.Join("\n",lines);
            return fileText;
        }
    }
}