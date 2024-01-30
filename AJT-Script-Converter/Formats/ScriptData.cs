using Yarhl.IO;

namespace AJT_Script_Converter.Formats;

public class ScriptData
{
    public const uint TypeId = 0xEE933AA7;
    public const uint Crc = 0x1AA1A4AC;

    public string FileName { get; set; }
    public List<ScriptSection> Sections { get; } = new();

    public ScriptData(DataReader reader, string fileName, List<ScriptSection> loadedSections)
    {
        FileName = fileName;
        var size = reader.ReadInt32();

        for (int i = 0; i < size; i++)
        {
            var currentIndex = reader.ReadInt32();
            Sections.Add(loadedSections[currentIndex - 1]);
            
            if (currentIndex - 1 != i)
            {
                throw new Exception("Unexpected value on CurrentIndex");
            }
        }
    }
}
