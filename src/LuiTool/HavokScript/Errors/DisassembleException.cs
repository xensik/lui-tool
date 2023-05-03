
namespace LuiTool.HavokScript.Errors;

public class DisassembleException : Exception
{
    public DisassembleException() { }
    public DisassembleException(string message) : base(message) { }
    public DisassembleException(string message, Exception inner) : base(message, inner) { }

    public static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new DisassembleException(message);
        }
    }
}
