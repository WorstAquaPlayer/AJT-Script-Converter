namespace AJT_Script_Converter.Formats;

public class ScriptSection
{
    public const uint TypeId = 0x83F3F042;
    public const uint Crc = 0xB263156;

    public string Label { get; set; }
    public string Data { get; set; }

    public ScriptSection(string label, string data)
    {
        Label = label;
        Data = data;
    }
}
