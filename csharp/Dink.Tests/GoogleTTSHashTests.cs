// This file is part of an MIT-licensed project: see LICENSE file or README.md for details.
// Copyright (c) 2025 Ian Thomas

namespace Dink.Tests;

using System.Text;
using DinkCompiler;

/// <summary>
/// A generated TTS clip carries two hashes in its LIST/INFO chunk: DINK (the line's
/// text) and DNKV (the voice request). Changing a character's TTS voice must
/// regenerate that character's clips, while clips stamped before DNKV existed must
/// not all regenerate at once on upgrade.
/// </summary>
public class GoogleTTSHashTests
{
    // A minimal valid WAV: RIFF header, 'fmt ' chunk, empty 'data' chunk.
    private static byte[] MinimalWav()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(0); // fixed up below
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1); bw.Write((short)1); bw.Write(12000); bw.Write(24000);
        bw.Write((short)2); bw.Write((short)16);
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(0);
        ms.Seek(4, SeekOrigin.Begin);
        bw.Write((int)ms.Length - 8);
        bw.Flush();
        return ms.ToArray();
    }

    // What an older dink (or Dinky's scratch recorder) writes: LIST/INFO/DINK only.
    private static byte[] WithOnlyTextHash(byte[] wav, string hash)
    {
        byte[] h = Encoding.UTF8.GetBytes(hash);
        int pad = h.Length % 2;
        using var ms = new MemoryStream();
        ms.Write(wav);
        using var bw = new BinaryWriter(ms);
        bw.Write(Encoding.ASCII.GetBytes("LIST"));
        bw.Write(4 + 8 + h.Length + pad);
        bw.Write(Encoding.ASCII.GetBytes("INFO"));
        bw.Write(Encoding.ASCII.GetBytes("DINK"));
        bw.Write(h.Length);
        bw.Write(h);
        if (pad == 1) bw.Write((byte)0);
        ms.Seek(4, SeekOrigin.Begin);
        bw.Write((int)ms.Length - 8);
        bw.Flush();
        return ms.ToArray();
    }

    private static (string?, string?) Read(byte[] bytes) => GoogleTTS.ReadHashes(new MemoryStream(bytes));

    [Fact]
    public void WrittenClipCarriesBothHashes()
    {
        string hash = GoogleTTS.GenerateHashFromText("Hello there.");
        string voice = GoogleTTS.GenerateVoiceHash("en-GB-Neural2-B");
        byte[] bytes = GoogleTTS.AppendHashes(MinimalWav(), hash, voice);

        Assert.Equal((hash, voice), Read(bytes));
        // RIFF size still covers the whole file.
        Assert.Equal(bytes.Length - 8, BitConverter.ToInt32(bytes, 4));
    }

    [Fact]
    public void TextHashChunkComesFirstAndIsUnchanged()
    {
        // Dinky's scratch recorder and Patterpad read the DINK chunk; it must stay
        // the first sub-chunk, byte for byte as before.
        string hash = GoogleTTS.GenerateHashFromText("Hello there.");
        byte[] wav = MinimalWav();
        byte[] old = WithOnlyTextHash(wav, hash);
        byte[] now = GoogleTTS.AppendHashes(wav, hash, GoogleTTS.GenerateVoiceHash("en-GB-Neural2-B"));

        int listAt = wav.Length;
        int dinkLen = old.Length - listAt - 12; // after LIST, size, INFO
        Assert.Equal(old.AsSpan(listAt + 12, dinkLen).ToArray(), now.AsSpan(listAt + 12, dinkLen).ToArray());
    }

    [Fact]
    public void OldClipHasNoVoiceHash()
    {
        string hash = GoogleTTS.GenerateHashFromText("Hello there.");
        Assert.Equal((hash, (string?)null), Read(WithOnlyTextHash(MinimalWav(), hash)));
    }

    [Fact]
    public void VoiceHashDependsOnTheVoice()
    {
        Assert.Equal(GoogleTTS.GenerateVoiceHash("en-GB-Neural2-B"), GoogleTTS.GenerateVoiceHash("en-GB-Neural2-B"));
        Assert.NotEqual(GoogleTTS.GenerateVoiceHash("en-GB-Neural2-B"), GoogleTTS.GenerateVoiceHash("en-GB-Neural2-D"));
        Assert.Equal(16, GoogleTTS.GenerateVoiceHash("en-GB-Neural2-B").Length);
    }

    [Fact]
    public void RegeneratesWhenVoiceOrTextChanges()
    {
        string text = GoogleTTS.GenerateHashFromText("Hello there.");
        string voiceB = GoogleTTS.GenerateVoiceHash("en-GB-Neural2-B");
        string voiceD = GoogleTTS.GenerateVoiceHash("en-GB-Neural2-D");

        Assert.True(GoogleTTS.IsUpToDate(text, voiceB, text, voiceB));
        Assert.False(GoogleTTS.IsUpToDate(text, voiceB, text, voiceD)); // voice changed
        Assert.False(GoogleTTS.IsUpToDate("other", voiceB, text, voiceB)); // text changed
        Assert.False(GoogleTTS.IsUpToDate(null, null, text, voiceB)); // no stamp at all
    }

    [Fact]
    public void ClipWithoutVoiceHashCountsAsMatching()
    {
        // Upgrading dink must not regenerate (and bill) a whole project at once.
        string text = GoogleTTS.GenerateHashFromText("Hello there.");
        Assert.True(GoogleTTS.IsUpToDate(text, null, text, GoogleTTS.GenerateVoiceHash("en-GB-Neural2-D")));
    }
}
