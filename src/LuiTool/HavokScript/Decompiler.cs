using LuiTool.Utils;
using LuiTool.Code;
using LuiTool.HavokScript.Assembly;
using LuiTool.HavokScript.Errors;

namespace LuiTool.HavokScript;

class Decompiler
{
    private HksFunction currAsm;
    private FunctionStatement currSrc;
    private List<Node> stack;

    public Decompiler()
    {  
    }

    public Chunk Decompile(HksFile assembly)
    {
        var func = DecompileFunction(assembly.Function);

        var list = new List<Statement>();
        list.Add(func);
        var chunk = new Chunk(0, list);

        var pass1 = new PassRefCounter();
        pass1.Visit(chunk);
        var pass2 = new PassWrapRegister();
        pass2.Visit(chunk);

        return chunk;
    }

    private FunctionStatement DecompileFunction(HksFunction func)
    {
        currAsm = func;
        currSrc = new FunctionStatement(func.Address, new Identifier(func.Address, ""), new List<string>(), new Block(func.Address, new List<Statement>()));
        stack = new List<Node>();

        foreach (var inst in func.Instructions)
        {
            DecompileInstruction(inst);
        }

        DecompileBlock(currSrc.body);

        // run passes

        // decompile sub functions

        // resolve closures

        return currSrc;
    }

    private void DecompileInstruction(HksInstruction inst)
    {
        switch (inst.Code)
        {
            case HksOpCode.JMP:
            {
                var stm = new Jump(inst.Address, inst.Args[0].Value);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.FORPREP:
            {
                var rinit = new Register(inst.Address, inst.Args[0].Value);
                var rlast = new Register(inst.Address, inst.Args[0].Value + 1);
                var rnext = new Register(inst.Address, inst.Args[0].Value + 2);
                var rval = new Register(inst.Address, inst.Args[0].Value + 3);
                var stm = new AsmForPrep(inst.Address, rinit, rlast, rnext, rval);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.FORLOOP:
            {
                var exp = new Register(inst.Address, inst.Args[0].Value);
                var stm = new AsmForLoop(inst.Address, exp, inst.Args[1].Value);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.TFORLOOP:
            {
                //var exp = new Register(inst.Address, inst.Args[0].Value);
                var stm = new AsmTForLoop(inst.Address);
                currSrc.body.statements.Add(stm);
                break;
            }
            // case HksOpCode.TESTSET:
            // {

            //     break;
            // }
            case HksOpCode.TEST:
            case HksOpCode.TEST_R1: // A C   if (R(A) ~= C) then PC++
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                if (inst.Args[1].Value == 1)
                {
                    var exp = new UnaryExpression(inst.Address, lhs, "not ");
                    var stm = new Test(inst.Address, exp);
                    currSrc.body.statements.Add(stm);
                }
                else
                {
                    var stm = new Test(inst.Address, lhs);
                    currSrc.body.statements.Add(stm);
                }
                break;
            }
            case HksOpCode.EQ: // A B C   if ((R(B) == RK(C)) ~= A) then PC++
            {
                var lhs = new Register(inst.Address, inst.Args[1].Value);
                var rhs = DecompileRK(inst.Address,inst.Args[2]);
                var opr = (inst.Args[0].Value == 1) ? "~=" : "==";
                var exp = new BinaryExpression(inst.Address, lhs, rhs, opr);
                var stm = new Test(inst.Address, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.EQ_BK: // A B C   if ((K(B) == R(C)) ~= A) then PC++
            {
                var lhs = DecompileConstant(inst.Address, inst.Args[1].Value);
                var rhs = new Register(inst.Address, inst.Args[2].Value);
                var opr = (inst.Args[0].Value == 1) ? "~=" : "==";
                var exp = new BinaryExpression(inst.Address, lhs, rhs, opr);
                var stm = new Test(inst.Address, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.LT: // A B C   if ((R(B) (</>=) RK(C)) ~= A) then PC++
            {
                var lhs = new Register(inst.Address, inst.Args[1].Value);
                var rhs = DecompileRK(inst.Address,inst.Args[2]);
                var opr = (inst.Args[0].Value == 1) ? ">=" : "<";
                var exp = new BinaryExpression(inst.Address, lhs, rhs, opr);
                var stm = new Test(inst.Address, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.LT_BK: // A B C   if ((K(B) (</>=) R(C)) ~= A) then PC++
            {
                var lhs = DecompileConstant(inst.Address, inst.Args[1].Value);
                var rhs = new Register(inst.Address, inst.Args[2].Value);
                var opr = (inst.Args[0].Value == 1) ? ">=" : "<";
                var exp = new BinaryExpression(inst.Address, lhs, rhs, opr);
                var stm = new Test(inst.Address, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.LE: // A B C   if ((R(B) (<=/>) RK(C)) ~= A) then PC++
            {
                var lhs = new Register(inst.Address, inst.Args[1].Value);
                var rhs = DecompileRK(inst.Address,inst.Args[2]);
                var opr = (inst.Args[0].Value == 1) ? ">" : "<=";
                var exp = new BinaryExpression(inst.Address, lhs, rhs, opr);
                var stm = new Test(inst.Address, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.LE_BK: // A B C   if ((K(B) (<=/>) R(C)) ~= A) then PC++
            {
                var lhs = DecompileConstant(inst.Address, inst.Args[1].Value);
                var rhs = new Register(inst.Address, inst.Args[2].Value);
                var opr = (inst.Args[0].Value == 1) ? ">" : "<=";
                var exp = new BinaryExpression(inst.Address, lhs, rhs, opr);
                var stm = new Test(inst.Address, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.LOADBOOL:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new BooleanLiteral(inst.Address, inst.Args[1].Value != 0);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);

                if (inst.Args[2].Value != 0)
                {
                    var jmp = new Jump(inst.Address, 1);
                    currSrc.body.statements.Add(jmp);
                }
                break;
            }
            case HksOpCode.LOADNIL:
            {
                var args = new List<Expression>();

                for (var i = inst.Args[0].Value; i <= inst.Args[1].Value; i++)
                {
                    args.Add(new Register(inst.Address, i));
                }

                var stm = new AssignmentStatement(inst.Address, args, new NilLiteral(inst.Address));
                break;
            }
            case HksOpCode.LOADK: // A Bx    R(A) := Kst(Bx)
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = DecompileConstant(inst.Address, inst.Args[1].Value);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.GETGLOBAL:
            case HksOpCode.GETGLOBAL_MEM:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = DecompileGlobal(inst.Address, inst.Args[1].Value);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SETGLOBAL: // A Bx    Gbl[Kst(Bx)] := R(A)
            {
                var lhs = DecompileGlobal(inst.Address, inst.Args[1].Value);
                var rhs = new Register(inst.Address, inst.Args[0].Value);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.ADD:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = new Register(inst.Address, inst.Args[1].Value);
                var right = DecompileRK(inst.Address,inst.Args[2]);
                var exp = new BinaryExpression(inst.Address, left, right, "+");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.ADD_BK:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = DecompileConstant(inst.Address, inst.Args[1].Value);
                var right = new Register(inst.Address, inst.Args[2].Value);
                var exp = new BinaryExpression(inst.Address, left, right, "+");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SUB:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = new Register(inst.Address, inst.Args[1].Value);
                var right = DecompileRK(inst.Address,inst.Args[2]);
                var exp = new BinaryExpression(inst.Address, left, right, "-");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SUB_BK:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = DecompileConstant(inst.Address, inst.Args[1].Value);
                var right = new Register(inst.Address, inst.Args[2].Value);
                var exp = new BinaryExpression(inst.Address, left, right, "-");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.MUL:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = new Register(inst.Address, inst.Args[1].Value);
                var right = DecompileRK(inst.Address,inst.Args[2]);
                var exp = new BinaryExpression(inst.Address, left, right, "*");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.MUL_BK:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = DecompileConstant(inst.Address, inst.Args[1].Value);
                var right = new Register(inst.Address, inst.Args[2].Value);
                var exp = new BinaryExpression(inst.Address, left, right, "*");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.DIV:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = new Register(inst.Address, inst.Args[1].Value);
                var right = DecompileRK(inst.Address,inst.Args[2]);
                var exp = new BinaryExpression(inst.Address, left, right, "/");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.DIV_BK:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = DecompileConstant(inst.Address, inst.Args[1].Value);
                var right = new Register(inst.Address, inst.Args[2].Value);
                var exp = new BinaryExpression(inst.Address, left, right, "/");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.MOD:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = new Register(inst.Address, inst.Args[1].Value);
                var right = DecompileRK(inst.Address,inst.Args[2]);
                var exp = new BinaryExpression(inst.Address, left, right, "%");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.MOD_BK:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = DecompileConstant(inst.Address, inst.Args[1].Value);
                var right = new Register(inst.Address, inst.Args[2].Value);
                var exp = new BinaryExpression(inst.Address, left, right, "%");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.POW:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = new Register(inst.Address, inst.Args[1].Value);
                var right = DecompileRK(inst.Address,inst.Args[2]);
                var exp = new BinaryExpression(inst.Address, left, right, "^");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.POW_BK:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var left = DecompileConstant(inst.Address, inst.Args[1].Value);
                var right = new Register(inst.Address, inst.Args[2].Value);
                var exp = new BinaryExpression(inst.Address, left, right, "^");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.UNM:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var right = new Register(inst.Address, inst.Args[1].Value);
                var exp = new UnaryExpression(inst.Address, right, "-");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.NOT:
            case HksOpCode.NOT_R1:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var right = new Register(inst.Address, inst.Args[1].Value);
                var exp = new UnaryExpression(inst.Address, right, "not ");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.LEN:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var right = new Register(inst.Address, inst.Args[1].Value);
                var exp = new UnaryExpression(inst.Address, right, "#");
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.VARARG: // A B     R(A), R(A+1), ..., R(A+B-1) = vararg
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new VarargsLiteral(inst.Address);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }

            case HksOpCode.GETFIELD:
            {

                break;
            }
            case HksOpCode.MOVE: // A B     R(A) := R(B)
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new Register(inst.Address, inst.Args[1].Value);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.RETURN: // A B    return R(A), ... ,R(A+B-2)
            {
                if (inst.Args[1].Value > 0)
                {
                    var rets = new List<Expression>();

                    for (var i = 0; i < inst.Args[1].Value - 1; i++)
                    {
                        rets.Add(new Register(inst.Address, inst.Args[0].Value + i));
                    }

                    var stm = new ReturnStatement(inst.Address, rets);
                    currSrc.body.statements.Add(stm);
                }
                break;
            }
            case HksOpCode.CALL:                   
            case HksOpCode.CALL_I:
            case HksOpCode.CALL_I_R1:
            {
                var args = new List<Expression>();

                for (var i = 1; i < inst.Args[1].Value; i++)
                {
                    args.Add(new Register(inst.Address, inst.Args[0].Value + i));
                }

                var name = new Register(inst.Address, inst.Args[0].Value);
                var call = new FunctionCall(inst.Address, name, args);

                var rets = new List<Expression>();

                for (var i = 0; i < inst.Args[2].Value; i++)
                {
                    rets.Add(new Register(inst.Address, inst.Args[0].Value + i));
                }

                var stm = new AssignmentStatement(inst.Address, rets, call);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.CLOSURE:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new Closure(inst.Address, inst.Args[1].Value);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.GETFIELD_R1:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);

                var key = DecompileGlobal(inst.Address, inst.Args[2].Value); // decompile NAME
                var obj = new Register(inst.Address, inst.Args[1].Value);
                var rhs = new TableAccess(inst.Address, obj, key);

                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.DATA:
            {
                // skip
                break;
            }
            default:
                break;
                throw new DecompileException("unhandled opcode " + inst.Code.ToString());
        }
    }

    private void DecompileBlock(Block blk)
    {
        DecompileStatements(blk.statements);
    }

     private void DecompileStatements(List<Statement> stmts)
    {
        DecompileLoops(stmts);
    }

    private void DecompileLoops(List<Statement> stmts)
    {
        for (var i = stmts.Count - 1; i >= 0; i--)
        {
            if (stmts[i] is AsmForLoop afl)
            {
                var begin = FindLocation(stmts, (afl.Address + 4) + (afl.offset * 4));

                if (begin < 4 || !(stmts[begin - 1] is AsmForPrep))
                    throw new DecompileException("bad for prep");

                begin -= 4;

                DecompileFor(stmts, begin, i);
                i = stmts.Count - 1;
                continue;
            }
            else if (stmts[i] is Jump jmp && jmp.offset < 0)
            {
                var begin = FindLocation(stmts, (jmp.Address + 4) + (jmp.offset * 4));

                if (i > 0 && stmts[i - 1] is AsmTForLoop)
                {
                    DecompileForEach(stmts, begin, i);
                    i = stmts.Count - 1;
                    continue;
                }
                else if (i > 0 && stmts[i - 1] is Test)
                {
                    DecompileRepeatUntil(stmts, begin, i);
                    i = stmts.Count - 1;
                    continue;
                }
                else
                {
                    DecompileWhile(stmts, begin, i);
                    i = stmts.Count - 1;
                    continue;
                }
            }
        }
    }

    private void DecompileFor(List<Statement> stmts, int begin, int end)
    {
        var addr = stmts[begin].Address;
        var init = stmts[begin];
        var cond = stmts[begin + 1];
        var step = stmts[begin + 2];
        var prep = stmts[begin + 3] as AsmForPrep;

        stmts.RemoveAt(end);
        stmts.RemoveAt(begin);
        stmts.RemoveAt(begin);
        stmts.RemoveAt(begin);
        stmts.RemoveAt(begin);
        end -= 5;

        var body = new List<Statement>();

        for (var j = 0; j <= end - begin; j++)
        {
            body.Add(stmts[begin]);
            stmts.RemoveAt(begin);
        }

        DecompileStatements(body);

        stmts.Insert(begin, new ForStatement(addr, prep?.rvar, init, cond, step, new Block(addr, body)));
    }

    private void DecompileForEach(List<Statement> stmts, int begin, int end)
    {
        var addr = stmts[begin].Address;

        var body = new List<Statement>();

        for (var j = 0; j <= end - begin; j++)
        {
            body.Add(stmts[begin]);
            stmts.RemoveAt(begin);
        }

        stmts.Insert(begin, new Block(addr, body));
    }

    private void DecompileRepeatUntil(List<Statement> stmts, int begin, int end)
    {
        var addr = stmts[begin].Address;

        var body = new List<Statement>();

        for (var j = 0; j <= end - begin; j++)
        {
            body.Add(stmts[begin]);
            stmts.RemoveAt(begin);
        }

        stmts.Insert(begin, new Block(addr, body));
    }

    private void DecompileWhile(List<Statement> stmts, int begin, int end)
    {
        var addr = stmts[begin].Address;

        var body = new List<Statement>();

        for (var j = 0; j <= end - begin; j++)
        {
            body.Add(stmts[begin]);
            stmts.RemoveAt(begin);
        }

        stmts.Insert(begin, new Block(addr, body));
    }

    private int FindLocation(List<Statement> stmts, int loc)
    {
        for (var i = 0; i < stmts.Count; i++)
        {
            if (stmts[i].Address == loc) return i;
        }

        throw new DecompileException("location not found " + loc.ToString());
    }

    private Expression DecompileRK(int addr, HksOpArg arg)
    {
        if (arg.Mode == HksOpArgMode.REG)
        {
            return new Register(addr, arg.Value);
        }
        else
        {
            return DecompileConstant(addr, arg.Value);
        }
    }
    private Expression DecompileConstant(int addr, int index)
    {
        var k = currAsm.Constants[index];

        switch (k.Type)
        {
            case HksType.TNIL:
                return new NilLiteral(addr);
            case HksType.TBOOLEAN:
                return new BooleanLiteral(addr, (bool)k.Value);
            case HksType.TNUMBER:
                return new NumberLiteral(addr, (double)k.Value);
            case HksType.TSTRING:
                return new StringLiteral(addr, (string)k.Value);
            default:
                throw new DecompileException("internal error: constant");
        }
    }

    private Expression DecompileGlobal(int addr, int index)
    {
        var k = currAsm.Constants[index];

        if (k.Type == HksType.TSTRING)
        {
            return new Identifier(addr, (string)k.Value);
        }

        throw new DecompileException("internal error: global only support string const");
    }
}
