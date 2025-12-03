namespace DinkCompiler;

using System;
using System.IO;
using Google.Cloud.TextToSpeech.V1;
using System.Linq;

public class GoogleTTS
{
    private Characters _characters;
    private GoogleTTSOptions _config;

    public GoogleTTS(Characters characters, GoogleTTSOptions config)
    {
        _config = config;
        _characters = characters;
    }

    public bool CreateFiles(IEnumerable<VoiceEntry> voiceLines)
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
            string fileName = line.ID+"_"+GetHash(line.Line)+".wav";
            string? prevFile = FindFirstWavMatch(_config.OutputFolder, line.ID);
            if (prevFile==fileName && !_config.ReplaceExisting)
            {
                continue;
            }

            if (prevFile!=null)
            {
                File.Delete(Path.GetFullPath(Path.Combine(_config.OutputFolder, prevFile)));
            }
            
            string fullPath = Path.GetFullPath(Path.Combine(_config.OutputFolder, fileName));

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

            GenerateAudio(client, line.Line, ttsVoice, fullPath);
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
            using (var output = File.Create(outputFile))
            {
                response.AudioContent.WriteTo(output);
            }
            
            Console.WriteLine($"Generated: {outputFile}");
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

 
    public static string GetHash(string input)
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
        string base64 = Convert.ToBase64String(bytes)
            .Replace('+', '-')   // URL safe
            .Replace('/', '_')   // URL safe
            .TrimEnd('=');       // Remove padding
        return base64.Substring(0, Math.Min(6, base64.Length));
    }

    public string? FindFirstWavMatch(string folderPath, string prefix)
    {
        string searchPattern = $"{prefix}*.wav";
        string? found=Directory.EnumerateFiles(folderPath, searchPattern).FirstOrDefault();
        if (found!=null)
            return Path.GetFileName(found);
        return null;
    }
}