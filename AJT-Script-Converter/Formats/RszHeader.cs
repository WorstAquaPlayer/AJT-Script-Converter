using Yarhl.IO;

namespace AJT_Script_Converter.Formats;

public class RszHeader
{
    public const string Magic = "RSZ";
    public long ReaderStartPosition { get; private set; }
    public long DataOffset { get; private set; }
    public List<InstanceInfo> InstanceInfos { get; } = new();

    public RszHeader(DataReader reader)
    {
        ReaderStartPosition = reader.Stream.Position;
        var magic = reader.ReadString(4);

        if (magic != $"{Magic}\0")
        {
            throw new Exception($"Invalid .user file. Wrong RSZ Magic String: {magic}");
        }

        var version = reader.ReadUInt32();

        if (version != 16)
        {
            throw new Exception($"Unsupported RSZ version: {version}");
        }

        var objectCount = reader.ReadInt32();

        if (objectCount != 1)
        {
            throw new Exception($"Script RSZ should only have one object");
        }

        var instanceCount = reader.ReadInt32();
        reader.ReadInt32(); // userdataCount

        var unknown = reader.ReadInt32();
        if (unknown != 0)
        {
            throw new Exception("Unknown value in RSZ is not 0");
        }

        var instanceOffset = reader.ReadInt64();
        DataOffset = reader.ReadInt64();
        var userdataOffset = reader.ReadInt64();

        if (DataOffset != userdataOffset)
        {
            throw new Exception("Unexpected offset differences on RSZ");
        }

        var scriptChunks = reader.ReadInt32();

        if (reader.Stream.Position - ReaderStartPosition != instanceOffset)
        {
            throw new Exception("Reader Stream Position isn't where it should (InstanceOffset)");
        }

        if (instanceCount - 1 != scriptChunks)
        {
            throw new Exception($"Mismatching values between InstanceCount ({instanceCount - 1}) and ScriptChunks ({scriptChunks})");
        }

        for (int i = 0; i < instanceCount; i++)
        {
            var typeId = reader.ReadUInt32();
            var crc = reader.ReadUInt32();

            InstanceInfos.Add(new(typeId, crc));
        }

        reader.Stream.Position = ReaderStartPosition + DataOffset;
    }
}
