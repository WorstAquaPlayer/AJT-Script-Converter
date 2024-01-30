using AJT_Script_Converter.Formats;
using System.Text;
using System.Text.RegularExpressions;
using Yarhl.IO;

namespace AJT_Script_Converter;

public enum GameType
{
    Unknown,
    GS4,
    GS56
}

internal class Program
{
    static void Main(string[] args)
    {
        int argc = args.Length;

        if (argc == 0)
        {
            Usage();
        }
        else
        {
            foreach(var arg in args)
            {
                if (arg.EndsWith(".txt"))
                {
                    Insert(arg);
                }
                else
                {
                    Extract(arg);
                }
            }
        }
    }

    static void Usage()
    {
        Console.WriteLine("Drag and drop file(s) into the executable!");
    }

    static void Insert(string filePath)
    {
        var fullText = File.ReadAllText(filePath);
        fullText = fullText.Replace("\r\n", "\n").Replace("<PAGE>\n", "<PAGE>");

        var lines = fullText.Split('\n');

        var fileName = lines[0];

        var sections = new List<ScriptSection>();

        string? label = null;
        var data = new StringBuilder();

        for (int i = 2; i < lines.Length; i++)
        {
            var line = lines[i];

            if (string.IsNullOrEmpty(line))
            {
                if (label == null)
                {
                    continue;
                }

                var dataString = data.ToString();

                if (dataString.EndsWith("\r\n"))
                {
                    dataString = dataString.Substring(0, dataString.Length - 2);
                }

                sections.Add(new(label, dataString));
                label = null;
                data.Clear();
                continue;
            }

            if (line.StartsWith('['))
            {
                label = Regex.Match(line, @"(?<=\[).*?(?=\])").Value;
            }
            else
            {
                data.AppendLine(line);
            }
        }

        var outFile = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath));

        using var outBinary = new BinaryFormat();
        var writer = new DataWriter(outBinary.Stream);

        writer.Write("USR\0");
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write((ulong)0x30);
        writer.Write((ulong)0x30);
        writer.Write((ulong)0x30);
        writer.WritePadding(0, 0x10);

        var rszOffset = writer.Stream.Position;
        writer.Write("RSZ\0");
        writer.Write(16);
        writer.Write(1);
        writer.Write(sections.Count + 2); // instanceCount
        writer.Write(0); // userdataCount
        writer.Write(0); // unknown

        var offsetsPosition = writer.Stream.Position;
        writer.Write((ulong)0); // temp instanceOffset
        writer.Write((ulong)0); // temp dataOffset
        writer.Write((ulong)0); // temp userdataOffset

        writer.Write(sections.Count + 1); // object

        var instanceOffsetPosition = writer.Stream.Position;
        writer.Write((long)0); // null object

        for (int i = 0; i < sections.Count;  i++)
        {
            writer.Write(ScriptSection.TypeId);
            writer.Write(ScriptSection.Crc);
        }

        writer.Write(ScriptData.TypeId);
        writer.Write(ScriptData.Crc);

        writer.WritePadding(0, 0x10);
        var dataOffsetPosition = writer.Stream.Position;

        writer.Stream.Position = offsetsPosition;
        writer.Write((ulong)(instanceOffsetPosition - rszOffset));
        writer.Write((ulong)(dataOffsetPosition - rszOffset));
        writer.Write((ulong)(dataOffsetPosition - rszOffset));

        writer.Stream.Position = dataOffsetPosition;

        foreach (var section in sections)
        {
            writer.Write(section.Label.Length + 1);
            var sectionLabel = Encoding.Unicode.GetBytes(section.Label);
            writer.Write(sectionLabel);
            writer.Write((short)0);

            writer.WritePadding(0, 4);

            writer.Write(section.Data.Length + 1);
            var sectionData = Encoding.Unicode.GetBytes(section.Data);
            writer.Write(sectionData);
            writer.Write((short)0);

            writer.WritePadding(0, 4);
        }

        writer.Write(fileName.Length + 1);
        var fileNameBytes = Encoding.Unicode.GetBytes(fileName);
        writer.Write(fileNameBytes);
        writer.Write((short)0);

        writer.WritePadding(0, 4);

        writer.Write(sections.Count);
        for (int i = 0; i < sections.Count;  i++)
        {
            writer.Write(i + 1);
        }

        outBinary.Stream.WriteTo(outFile);
    }

    static void Extract(string filePath)
    {
        using var scriptStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var reader = new DataReader(scriptStream);

        var usrHeader = new UsrHeader(reader);
        var rszHeader = new RszHeader(reader);

        var instanceInfos = rszHeader.InstanceInfos;
        var gameType = GameType.Unknown;

        foreach (var instanceInfo in instanceInfos)
        {
            switch (instanceInfo.TypeId)
            {
                case 0:
                    continue;
                case 0xDAA48445:
                    gameType = GameType.GS4;
                    break;
                case ScriptData.TypeId:
                case ScriptSection.TypeId:
                    gameType = GameType.GS56;
                    break;
                default:
                    throw new Exception("Unknown Instance Type in file");
            }
        }

        switch (gameType)
        {
            case GameType.GS4:
                ExtractGS4(reader, filePath);
                break;
            case GameType.GS56:
                ExtractGS56(reader, filePath, instanceInfos);
                break;
            default:
                throw new Exception("Unsupported file");
        }
    }

    static void ExtractGS4(DataReader reader, string filePath)
    {
        var binarySize = reader.ReadInt32();
        var binaryBytes = reader.ReadBytes(binarySize);

        var outBinFile = $"{filePath}.bin";
        File.WriteAllBytes(outBinFile, binaryBytes);
    }

    static void ExtractGS56(DataReader reader, string filePath, List<InstanceInfo> instanceInfos)
    {
        ScriptData? scriptData = null;
        var scriptSections = new List<ScriptSection>();

        foreach (var instanceInfo in instanceInfos)
        {
            if (instanceInfo.TypeId == ScriptSection.TypeId)
            {
                var labelSize = reader.ReadInt32();
                var labelBytes = reader.ReadBytes(labelSize * 2);
                var label = Encoding.Unicode.GetString(labelBytes);

                if (!label.EndsWith('\0'))
                {
                    throw new Exception("Label should be null terminated");
                }

                label = label.Substring(0, label.Length - 1);
                reader.SkipPadding(4);

                var dataSize = reader.ReadInt32();
                var dataBytes = reader.ReadBytes(dataSize * 2);
                var data = Encoding.Unicode.GetString(dataBytes);

                if (!data.EndsWith('\0'))
                {
                    throw new Exception("Data should be null terminated");
                }

                data = data.Substring(0, data.Length - 1);

                var section = new ScriptSection(label, data);
                scriptSections.Add(section);

                reader.SkipPadding(4);
            }
            else if (instanceInfo.TypeId == ScriptData.TypeId)
            {
                var fileNameSize = reader.ReadInt32();
                var fileNameBytes = reader.ReadBytes(fileNameSize * 2);
                var fileName = Encoding.Unicode.GetString(fileNameBytes);

                if (!fileName.EndsWith('\0'))
                {
                    throw new Exception("FileName should be null terminated");
                }

                fileName = fileName.Substring(0, fileName.Length - 1);
                reader.SkipPadding(4);

                scriptData = new ScriptData(reader, fileName, scriptSections);
            }
        }

        if (scriptData == null)
        {
            throw new Exception("ScriptData was not defined properly!");
        }

        Encoding enc = Encoding.GetEncoding("utf-8");

        var outTxtFile = $"{filePath}.txt";
        using StreamWriter sw = new StreamWriter(File.Create(outTxtFile), enc);

        sw.WriteLine($"{scriptData.FileName}");
        sw.WriteLine();

        for (int i = 0; i < scriptData.Sections.Count; i++)
        {
            var section = scriptData.Sections[i];

            sw.WriteLine($"[{section.Label}]");
            var text = section.Data.Replace("<PAGE>", "<PAGE>\r\n");
            sw.WriteLine(text);
            sw.WriteLine();
        }
    }
}
