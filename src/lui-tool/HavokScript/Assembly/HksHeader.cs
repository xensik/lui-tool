
namespace LuiTool.HavokScript.Assembly;

public class HksHeader
{
    public uint Signature { get; set; }
    public byte LuaVersion { get; set; }
    public HksFormat FormatVersion { get; set; }
    public HksEndianness Endianness { get; set; }
    public byte SizeofInt { get; set; }
    public byte SizeofSizeT { get; set; }
    public byte SizeofInstruction { get; set; }
    public byte SizeofNumber { get; set; }
    public HksNumberType IntegralFlag { get; set; }
    public byte BuildFlags { get; set; }
    public byte ReferencedMode { get; set; }
    public List<HksTypeMeta> TypeMeta { get; set; }
}
