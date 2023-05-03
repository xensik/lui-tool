using LuiTool.HavokScript;
using LuiTool.Code;

Console.WriteLine("LUI Tool");

if (args.Length != 1)
    return;

var data = File.ReadAllBytes(args[0]);
var disasm = new Disassembler(data);
var outasm = disasm.Disassemble();
var source = new Source();

File.WriteAllText("output.dis.lua", source.Dump(outasm));

var decomp = new Decompiler();
var outsrc = decomp.Decompile(outasm);
var printer = new CodePrinterVisitor();
printer.Visit(outsrc);

File.WriteAllText("output.dec.lua", printer.GetCode());
