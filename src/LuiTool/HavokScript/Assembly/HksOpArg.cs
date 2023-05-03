
namespace LuiTool.HavokScript.Assembly;

public class HksOpArg
{
    public HksOpArgMode Mode { get; set; }
    public int Value { get; set; }

    public HksOpArg(HksOpArgMode Mode, int Value)
    {
        this.Mode = Mode;
        this.Value = Value;
    }
}
