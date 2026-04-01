
namespace LuiTool.HavokScript.Assembly;

public class HksOpMeta
{
    public HksOpCode OpCode { get; }
    public HksOpMode OpMode { get; }
    public HksOpArgModeA OpArgModeA { get; }
    public HksOpArgModeB OpArgModeB { get; }
    public HksOpArgModeC OpArgModeC { get; }

    public HksOpMeta(HksOpCode OpCode, HksOpMode OpMode, HksOpArgModeA OpArgModeA, HksOpArgModeB OpArgModeB, HksOpArgModeC OpArgModeC)
    {
        this.OpCode = OpCode;
        this.OpMode = OpMode;
        this.OpArgModeA = OpArgModeA;
        this.OpArgModeB = OpArgModeB;
        this.OpArgModeC = OpArgModeC;
    }
}
