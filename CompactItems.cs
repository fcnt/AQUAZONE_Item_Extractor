using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public sealed class AquaCompactItem
{
    public string Category { get; set; }
    public int ResourceId { get; set; }
    public string Name { get; set; }
    public string Source { get; set; }
    public string File { get; set; }
}

public static class AquaCompactItems
{
    private sealed class Resource
    {
        public string Type;
        public short Id;
        public byte[] Name;
        public byte[] Data;
    }

    private static void CheckRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || length < 0 || (long)offset + length > data.Length)
            throw new InvalidDataException("Resource range is outside the file");
    }

    private static byte[] Slice(byte[] data, int offset, int length)
    {
        CheckRange(data, offset, length);
        byte[] result = new byte[length];
        Buffer.BlockCopy(data, offset, result, 0, length);
        return result;
    }

    private static List<Resource> Read(byte[] data)
    {
        CheckRange(data, 0, 256);
        int start = BitConverter.ToInt32(data, 0);
        int map = BitConverter.ToInt32(data, 4);
        int dataSize = BitConverter.ToInt32(data, 8);
        CheckRange(data, start, dataSize);
        CheckRange(data, map, BitConverter.ToInt32(data, 12));
        CheckRange(data, map, 30);
        if (start != 256 || (long)start + dataSize != map || BitConverter.ToInt32(data, 20) != 1500)
            throw new InvalidDataException("Expected an AQUAZONE 1.5 resource file");
        int types = map + BitConverter.ToUInt16(data, map + 24);
        int names = map + BitConverter.ToUInt16(data, map + 26);
        int count = 1 + BitConverter.ToUInt16(data, map + 28);
        CheckRange(data, types, count * 8);
        var result = new List<Resource>();
        for (int i = 0; i < count; i++)
        {
            int entry = types + i * 8;
            char[] type = Encoding.ASCII.GetChars(data, entry, 4);
            Array.Reverse(type);
            int number = 1 + BitConverter.ToUInt16(data, entry + 4);
            int references = types + BitConverter.ToUInt16(data, entry + 6);
            CheckRange(data, references, number * 12);
            for (int j = 0; j < number; j++)
            {
                int reference = references + j * 12;
                int nameOffset = BitConverter.ToUInt16(data, reference + 2);
                byte[] name = null;
                if (nameOffset != 65535)
                {
                    CheckRange(data, names + nameOffset, 1);
                    name = Slice(data, names + nameOffset + 1, data[names + nameOffset]);
                }
                int offset = checked(start + (int)(BitConverter.ToUInt32(data, reference + 4) & 0xffffff));
                CheckRange(data, offset, 4);
                int length = BitConverter.ToInt32(data, offset);
                if (length < 0 || (long)offset + 4 + length > map)
                    throw new InvalidDataException("Resource payload is outside the data section");
                result.Add(new Resource {
                    Type = new string(type), Id = BitConverter.ToInt16(data, reference),
                    Name = name, Data = Slice(data, offset + 4, length)
                });
            }
        }
        return result;
    }

    private static string PascalName(byte[] data)
    {
        if (data == null || data.Length == 0 || data[0] >= data.Length) return "";
        return Encoding.GetEncoding(932).GetString(data, 1, data[0]);
    }

    private static void WriteItem(string path, byte[] original, string signature, List<Resource> resources)
    {
        var types = resources.GroupBy(r => r.Type).ToList();
        var offsets = new List<int>();
        byte[] payload;
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            foreach (var group in types)
                foreach (Resource resource in group)
                {
                    offsets.Add(checked((int)stream.Position));
                    writer.Write(resource.Data.Length);
                    writer.Write(resource.Data);
                }
            payload = stream.ToArray();
        }
        byte[] map;
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        using (var nameStream = new MemoryStream())
        using (var nameWriter = new BinaryWriter(nameStream))
        {
            int nameStart = 30 + types.Count * 8 + resources.Count * 12;
            if (nameStart > ushort.MaxValue || payload.Length > 0xffffff)
                throw new InvalidDataException("Item exceeds the resource format limits");
            writer.Write(new byte[16]);
            writer.Write(Slice(original, BitConverter.ToInt32(original, 4) + 16, 8));
            writer.Write((ushort)30);
            writer.Write((ushort)nameStart);
            writer.Write((ushort)(types.Count - 1));
            int references = types.Count * 8;
            foreach (var group in types)
            {
                char[] type = group.Key.ToCharArray();
                Array.Reverse(type);
                writer.Write(Encoding.ASCII.GetBytes(type));
                writer.Write((ushort)(group.Count() - 1));
                writer.Write((ushort)references);
                references += group.Count() * 12;
            }
            int index = 0;
            foreach (var group in types)
                foreach (Resource resource in group)
                {
                    writer.Write(resource.Id);
                    if (resource.Name == null) writer.Write(ushort.MaxValue);
                    else
                    {
                        if (nameStream.Position >= ushort.MaxValue || resource.Name.Length > 255)
                            throw new InvalidDataException("Resource name exceeds the format limits");
                        writer.Write((ushort)nameStream.Position);
                        nameWriter.Write((byte)resource.Name.Length);
                        nameWriter.Write(resource.Name);
                    }
                    writer.Write(offsets[index++]);
                    writer.Write(0);
                }
            writer.Write(nameStream.ToArray());
            map = stream.ToArray();
        }
        byte[] header = Slice(original, 0, 256);
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(signature), 0, header, 16, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(256), 0, header, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(256 + payload.Length), 0, header, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(payload.Length), 0, header, 8, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(map.Length), 0, header, 12, 4);
        Buffer.BlockCopy(header, 0, map, 0, 16);
        using (var stream = File.Create(path))
        {
            stream.Write(header, 0, header.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Write(map, 0, map.Length);
        }
        Read(File.ReadAllBytes(path));
    }

    private static string Fingerprint(IEnumerable<Resource> resources)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        using (var sha = SHA256.Create())
        {
            foreach (Resource resource in resources.OrderBy(r => r.Type, StringComparer.Ordinal).ThenBy(r => r.Id))
            {
                writer.Write(resource.Type);
                writer.Write(resource.Id);
                writer.Write(resource.Data.Length);
                writer.Write(resource.Data);
            }
            return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").Substring(0, 12);
        }
    }

    public static void ConvertClassicsFish(string source, string destination, string plecoGraphics)
    {
        if (File.Exists(destination)) throw new IOException("File already exists: " + destination);
        byte[] original = File.ReadAllBytes(source);
        List<Resource> resources = Read(original);
        Resource definition = resources.Single(r => r.Type == "FsTI");
        Resource version = resources.Single(r => r.Type == "FiVe" && r.Id == definition.Id);
        int expectedVersion;
        switch (definition.Id)
        {
            case 3400: case 3600: expectedVersion = 169; break;
            case 3800: expectedVersion = 171; break;
            case 23000: expectedVersion = 180; break;
            default: throw new InvalidDataException("Unsupported Classics fish species ID");
        }
        if (definition.Data.Length != 210 || version.Data.Length != 2 ||
            BitConverter.ToUInt16(version.Data, 0) != expectedVersion ||
            resources.Single(r => r.Type == "FsTH" && r.Id == definition.Id).Data.Length != 1128 ||
            resources.Single(r => r.Type == "FsT2" && r.Id == definition.Id).Data.Length != 102)
            throw new InvalidDataException("Unexpected fish definition layout");
        var individuals = resources.Where(r => r.Type == "FshI").ToList();
        var histories = resources.Where(r => r.Type == "FshH").ToList();
        if (individuals.Count == 0 || individuals.Count != histories.Count ||
            individuals.Any(r => r.Data.Length != 464) || histories.Any(r => r.Data.Length != 1134) ||
            !new HashSet<short>(individuals.Select(r => r.Id)).SetEquals(histories.Select(r => r.Id)))
            throw new InvalidDataException("Expected complete Compact-generated fish individuals");
        if (resources.Any(r => r.Type == "FiFV" || r.Type == "FsT3"))
            throw new InvalidDataException("Generate the fish with the Compact CD FGW.EXE before conversion");

        // Royal pleco DNA contains empty sprite records. Its original animation is in GP23000.rez.
        if (definition.Id == 23000)
        {
            if (string.IsNullOrEmpty(plecoGraphics)) throw new InvalidDataException("GP23000.rez is required for royal pleco");
            List<Resource> bank = Read(File.ReadAllBytes(plecoGraphics));
            foreach (Resource resource in resources.Where(r =>
                r.Type == "ELRA" || r.Type == "ELRB" || r.Type == "AMV#" || r.Type == "BMV#" || r.Type == "EGPC"))
            {
                Resource artwork = bank.Single(r => r.Type == resource.Type && r.Id == resource.Id);
                if (artwork.Data.Length <= 8) throw new InvalidDataException("Royal pleco animation is missing");
                resource.Data = artwork.Data;
            }
        }
        // Animation tables use big-endian frame numbers, while sprite headers use little-endian counts.
        foreach (Resource animation in resources.Where(r => r.Type == "AMV#" || r.Type == "BMV#"))
        {
            string imageType = animation.Type == "AMV#" ? "ELRA" : "ELRB";
            Resource image = resources.Single(r => r.Type == imageType && r.Id == animation.Id);
            if (image.Data.Length < 8 || animation.Data.Length < 2)
                throw new InvalidDataException("Incomplete fish animation");
            int frames = BitConverter.ToUInt16(image.Data, 2);
            int entries = animation.Data[0] * 256 + animation.Data[1];
            if (frames == 0 || animation.Data.Length != 2 + entries * 2)
                throw new InvalidDataException("Unexpected fish animation layout");
            for (int entry = 0; entry < entries; entry++)
            {
                int offset = 2 + entry * 2;
                int frame = animation.Data[offset] * 256 + animation.Data[offset + 1];
                if (frame < frames) continue;
                // The supplied arowana tables reference 8/16 frames, but the embedded sprites provide four.
                if (definition.Id != 3400) throw new InvalidDataException("Animation references an absent fish frame");
                frame %= frames;
                animation.Data[offset] = (byte)(frame >> 8);
                animation.Data[offset + 1] = (byte)frame;
            }
        }
        foreach (string type in new[] { "ELRA", "ELRB" })
        {
            for (int frameSet = 0; frameSet < 4; frameSet++)
            {
                Resource artwork = resources.Single(r => r.Type == type && r.Id == definition.Id + frameSet);
                if (artwork.Data.Length <= 8) throw new InvalidDataException("Fish animation is missing");
            }
        }
        // Species definitions, living individuals, histories, and animation payloads retain their original layouts.
        version.Data = new byte[] { 150, 0 };
        WriteItem(destination, original, Encoding.ASCII.GetString(original, 16, 4), resources);
    }

    public static AquaCompactItem[] Extract(string source, string destination)
    {
        byte[] original = File.ReadAllBytes(source);
        List<Resource> all = Read(original);
        var output = new List<AquaCompactItem>();
        // Each family contains the item definition, artwork, and optional individuals.
        string[][] families = {
            new[] { "Plants", "plt", "PlTI", "PlTH", "PLDP", "PLPC", "PlVe", "lPqA" },
            new[] { "Accessories", "acc", "AccI", "AccH", "ACDP", "ACPC", "AcVe", "cAqA" },
            new[] { "Food", "fd", "Fd_I", "Fd_H", "FDDP", "FDPC", "FdVe", "dFqA" },
            new[] { "Medicine", "med", "DrgI", "DrgH", "DRDP", "", "DrVe", "rDqA" },
            new[] { "Gravel", "grv", "Grvl", "", "BADP", "BAPC", "", "rGqA" }
        };
        foreach (string[] family in families)
        {
            var definitions = all.Where(r => r.Type == family[2]).OrderBy(r => r.Id).ToList();
            for (int i = 0; i < definitions.Count; i++)
            {
                Resource definition = definitions[i];
                int next = i + 1 < definitions.Count ? definitions[i + 1].Id : 32768;
                var selected = all.Where(r =>
                    ((r.Type == family[2] || r.Type == family[3]) && r.Id == definition.Id) ||
                    ((r.Type == family[4] || r.Type == family[5]) && r.Id >= definition.Id && r.Id < next)).ToList();
                if (family[0] == "Gravel") selected = selected.Where(r => r.Id == definition.Id).ToList();
                Resource description = selected.FirstOrDefault(r => r.Type == family[3]);
                string name = description == null ? "" : PascalName(description.Data);
                if (family[0] == "Plants")
                {
                    name = definition.Name == null ? "" : Encoding.GetEncoding(932).GetString(definition.Name);
                    var individuals = all.Where(r => r.Type == "PlPI" && PascalName(r.Data) == name).ToList();
                    if (individuals.Count == 0) continue;
                    for (int individualIndex = 0; individualIndex < individuals.Count; individualIndex++)
                    {
                        Resource individual = individuals[individualIndex];
                        short localId = checked((short)(128 + individualIndex));
                        selected.Add(new Resource { Type = individual.Type, Id = localId, Name = individual.Name, Data = individual.Data });
                        Resource history = all.First(r => r.Type == "PlPH" && r.Id == individual.Id);
                        selected.Add(new Resource { Type = history.Type, Id = localId, Name = history.Name, Data = history.Data });
                    }
                }
                if (!selected.Any(r => r.Type == family[4])) continue;
                string hash = Fingerprint(selected.Where(r => r.Type != family[2] && r.Type != "PlPI" && r.Type != "PlPH"));
                if (family[6] != "") selected.Add(new Resource { Type = family[6], Id = definition.Id, Data = new byte[] { 150, 0 } });
                string folder = Path.Combine(destination, family[0]);
                Directory.CreateDirectory(folder);
                string leaf = definition.Id.ToString("D5") + "-" + hash + "." + family[1];
                string path = Path.Combine(folder, leaf);
                if (File.Exists(path)) continue;
                WriteItem(path, original, family[7], selected);
                output.Add(new AquaCompactItem { Category = family[0], ResourceId = definition.Id, Name = name,
                    Source = Path.GetFileName(source), File = family[0] + "/" + leaf });
            }
        }
        var gravelIds = new HashSet<short>(all.Where(r => r.Type == "Grvl").Select(r => r.Id));
        foreach (Resource background in all.Where(r => r.Type == "BAPC" && !gravelIds.Contains(r.Id)))
        {
            byte[] bitmap = background.Data;
            if (bitmap.Length < 54 || bitmap[0] != 'B' || bitmap[1] != 'M' ||
                BitConverter.ToInt32(bitmap, 18) != 640 || BitConverter.ToInt32(bitmap, 22) != 480) continue;
            string folder = Path.Combine(destination, "Backgrounds");
            Directory.CreateDirectory(folder);
            string leaf = background.Id.ToString("D5") + "-" + Fingerprint(new[] { background }) + ".bmp";
            string path = Path.Combine(folder, leaf);
            if (File.Exists(path)) continue;
            File.WriteAllBytes(path, bitmap);
            output.Add(new AquaCompactItem { Category = "Backgrounds", ResourceId = background.Id, Name = "",
                Source = Path.GetFileName(source), File = "Backgrounds/" + leaf });
        }
        return output.ToArray();
    }
}
