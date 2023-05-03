
namespace LuiTool.HavokScript.Assembly;

public enum HksStrippingLevel : byte
{
    NONE = 0x0,         // no DebugInfo
    PROFILING = 0x1,
    ALL = 0x2,          // 1 + hash
    DEBUG_ONLY = 0x3,
    CALLSTACK_RECONSTRUCTION = 0x4, // string line
}
