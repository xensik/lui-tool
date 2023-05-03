
namespace LuiTool.HavokScript.Assembly;

public class HksInstruction
{
    public int Address { get; set; }
    public HksOpCode Code { get; set; }
    public List<HksOpArg> Args { get; set; }

    public HksInstruction(int Address, HksOpCode Code, List<HksOpArg> Args)
    {
        this.Address = Address;
        this.Code = Code;
        this.Args = Args;
    }
}
