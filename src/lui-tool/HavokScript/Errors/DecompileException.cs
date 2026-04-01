
namespace LuiTool.HavokScript.Errors;

public class DecompileException : Exception
{
    public DecompileException() { }
    public DecompileException(string message) : base(message) { }
    public DecompileException(string message, Exception inner) : base(message, inner) { }

    public static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new DecompileException(message);
        }
    }
}
