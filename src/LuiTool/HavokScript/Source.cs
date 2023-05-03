using System.Text;
using LuiTool.HavokScript.Assembly;

namespace LuiTool.HavokScript;

class Source
{
    private StringBuilder buf;
    
    public Source()
    {
        buf = new();
    }

    public string Dump(HksFile file)
    {
        DumpHeader(file.Header);
        DumpFunction(file.Function);
        DumpPrototype(file.Proto);
        return buf.ToString();
    }

    public void DumpHeader(HksHeader header)
    {
        // Signature
        // LuaVersion
        // FormatVersion
        buf.AppendFormat(".format {0}\n", header.FormatVersion);
        buf.AppendFormat(".endianness {0}\n", header.Endianness.ToString().ToLower());
        buf.AppendFormat(".int_size {0}\n", header.SizeofInt);
        buf.AppendFormat(".size_t_size {0}\n", header.SizeofSizeT);
        buf.AppendFormat(".instruction_size {0}\n", header.SizeofInstruction);
        buf.AppendFormat(".number_size {0}\n", header.SizeofNumber);
        buf.AppendFormat(".number_type {0}\n", header.IntegralFlag.ToString().ToLower());
        buf.AppendFormat(".build_flags {0}\n", header.BuildFlags);
        buf.AppendFormat(".referenced_mode {0}\n\n", header.ReferencedMode);
    }

    public void DumpFunction(HksFunction func)
    {
        buf.AppendFormat(".function {0}\n", string.Format("_id_{0:X8}", func.Address));
        buf.AppendFormat(".upval_count {0}\n", func.UpvalCount);
        buf.AppendFormat(".param_count {0}\n", func.ParamCount);
        buf.AppendFormat(".varg_flags {0}\n", func.VarArgFlags);
        buf.AppendFormat(".reg_count {0}\n", func.RegCount);
        buf.AppendFormat(".instr_count {0}\n", func.Instructions.Count);
        buf.AppendFormat(".const_count {0}\n", func.Constants.Count);
        buf.AppendFormat(".func_count {0}\n", func.Closures.Count);

        for (var i = 0; i < func.Constants.Count; i++)
        {
            HksType type = func.Constants[i].Type;
            object? value = func.Constants[i].Value;
            string valueStr = type switch
            {
                HksType.TNIL => "nil",
                HksType.TSTRING => "\"" + (string)value! + "\"",
                HksType.TBOOLEAN => (byte)value! == 0 ? "false" : "true",
                HksType.TNUMBER => value!.ToString()!,
                _ => type.ToString() + "(" + value + ")"
            };
    
            buf.AppendFormat(".constant {0} ; {1}\n", valueStr, i);
        }

        for (var i = 0; i < func.Instructions.Count; i++)
        {
            var inst = func.Instructions[i];
            buf.AppendFormat("{0} ", inst.Code.ToString());

            for (int j = 0; j < inst.Args.Count; j++)
            {
                var arg = inst.Args[j];

                switch (arg.Mode)
                {
                    case HksOpArgMode.CONST:
                        buf.AppendFormat("K({0})", arg.Value);
                        break;
                    case HksOpArgMode.REG:
                        buf.AppendFormat("R({0})", arg.Value);
                        break;
                    default:
                        buf.Append(arg.Value);
                        break;
                }

                if (j < inst.Args.Count - 1)
                    buf.Append(", ");
            }

            buf.Append('\n');
        }

        buf.Append("\n");

        for (var i = 0; i < func.Closures.Count; i++)
        {
            DumpFunction(func.Closures[i]);
        }
    }

    public void DumpPrototype(HksPrototype proto)
    {
        
    }
}
