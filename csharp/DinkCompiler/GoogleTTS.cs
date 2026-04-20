namespace DinkCompiler;

using System;
using System.IO;
using Google.Cloud.TextToSpeech.V1;
using System.Text;
using DinkTool;
using SimpleVCLib;

// Quite pleased with this - I store a hash of the line's text inside the WAV file, and so
// only regenerate the file if the line's text has changed.
public class GoogleTTS
{
    private Characters _characters;
    private GoogleTTSSettings _config;

    public GoogleTTS(Characters characters, GoogleTTSSettings config)
    {
        _config = config;
        _characters = characters;
    }

    private bool CreateFiles(IEnumerable<VoiceEntry> voiceLines)
    {
        if (!File.Exists(_config.Authentication))
        {
            Console.WriteLine($"Error - Credentials file not found: '{_config.Authentication}'");
            return false;
        }
        
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _config.Authentication);
        
        if (!Directory.Exists(_config.OutputFolder))
            Directory.CreateDirectory(_config.OutputFolder);

        var client = TextToSpeechClient.Create();
        foreach(VoiceEntry line in voiceLines)
        {
            string fileName = line.ID+".wav";
            string fullPath = Path.GetFullPath(Path.Combine(_config.OutputFolder, fileName));

            // Create a hash based on the current line text.
            string hash = GenerateHashFromText(line.Line);

            // Does a previous file exist with the same hash?
            // In which case, don't recreate!
            bool prevFileExists = File.Exists(fullPath);
            if (prevFileExists && !_config.ReplaceExisting)
            {
                string? existingHash = ReadHashFromWAV(fullPath);
                if (existingHash == hash)
                    continue; 
            }

            string ttsVoice;
            Character? character = _characters.Get(line.Character);
            if (character==null)
                continue;
            else
                ttsVoice = character?.TTSVoice??"";

            if (string.IsNullOrWhiteSpace(ttsVoice))
            {
                Console.WriteLine($"TTS voice missing for character: '{line.Character}'");
                continue;
            }

            if (GenerateAudio(client, line.Line, ttsVoice, fullPath))
            {
                WriteHashToWAV(fullPath, hash);
            }
        }
        return true;
    }

    private static string GetISOCode(string voiceName)
    {
        if (string.IsNullOrEmpty(voiceName)) return "en-US";

        var parts = voiceName.Split('-');
        if (parts.Length >= 2)
        {
            return $"{parts[0]}-{parts[1]}";
        }

        return "en-US";
    }

    private bool GenerateAudio(TextToSpeechClient client, string text, 
            string voiceName, string outputFile)
    {
        try
        {
            var input = new SynthesisInput
            {
                Text = text
            };

            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = GetISOCode(voiceName),
                Name = voiceName
            };

            var audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Linear16,
                SampleRateHertz = 12000
            };

            var response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);
            var writeResult = VCLib.WriteBinaryFile(outputFile, response.AudioContent.ToByteArray(), forceWrite: true);
            if (!writeResult.Success)
            {
                Console.WriteLine($"FAILED on {outputFile}: {writeResult.Message}");
                return false;
            }

            Console.WriteLine($"Generated TTS: {outputFile}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED on {outputFile}: {ex.Message}");
            return false;
        }
    }

    public bool Generate(VoiceLines voiceLines)
    {
        return CreateFiles(voiceLines.OrderedEntries);
    }
 
    private static string GenerateHashFromText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "000000";
        uint hash = 2166136261;
        unchecked
        {
            foreach (char c in input)
            {
                hash = (hash ^ c) * 16777619;
            }
        }
        byte[] bytes = BitConverter.GetBytes(hash);
        string base64 = Convert.ToBase64String(bytes);
        return base64.Substring(0, Math.Min(6, base64.Length));
    }

    // Standard RIFF Chunk IDs
    private static readonly int IdRiff = BitConverter.ToInt32(Encoding.ASCII.GetBytes("RIFF"), 0);
    private static readonly int IdWave = BitConverter.ToInt32(Encoding.ASCII.GetBytes("WAVE"), 0);
    private static readonly int IdList = BitConverter.ToInt32(Encoding.ASCII.GetBytes("LIST"), 0);
    private static readonly int IdInfo = BitConverter.ToInt32(Encoding.ASCII.GetBytes("INFO"), 0);
    private static readonly int IdDink = BitConverter.ToInt32(Encoding.ASCII.GetBytes("DINK"), 0); // Custom tag

    /// <summary>
    /// Writes the Google TTS audio to disk and appends the hash code as metadata.
    /// </summary>
    private static void WriteHashToWAV(string filePath, string hashCode)
    {
        byte[] original = File.ReadAllBytes(filePath);
        using var ms = new MemoryStream(original.Length + 32);
        ms.Write(original, 0, original.Length);
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        // Prepare the hash data
        byte[] hashBytes = Encoding.UTF8.GetBytes(hashCode);
        // RIFF chunks must be word-aligned (even number of bytes).
        bool needsPadding = hashBytes.Length % 2 != 0;
        int dataSize = hashBytes.Length + (needsPadding ? 1 : 0);

        // Structure: 'LIST' + (TotalListSize) + 'INFO' + 'DINK' + (StrSize) + String + [Pad]
        int dinkChunkSize = 4 + 4 + dataSize;
        int listChunkSize = 4 + dinkChunkSize;

        bw.Write(IdList);
        bw.Write(listChunkSize);
        bw.Write(IdInfo);
        bw.Write(IdDink);
        bw.Write(hashBytes.Length);
        bw.Write(hashBytes);
        if (needsPadding)
            bw.Write((byte)0);

        // Update the main RIFF header size (FileLength - 8 bytes)
        ms.Seek(4, SeekOrigin.Begin);
        bw.Write((int)ms.Length - 8);

        bw.Flush();
        var result = VCLib.WriteBinaryFile(filePath, ms.ToArray(), forceWrite: true);
        if (!result.Success)
            Console.WriteLine($"Warning: Could not write hash to '{filePath}': {result.Message}");
        else
            Console.WriteLine("Write successful.");
    }

    /// <summary>
    /// Minimally reads the file to find the ICMT hash without loading audio data.
    /// </summary>
    private static string? ReadHashFromWAV(string filePath)
    {
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var br = new BinaryReader(fs))
        {
            // Read RIFF Header (12 bytes)
            if (br.ReadInt32() != IdRiff) return null;
            br.ReadInt32(); // Skip file size
            if (br.ReadInt32() != IdWave) return null;

            // Iterate through chunks
            while (fs.Position < fs.Length)
            {
                int chunkId = br.ReadInt32();
                int chunkSize = br.ReadInt32();

                // If we find the LIST chunk
                if (chunkId == IdList)
                {
                    int listType = br.ReadInt32();
                    if (listType == IdInfo)
                    {
                        // We are inside the INFO list, look for ICMT
                        long listEnd = fs.Position - 4 + chunkSize; // -4 because we read the type
                        
                        while (fs.Position < listEnd)
                        {
                            int subChunkId = br.ReadInt32();
                            int subChunkSize = br.ReadInt32();

                            if (subChunkId == IdDink)
                            {
                                byte[] data = br.ReadBytes(subChunkSize);
                                // Handle padding if we read past this loop, but here we just return
                                return Encoding.UTF8.GetString(data).TrimEnd('\0');
                            }
                            else
                            {
                                // Skip unrelated metadata tags
                                if (subChunkSize % 2 != 0) subChunkSize++;
                                fs.Seek(subChunkSize, SeekOrigin.Current); 
                            }
                        }
                    }
                    else
                    {
                        // Not an INFO list, skip it
                        // (Usually shouldn't happen for metadata, but good safety)
                        if (chunkSize % 2 != 0) chunkSize++;
                        fs.Seek(chunkSize - 4, SeekOrigin.Current); 
                    }
                }
                else
                {
                    // This is 'fmt ', 'data', or other chunks. 
                    // SKIP THEM efficiently without reading into memory.
                    if (chunkSize % 2 != 0) chunkSize++; // Word alignment padding
                    fs.Seek(chunkSize, SeekOrigin.Current);
                }
            }
        }
        return null; // Not found
    }
}