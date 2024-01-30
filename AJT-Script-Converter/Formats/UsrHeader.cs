using Yarhl.IO;

namespace AJT_Script_Converter.Formats;

public class UsrHeader
{
    public const string Magic = "USR";

    public UsrHeader(DataReader reader)
    {
        var magic = reader.ReadString(4);

        if (magic != $"{Magic}\0")
        {
            throw new Exception($"Invalid .user file. Wrong USR Magic String: {magic}");
        }

        reader.ReadInt32(); // resourceCount
        reader.ReadInt32(); // userdataCount
        reader.ReadInt32(); // infoCount

        var resourceTableOffset = reader.ReadInt64();
        var userdataTableOffset = reader.ReadInt64();
        var dataOffset = reader.ReadInt64();

        if (resourceTableOffset != userdataTableOffset && userdataTableOffset != dataOffset)
        {
            throw new Exception("Unexpected offset differences on USR");
        }

        reader.Stream.Position = dataOffset;
    }
}
