
namespace LuiTool.HavokScript.Assembly;

public static class HksOp
{
    public static int GetCode(uint op)
    {
        return (int)((op & 0xFE000000) >> 25);
    }

    public static int GetArgA(uint op)
    {
        return (int)((op & 0x000000FF) >> 0);
    }

    public static int GetArgB(uint op)
    {
        return (int)((op & 0x01FE0000) >> 17);
    }

    public static int GetArgC(uint op)
    {
        return (int)((op & 0x0001FF00) >> 8);
    }

    public static int GetArgBx(uint op)
    {
        return (int)((op & 0x01FFFF00) >> 8);
    }

    public static int GetArgsBx(uint op)
    {
        return (int)(GetArgBx(op) - 0xFFFF);
    }

    public static bool GetsZero(uint op)
    {
        return (GetArgC(op) >= 0x100);
    }
}
