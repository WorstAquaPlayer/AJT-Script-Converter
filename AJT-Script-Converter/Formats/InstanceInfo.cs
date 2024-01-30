namespace AJT_Script_Converter.Formats;

public class InstanceInfo
{
    public uint TypeId { get; set; }
    public uint Crc { get; set; }

    public InstanceInfo(uint typeId, uint crc)
    {
        TypeId = typeId;
        Crc = crc;
    }
}
