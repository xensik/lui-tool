
namespace LuiTool.HavokScript.Assembly;

public class HksFunction
{
    public int Address { get; set; }
    public uint UpvalCount { get; set; }
    public uint ParamCount { get; set; }
    public byte VarArgFlags { get; set; }
    public uint RegCount { get; set; }
    public List<HksInstruction> Instructions { get; set; }
    public List<HksConstant> Constants { get; set; }
    public HksDebug? Debug { get; set; }
    public List<HksFunction> Closures { get; set; }
}
