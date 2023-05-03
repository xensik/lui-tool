
namespace LuiTool.HavokScript.Assembly;

public enum HksVarArgFlags : byte
{
    HASARG   = 1 << 0,
    ISVARARG = 1 << 1,
    NEEDSARG = 1 << 2
}
