using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class AquaBuildBytes
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    public static extern uint GetACP();

    public static byte[] ReadVerified(string path, string hash)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
        {
            string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            if (!actual.Equals(hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unexpected file version: " + Path.GetFileName(path));
        }
        return File.ReadAllBytes(path);
    }

    public static void Patch(string path, string hash, int offset, byte[] before, byte[] after)
    {
        byte[] data = ReadVerified(path, hash);
        if (before.Length != after.Length || offset < 0 || offset + before.Length > data.Length)
            throw new InvalidDataException("Invalid patch range");
        for (int i = 0; i < before.Length; i++)
            if (data[offset + i] != before[i])
                throw new InvalidDataException("Unexpected patch bytes: " + Path.GetFileName(path));
        Buffer.BlockCopy(after, 0, data, offset, after.Length);
        File.WriteAllBytes(path, data);
    }

    public static byte[] ReplaceUnique(byte[] data, byte[] before, byte[] after)
    {
        if (before.Length != after.Length)
            throw new InvalidDataException("Replacement must preserve length");
        int found = -1;
        for (int i = 0; i <= data.Length - before.Length; i++)
        {
            int j = 0;
            while (j < before.Length && data[i + j] == before[j]) j++;
            if (j != before.Length) continue;
            if (found >= 0) throw new InvalidDataException("Expected a single replacement match");
            found = i;
        }
        if (found < 0) throw new InvalidDataException("Replacement target is missing");
        byte[] output = (byte[])data.Clone();
        Buffer.BlockCopy(after, 0, output, found, after.Length);
        return output;
    }

    public static void ExtractCabinet(string source, string target)
    {
        byte[] data = File.ReadAllBytes(source);
        for (int offset = 0; offset <= data.Length - 36; offset++)
        {
            if (data[offset] != 'M' || data[offset + 1] != 'S' ||
                data[offset + 2] != 'C' || data[offset + 3] != 'F') continue;
            uint size = BitConverter.ToUInt32(data, offset + 8);
            if (BitConverter.ToUInt32(data, offset + 4) != 0 ||
                BitConverter.ToUInt32(data, offset + 12) != 0 ||
                size < 36 || size > data.Length - offset) continue;
            using (var stream = File.Create(target))
                stream.Write(data, offset, (int)size);
            return;
        }
        throw new InvalidDataException("No cabinet in the accessory installer");
    }

    public static void ExpandSzdd(string source, string target)
    {
        byte[] data = File.ReadAllBytes(source);
        byte[] signature = { 0x53, 0x5a, 0x44, 0x44, 0x88, 0xf0, 0x27, 0x33, 0x41 };
        bool compressed = data.Length >= 14;
        for (int i = 0; i < signature.Length && compressed; i++)
            compressed = data[i] == signature[i];
        if (!compressed)
        {
            File.WriteAllBytes(target, data);
            return;
        }
        byte[] output = new byte[BitConverter.ToUInt32(data, 10)];
        byte[] window = new byte[4096];
        for (int i = 0; i < window.Length; i++) window[i] = 32;
        int read = 14, write = 0, cursor = 4080;
        while (write < output.Length)
        {
            int flags = data[read++];
            for (int bit = 0; bit < 8 && write < output.Length; bit++)
            {
                if ((flags & (1 << bit)) != 0)
                {
                    byte literal = data[read++];
                    output[write++] = window[cursor] = literal;
                    cursor = (cursor + 1) & 4095;
                    continue;
                }
                int low = data[read++], high = data[read++];
                int offset = low | ((high & 240) << 4), length = (high & 15) + 3;
                if (write + length > output.Length)
                    throw new InvalidDataException("Invalid SZDD output length");
                for (int i = 0; i < length; i++)
                {
                    byte literal = window[(offset + i) & 4095];
                    output[write++] = window[cursor] = literal;
                    cursor = (cursor + 1) & 4095;
                }
            }
        }
        File.WriteAllBytes(target, output);
    }
}
