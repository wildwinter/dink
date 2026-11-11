namespace DinkCompiler;

using System;
using System.IO;
using Google.Cloud.TextToSpeech.V1;
using System.Security.Cryptography;
using System.Text;
using DinkTool;
using SimpleVCLib;

// Quite pleased with this - I store a hash of the line's text inside the WAV file, and so
// only regenerate the file if the line's text has changed.
// A second hash, of the voice request (voice, language, encoding, sample rate), sits beside
// it, so changing a character's TTS voice regenerates that character's lines too.
public class GoogleTTS
{
    private const AudioEncoding Encoding16 = AudioEncoding.Linear16;
    private const int SampleRate = 12000;

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

            Character? character = _characters.Get(line.Character);
            if (character==null)
                continue;
            string ttsVoice = character?.TTSVoice??"";

            // Hashes of the current line text and of the voice request.
            string hash = GenerateHashFromText(line.Line);
            string voiceHash = GenerateVoiceHash(ttsVoice);

            // Does a previous file exist with the same hashes?
            // In which case, don't recreate!
            if (File.Exists(fullPath) && !_config.ReplaceExisting)
            {
                var (existingHash, existingVoiceHash) = ReadHashesFromWAV(fullPath);
                if (IsUpToDate(existingHash, existingVoiceHash, hash, voiceHash))
                    continue;
            }

            if (string.IsNullOrWhiteSpace(ttsVoice))
            {
                Console.WriteLine($"TTS voice missing for character: '{line.Character}'");
                continue;
            }

            byte[]? audioBytes = GenerateAudio(client, line.Line, ttsVoice);
            if (audioBytes != null)
            {
                WriteAudioWithHash(fullPath, audioBytes, hash, voiceHash);
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

    private static byte[]? GenerateAudio(TextToSpeechClient client, string text, string voiceName)
    {
        try
        {
            var input = new SynthesisInput { Text = text };

            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = GetISOCode(voiceName),
                Name = voiceName
            };

            var audioConfig = new AudioConfig
            {
                AudioEncoding = Encoding16,
                SampleRateHertz = SampleRate
            };

            var response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);
            return response.AudioContent.ToByteArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TTS generation failed: {ex.Message}");
            return null;
        }
    }

    public bool Generate(VoiceLines voiceLines)
    {
        return CreateFiles(voiceLines.OrderedEntries);
    }
 
    /// <summary>
    /// A clip is up to date when its text hash matches and its voice hash does too. A clip
    /// with no voice hash (written by an older dink, or by Dinky's scratch recorder) counts as
    /// matching, so upgrading doesn't regenerate, and bill, a whole project at once, and a
    /// recorded scratch take isn't replaced by a voice change.
    /// </summary>
    internal static bool IsUpToDate(string? existingHash, string? existingVoiceHash, string hash, string voiceHash)
    {
        if (existingHash != hash) return false;
        return existingVoiceHash == null || existingVoiceHash == voiceHash;
    }

    /// <summary>
    /// Hash of everything sent to Google besides the text: voice name, language code,
    /// encoding and sample rate. SHA-256, first 16 hex digits.
    /// </summary>
    internal static string GenerateVoiceHash(string voiceName)
    {
        string request = $"{voiceName}|{GetISOCode(voiceName)}|{Encoding16}|{SampleRate}";
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(request));
        return Convert.ToHexString(digest).Substring(0, 16).ToLowerInvariant();
    }

    internal static string GenerateHashFromText(string input)
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
    private static readonly int IdDink = BitConverter.ToInt32(Encoding.ASCII.GetBytes("DINK"), 0); // Custom tag: text hash
    private static readonly int IdDinkVoice = BitConverter.ToInt32(Encoding.ASCII.GetBytes("DNKV"), 0); // Custom tag: voice hash

    /// <summary>
    /// Combines raw TTS audio bytes with the hash metadata and writes to disk in a single operation.
    /// </summary>
    private static void WriteAudioWithHash(string filePath, byte[] audioBytes, string hashCode, string voiceHash)
    {
        byte[] bytes = AppendHashes(audioBytes, hashCode, voiceHash);
        var result = VCLib.WriteBinaryFile(filePath, bytes, forceWrite: true);
        if (!result.Success)
            Console.WriteLine($"Warning: Could not write TTS to '{filePath}': {result.Message}");
        else
            Console.WriteLine($"Generated TTS: {filePath}");
    }

    /// <summary>
    /// Appends 'LIST' + size + 'INFO' + 'DINK' sub-chunk (text hash) + 'DNKV' sub-chunk
    /// (voice hash) to a WAV, and fixes up the RIFF size. DINK stays first and unchanged:
    /// Dinky's scratch recorder and Patterpad read it.
    /// </summary>
    internal static byte[] AppendHashes(byte[] audioBytes, string hashCode, string voiceHash)
    {
        byte[] hashBytes = Encoding.UTF8.GetBytes(hashCode);
        byte[] voiceBytes = Encoding.UTF8.GetBytes(voiceHash);
        int SubChunkSize(byte[] data) => 4 + 4 + data.Length + (data.Length % 2);
        int listChunkSize = 4 + SubChunkSize(hashBytes) + SubChunkSize(voiceBytes);

        using var ms = new MemoryStream(audioBytes.Length + 8 + listChunkSize);
        ms.Write(audioBytes, 0, audioBytes.Length);
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        bw.Write(IdList);
        bw.Write(listChunkSize);
        bw.Write(IdInfo);
        foreach (var (id, data) in new[] { (IdDink, hashBytes), (IdDinkVoice, voiceBytes) })
        {
            bw.Write(id);
            bw.Write(data.Length);
            bw.Write(data);
            if (data.Length % 2 != 0)
                bw.Write((byte)0);
        }

        // Update the main RIFF header size (FileLength - 8 bytes)
        ms.Seek(4, SeekOrigin.Begin);
        bw.Write((int)ms.Length - 8);

        bw.Flush();
        return ms.ToArray();
    }

    /// <summary>
    /// Minimally reads the file to find the DINK (text) and DNKV (voice) hashes without
    /// loading audio data. Either is null when absent.
    /// </summary>
    internal static (string? hash, string? voiceHash) ReadHashesFromWAV(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return ReadHashes(fs);
    }

    internal static (string? hash, string? voiceHash) ReadHashes(Stream fs)
    {
        string? hash = null, voiceHash = null;
        using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);

        // Read RIFF Header (12 bytes)
        if (fs.Length < 12) return (null, null);
        if (br.ReadInt32() != IdRiff) return (null, null);
        br.ReadInt32(); // Skip file size
        if (br.ReadInt32() != IdWave) return (null, null);

        // Iterate through chunks
        while (fs.Position + 8 <= fs.Length)
        {
            int chunkId = br.ReadInt32();
            int chunkSize = br.ReadInt32();
            long chunkEnd = fs.Position + chunkSize + (chunkSize % 2);

            if (chunkId == IdList && chunkSize >= 4 && br.ReadInt32() == IdInfo)
            {
                long listEnd = fs.Position - 4 + chunkSize;
                while (fs.Position + 8 <= listEnd)
                {
                    int subChunkId = br.ReadInt32();
                    int subChunkSize = br.ReadInt32();
                    long subEnd = fs.Position + subChunkSize + (subChunkSize % 2);

                    if (subChunkId == IdDink || subChunkId == IdDinkVoice)
                    {
                        string value = Encoding.UTF8.GetString(br.ReadBytes(subChunkSize)).TrimEnd('\0');
                        if (subChunkId == IdDink) hash ??= value;
                        else voiceHash ??= value;
                    }
                    fs.Seek(subEnd, SeekOrigin.Begin);
                }
            }
            // fmt, data and other chunks are skipped without reading into memory.
            fs.Seek(chunkEnd, SeekOrigin.Begin);
        }
        return (hash, voiceHash);
    }
}
