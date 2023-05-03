
using LuiTool.Utils;
using LuiTool.HavokScript.Errors;
using LuiTool.HavokScript.Assembly;

namespace LuiTool.HavokScript;

class Disassembler
{
    private readonly Reader reader;
    private HksHeader? header;

    public Disassembler(byte[] data)
    {
        reader = new Reader(data);
    }

    public HksFile Disassemble()
    {
        var file = new HksFile
        {
            Header = DisassembleHeader(),
            Function = DisassembleFunction(),
            Proto = DisassemblePrototype()
        };

        return file;
    }

    private HksHeader DisassembleHeader()
    {
        header = new HksHeader
        {
            Signature = reader.ReadUInt32(),
            LuaVersion = reader.ReadUInt8(),
            FormatVersion = DisassembleFormat(),
            Endianness = (HksEndianness)reader.ReadUInt8(),
            SizeofInt = reader.ReadUInt8(),
            SizeofSizeT = reader.ReadUInt8(),
            SizeofInstruction = reader.ReadUInt8(),
            SizeofNumber = reader.ReadUInt8(),
            IntegralFlag = (HksNumberType)reader.ReadUInt8(),
            BuildFlags = reader.ReadUInt8(),
            ReferencedMode = reader.ReadUInt8(),
            TypeMeta = DisassembleTypeMeta()
        };

        return header;
    }

    private HksFormat DisassembleFormat()
    {
        return reader.ReadUInt8() switch
        {
            13 => HksFormat.V13,
            14 => HksFormat.V14,
            _ => throw new DisassembleException("unsupported bytecode format"),
        };
    }

    private List<HksTypeMeta> DisassembleTypeMeta()
    {
        var types = new List<HksTypeMeta>{};
        var count = reader.ReadUInt32();

        for (var i = 0u; i < count; i++)
        {
            types.Add(new HksTypeMeta
            {
                Id = reader.ReadUInt32(),
                Name = reader.ReadString(reader.ReadInt32())
            });
        }

        return types;
    }

    private HksFunction DisassembleFunction()
    {
        var addr = reader.GetPosition();

        return new HksFunction
        {
            UpvalCount = reader.ReadUInt32(),
            ParamCount = reader.ReadUInt32(),
            VarArgFlags = reader.ReadUInt8(),
            RegCount = reader.ReadUInt32(),
            Instructions = DisassembleInstructions(),
            Constants = DisassembleConstants(),
            Debug = DisassembleDebug(),
            Closures = DisassembleClosures(),
            Address = addr
        };
    }

    private List<HksInstruction> DisassembleInstructions()
    {
        var insts = new List<HksInstruction>{};
        var count = DisassembleSize();

        reader.Pad(4);

        for (var i = 0ul; i < count; i++)
        {
            insts.Add(DisassembleInstruction());
        }

        return insts;
    }

    private HksInstruction DisassembleInstruction()
    {
        var addr = reader.GetPosition();
        var raw = reader.ReadUInt32();

        var args = new List<HksOpArg>();
        var code = HksOp.GetCode(raw);
        var meta = HksOpMetaTable.OpMeta[code];

        if (meta.OpArgModeA != HksOpArgModeA.UNUSED)
        {
            var modeA = meta.OpArgModeA switch
            {
                HksOpArgModeA.NUMBER => HksOpArgMode.NUMBER,
                HksOpArgModeA.REG => HksOpArgMode.REG,
                _ => throw new DisassembleException("internal error")
            };

            var dataA = HksOp.GetArgA(raw);
            args.Add(new HksOpArg(modeA, dataA));
        }

        if (meta.OpMode == HksOpMode.ABC)
        {
            if (meta.OpArgModeB != HksOpArgModeB.UNUSED)
            {
                var arg = meta.OpArgModeB switch
                {
                    HksOpArgModeB.NUMBER => new HksOpArg(HksOpArgMode.NUMBER, HksOp.GetArgB(raw)),
                    HksOpArgModeB.REG    => new HksOpArg(HksOpArgMode.REG,    HksOp.GetArgB(raw)),
                    HksOpArgModeB.CONST  => new HksOpArg(HksOpArgMode.CONST,  HksOp.GetArgB(raw)),
                     _ => throw new DisassembleException("internal error")
                };

                args.Add(arg);
            }

            if (meta.OpArgModeC != HksOpArgModeC.UNUSED)
            {
                var arg = meta.OpArgModeC switch
                {
                    HksOpArgModeC.NUMBER => new HksOpArg(HksOpArgMode.NUMBER, HksOp.GetArgC(raw)),
                    HksOpArgModeC.REG    => new HksOpArg(HksOpArgMode.REG,    HksOp.GetArgC(raw)),
                    HksOpArgModeC.CONST  => new HksOpArg(HksOpArgMode.CONST,  HksOp.GetArgC(raw)),
                    HksOpArgModeC.R_OR_K => HksOp.GetsZero(raw) ? new HksOpArg(HksOpArgMode.CONST,  HksOp.GetArgC(raw) & 0xFF) : new HksOpArg(HksOpArgMode.REG,  HksOp.GetArgC(raw)),
                     _ => throw new DisassembleException("internal error")
                };

                args.Add(arg);
            }
        }
        else if (meta.OpMode == HksOpMode.ABx)
        {
            var arg = meta.OpArgModeB switch
            {
                HksOpArgModeB.NUMBER => new HksOpArg(HksOpArgMode.NUMBER, HksOp.GetArgBx(raw)),
                HksOpArgModeB.OFFSET => new HksOpArg(HksOpArgMode.NUMBER, HksOp.GetArgBx(raw)),
                HksOpArgModeB.CONST  => new HksOpArg(HksOpArgMode.CONST,  HksOp.GetArgBx(raw)),
                _ => throw new DisassembleException("internal error")
            };

            args.Add(arg);
        }
        else
        {
            args.Add(new HksOpArg(HksOpArgMode.NUMBER, HksOp.GetArgsBx(raw)));
        }

        return new HksInstruction(addr, (HksOpCode)code, args);
    }

    private List<HksConstant> DisassembleConstants()
    {
        var consts = new List<HksConstant>();
        var count = reader.ReadUInt32();

        for (var i = 0; i < count; i++)
        {
            var type = (HksType)reader.ReadUInt8();
            object? value = null;

            switch (type)
            {
                case HksType.TNIL:
                    value = null;
                    break;
                case HksType.TBOOLEAN:
                    value = reader.ReadUInt8();
                    break;
                case HksType.TLIGHTUSERDATA:
                    value = DisassembleSize();
                    break;
                case HksType.TNUMBER:
                    if (header?.IntegralFlag == HksNumberType.FLOAT)
                    {
                        value = (header!.SizeofNumber == 4) ? reader.ReadFloat() : reader.ReadDouble();
                    }
                    else
                    {
                        value = (header!.SizeofNumber == 4) ? reader.ReadInt32() : reader.ReadInt64();
                    }
                    break;
                case HksType.TSTRING:
                    value = DisassembleString();
                    break;
                case HksType.TUI64:
                    value = reader.ReadUInt64();
                    break;
                case HksType.TTABLE:
                case HksType.TFUNCTION:
                case HksType.TUSERDATA:
                case HksType.TTHREAD:
                case HksType.TIFUNCTION:
                case HksType.TCFUNCTION:
                case HksType.TSTRUCT:
                default:
                    throw new DisassembleException("type not implemented: " + type.ToString());
            }

            consts.Add(new HksConstant{ Type = type, Value = value });
        }

        return consts;
    }

    private HksDebug? DisassembleDebug()
    {
        var hasDebugInfo = reader.ReadUInt32();

        if (hasDebugInfo == 1)
        {
            // T6: 1 + hash
            var hash = reader.ReadUInt32();
        }

        // T6, T7
        // striplevel 1
        // hks::BytecodeWriter::dumpInt(this, 1);
        // hks::BytecodeWriter::dumpInt(this, 0);
        // hks::BytecodeWriter::dumpInt(this, 0);
        // hks::BytecodeWriter::dumpInt(this, 0);
        // hks::BytecodeWriter::dumpInt(this, fun->m_debug->line_defined);
        // hks::BytecodeWriter::dumpInt(this, fun->m_debug->last_line_defined);
        // hks::BytecodeWriter::dumpString(this, source);
        // hks::BytecodeWriter::dumpString(this, fun->m_debug->name);

        // striplevel 2
        // var hash = reader.ReadUInt32();

        return new HksDebug();
    }

    private List<HksFunction> DisassembleClosures()
    {
        var closures = new List<HksFunction>();
        var count = reader.ReadUInt32();

        for (var i = 0u; i < count; i++)
        {
            closures.Add(DisassembleFunction());
        }

        return closures;
    }

    private HksPrototype DisassemblePrototype()
    {
        // t6 no prototypes

        // var mismatch = reader.ReadUInt32(); // always 1

        // TODO protos:

        // var end = DisassembleSize(); // always 0
        return new HksPrototype();
    }

    private ulong DisassembleSize()
    {
        return (header!.SizeofSizeT == 4) ? reader.ReadUInt32() : reader.ReadUInt64();
    }

    private string DisassembleString()
    {
        var size = DisassembleSize();
        return (size == 0) ? "" : reader.ReadString((int)size).Remove((int)size - 1);
    }
}
