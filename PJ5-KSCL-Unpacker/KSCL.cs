using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PJ5_KSCL_Unpacker
{
    public static class KSCL
    {
        private struct Header
        {
            public long KSCLLength;
            public int TexCount;
            public int HeaderlessSize;
            public int TablePointer;
            public int NameLength;
            public int NameCount;
        }
        private struct KSLT_Texture
        {
            public string Name;
            public int Pointer;
            public int FormatType;
            public short Width;
            public short Height;
            public int RawSize;
            public byte[] RawData;
        }
        private static Header ReadHeader(ref BinaryReader reader)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            Header header = new Header();
            int ksclMagic = reader.ReadInt32();
            if (ksclMagic != 0x4B53434C) throw new Exception("Unsupported file type.");
            reader.ReadInt32();
            header.KSCLLength = reader.ReadInt32() + 0x8C;
            reader.BaseStream.Position = header.KSCLLength;
            int ksltMagic = reader.ReadInt32();
            reader.ReadInt32();
            if (ksltMagic != 0x4B534C54) throw new Exception("Unsupported file type.");
            header.TexCount = reader.ReadInt32();
            header.HeaderlessSize = reader.ReadInt32();
            header.TablePointer = reader.ReadInt32();
            header.NameLength = reader.ReadInt32();
            header.NameCount = reader.ReadInt32();
            return header;
        }
        private static string ReadString(ref BinaryReader reader)
        {
            StringBuilder str = new StringBuilder();
            byte[] ch = reader.ReadBytes(1);
            while (ch[0] != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
            {
                str.Append(Encoding.ASCII.GetString(ch));
                ch = reader.ReadBytes(1);
            }
            return str.ToString();
        }
        private static byte[] GetDDSHeader(int format)
        {
            switch (format)
            {
                case 0:
                    return Properties.Resources.R8G8B8A8_Header;
                case 3:
                    return Properties.Resources.BC3DXT5_Header;
                default:
                    throw new Exception($"Unsupported format type: {format}");
            }
        }
        private static KSLT_Texture[] GetTextures(ref BinaryReader reader, Header header)
        {
            reader.BaseStream.Position = header.TablePointer + header.KSCLLength + 0x40 + (0x14 * header.TexCount);
            string[] names = new string[header.TexCount];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = ReadString(ref reader);
            }
            reader.BaseStream.Position = header.TablePointer + header.KSCLLength + 0x40;
            KSLT_Texture[] textures = new KSLT_Texture[header.TexCount];
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i].Pointer = reader.ReadInt32();
                textures[i].Name = names[i];
                long temp = reader.BaseStream.Position + 0x10;
                reader.BaseStream.Position = textures[i].Pointer + header.KSCLLength;
                textures[i].FormatType = reader.ReadInt32();
                textures[i].Width = reader.ReadInt16();
                textures[i].Height = reader.ReadInt16();
                reader.BaseStream.Position += 0x14;
                textures[i].RawSize = reader.ReadInt32();
                reader.BaseStream.Position += 0x28;
                textures[i].RawData = reader.ReadBytes(textures[i].RawSize);
                reader.BaseStream.Position = temp;
            }
            return textures;
        }
        public static void Unpack(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                BinaryReader reader = new BinaryReader(stream);
                Header header = ReadHeader(ref reader);
                string exportPath = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);
                KSLT_Texture[] textures = GetTextures(ref reader, header);
                for (int i = 0; i < textures.Length; i++)
                {
                    byte[] texBytes = new byte[0x80 + textures[i].RawSize];
                    byte[] ddsHeader = GetDDSHeader(textures[i].FormatType);
                    ddsHeader.CopyTo(texBytes, 0);
                    MemoryStream texStream = new MemoryStream(texBytes);
                    using (BinaryWriter writer = new BinaryWriter(texStream))
                    {
                        writer.BaseStream.Seek(0xC, SeekOrigin.Begin);
                        writer.Write((int)textures[i].Height);
                        writer.Write((int)textures[i].Width);
                        writer.Write(textures[i].RawSize);
                        writer.BaseStream.Position = 0x80;
                        writer.Write(textures[i].RawData);
                    }
                    File.WriteAllBytes(Path.Combine(exportPath, $"{textures[i].Name}.dds"), texStream.ToArray());
                    Console.WriteLine($"Exported: {Path.GetFileNameWithoutExtension(file)}/{textures[i].Name}.dds");
                    texStream.Close();
                }
                reader.Close();
            }
        }
        public static byte[] Repack(string exported, string file)
        {
            MemoryStream result = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(result))
            {
                using (FileStream stream = File.OpenRead(file))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    Header header = ReadHeader(ref reader);
                    KSLT_Texture[] textures = GetTextures(ref reader, header);
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    writer.Write(reader.ReadBytes((int)reader.BaseStream.Length));
                    foreach (var tex in textures)
                    {
                        string importPath = Path.Combine(exported, $"{tex.Name}.dds");
                        if (!File.Exists(importPath)) continue;
                        byte[] importData = File.ReadAllBytes(importPath).Skip(0x80).ToArray();
                        if (importData.Length != tex.RawSize)
                        {
                            Console.WriteLine($"{tex.Name}: Import data size does not match the original size!");
                            continue;
                        }
                        writer.BaseStream.Position = tex.Pointer + header.KSCLLength + 0x48;
                        writer.Write(importData);
                    }
                }
            }
            return result.ToArray();
        }
        
    }
}
