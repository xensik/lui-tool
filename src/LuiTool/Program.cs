using LuiTool.Cli;
using LuiTool.HavokScript;
using LuiTool.HavokScript.Assembly;
using LuiTool.Code;

var app = new CliApp("LUI Tool");

app.Register(new CliCommand(
    "disasm",
    "Disassemble HavokScript bytecode into assembly text.",
    [
        new CliArgument("input", "Path to the input HavokScript bytecode file."),
        new CliArgument("output", "Destination path for the disassembly output. Defaults to output.dis.lua.", required: false)
    ],
    invocation =>
    {
        var inputPath = invocation.GetRequired("input");
        var outputPath = invocation.GetOptional("output") ?? "output.dis.lua";

        WriteDisassembly(inputPath, outputPath);
        Console.WriteLine($"wrote disassembly to {outputPath}");
        return 0;
    }));

app.Register(new CliCommand(
    "decomp",
    "Decompile HavokScript bytecode into Lua source.",
    [
        new CliArgument("input", "Path to the input HavokScript bytecode file."),
        new CliArgument("output", "Destination path for the decompiled output. Defaults to output.dec.lua.", required: false)
    ],
    invocation =>
    {
        var inputPath = invocation.GetRequired("input");
        var outputPath = invocation.GetOptional("output") ?? "output.dec.lua";

        WriteDecompilation(inputPath, outputPath);
        Console.WriteLine($"wrote decompilation to {outputPath}");
        return 0;
    }));

return app.Run(args);

static void WriteDisassembly(string inputPath, string outputPath)
{
    try
    {
        var assembly = LoadAssembly(inputPath);
        var source = new Source();
        File.WriteAllText(outputPath, source.Dump(assembly));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during disassembly: {ex.Message}");
    }

}

static void WriteDecompilation(string inputPath, string outputPath)
{
    var assembly = LoadAssembly(inputPath);
    var decompiler = new Decompiler();
    var source = decompiler.Decompile(assembly);
    var printer = new CodePrinterVisitor();
    printer.Visit(source);
    File.WriteAllText(outputPath, printer.GetCode());
}

static HksFile LoadAssembly(string inputPath)
{
    var data = File.ReadAllBytes(inputPath);
    var disassembler = new Disassembler(data);
    return disassembler.Disassemble();
}
