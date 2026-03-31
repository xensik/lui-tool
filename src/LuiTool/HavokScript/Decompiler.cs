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
    private List<FunctionStatement> decompiled;
    private Dictionary<int, List<int>> closureUpvalMaps;
    private int currentClosureIdx;
    private int remainingUpvals;

    public Decompiler()
    {
        decompiled = new List<FunctionStatement>();
    }

    public Chunk Decompile(HksFile assembly)
    {
        var func = DecompileFunction(assembly.Function);

        // unwrap main function body directly into chunk (top-level code)
        var chunk = new Chunk(0, func.body.statements);

        // first pass: inline single-use registers (resolves intermediate computations)
        new PassRefCounter().Visit(chunk);
        new PassWrapRegister(preserveEmptyTables: true).Visit(chunk);

        // merge NEWTABLE + SETFIELD sequences into populated table constructors
        BuildTableConstructorsTree(chunk);

        // second pass: inline the populated table constructors
        new PassRefCounter().Visit(chunk);
        new PassWrapRegister().Visit(chunk);

        return chunk;
    }

    private FunctionStatement DecompileFunction(HksFunction func)
    {
        var prevAsm = currAsm;
        var prevSrc = currSrc;
        var prevStack = stack;

        currAsm = func;

        var paramNames = new List<string>();
        for (var i = 0; i < func.ParamCount; i++)
        {
            paramNames.Add($"arg{i}");
        }

        currSrc = new FunctionStatement(func.Address, new Identifier(func.Address, ""), paramNames, new Block(func.Address, new List<Statement>()));
        stack = new List<Node>();
        closureUpvalMaps = new Dictionary<int, List<int>>();
        currentClosureIdx = -1;
        remainingUpvals = 0;

        foreach (var inst in func.Instructions)
        {
            DecompileInstruction(inst);
        }

        DecompileBlock(currSrc.body);

        // save upval maps before recursing into sub-functions
        var savedUpvalMaps = closureUpvalMaps;

        // decompile sub functions
        var subfuncs = new List<FunctionStatement>();
        foreach (var closure in func.Closures)
        {
            subfuncs.Add(DecompileFunction(closure));
        }

        // restore upval maps for closure resolution
        closureUpvalMaps = savedUpvalMaps;

        // resolve closures
        ResolveClosures(currSrc.body, subfuncs);

        var result = currSrc;

        currAsm = prevAsm;
        currSrc = prevSrc;
        stack = prevStack;

        return result;
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
            case HksOpCode.GETFIELD_MM:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var key = DecompileGlobal(inst.Address, inst.Args[2].Value);
                var obj = new Register(inst.Address, inst.Args[1].Value);
                var rhs = new TableAccess(inst.Address, obj, key, isDotAccess: true);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SETFIELD: // A B C   R(A)[K(B)] := RK(C)
            case HksOpCode.SETFIELD_R1:
            {
                var obj = new Register(inst.Address, inst.Args[0].Value);
                var key = DecompileGlobal(inst.Address, inst.Args[1].Value);
                var lhs = new TableAccess(inst.Address, obj, key, isDotAccess: true);
                var rhs = DecompileRK(inst.Address, inst.Args[2]);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.NEWTABLE: // A B C   R(A) := {} (size B, C)
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new TableConstructor(inst.Address, new List<Expression>(), new List<Expression>());
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SELF: // A B C   R(A+1) := R(B); R(A) := R(B)[RK(C)]
            {
                var obj = new Register(inst.Address, inst.Args[1].Value);
                Expression key;
                if (inst.Args[2].Mode == HksOpArgMode.CONST)
                    key = DecompileGlobal(inst.Address, inst.Args[2].Value);
                else
                    key = DecompileRK(inst.Address, inst.Args[2]);
                var access = new TableAccess(inst.Address, obj, key, isDotAccess: inst.Args[2].Mode == HksOpArgMode.CONST);
                var lhs1 = new Register(inst.Address, inst.Args[0].Value + 1);
                var stm1 = new AssignmentStatement(inst.Address, lhs1, new Register(inst.Address, inst.Args[1].Value));
                currSrc.body.statements.Add(stm1);
                var lhs0 = new Register(inst.Address, inst.Args[0].Value);
                var stm0 = new AssignmentStatement(inst.Address, lhs0, access);
                currSrc.body.statements.Add(stm0);
                break;
            }
            case HksOpCode.GETTABLE_S: // A B C   R(A) := R(B)[RK(C)]
            case HksOpCode.GETTABLE_N:
            case HksOpCode.GETTABLE:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var obj = new Register(inst.Address, inst.Args[1].Value);
                var key = DecompileRK(inst.Address, inst.Args[2]);
                var rhs = new TableAccess(inst.Address, obj, key);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SETTABLE_S: // A B C   R(A)[R(B)] := RK(C)
            case HksOpCode.SETTABLE_N:
            case HksOpCode.SETTABLE:
            {
                var obj = new Register(inst.Address, inst.Args[0].Value);
                var key = new Register(inst.Address, inst.Args[1].Value);
                var lhs = new TableAccess(inst.Address, obj, key);
                var rhs = DecompileRK(inst.Address, inst.Args[2]);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SETTABLE_S_BK: // A B C   R(A)[K(B)] := RK(C)
            case HksOpCode.SETTABLE_N_BK:
            case HksOpCode.SETTABLE_BK:
            {
                var obj = new Register(inst.Address, inst.Args[0].Value);
                var key = DecompileConstant(inst.Address, inst.Args[1].Value);
                var lhs = new TableAccess(inst.Address, obj, key);
                var rhs = DecompileRK(inst.Address, inst.Args[2]);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SETLIST: // A B C   R(A)[(C-1)*FPF+i] := R(A+i), 1 <= i <= B
            {
                // handled during table constructor building pass
                break;
            }
            case HksOpCode.CONCAT: // A B C   R(A) := R(B) .. ... .. R(C)
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                Expression exp = new Register(inst.Address, inst.Args[1].Value);
                for (var r = inst.Args[1].Value + 1; r <= inst.Args[2].Value; r++)
                {
                    exp = new BinaryExpression(inst.Address, exp, new Register(inst.Address, r), "..");
                }
                var stm = new AssignmentStatement(inst.Address, lhs, exp);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.GETUPVAL: // A B   R(A) := UpValue[B]
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new Identifier(inst.Address, $"upval{inst.Args[1].Value}");
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.SETUPVAL: // A B   UpValue[B] := R(A)
            case HksOpCode.SETUPVAL_R1:
            {
                var lhs = new Identifier(inst.Address, $"upval{inst.Args[1].Value}");
                var rhs = new Register(inst.Address, inst.Args[0].Value);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.CLOSE:
            {
                // no-op in decompiled output
                break;
            }
            case HksOpCode.TAILCALL:
            case HksOpCode.TAILCALL_I:
            case HksOpCode.TAILCALL_I_R1:
            case HksOpCode.TAILCALL_C:
            case HksOpCode.TAILCALL_M:
            {
                var args = new List<Expression>();
                for (var i = 1; i < inst.Args[1].Value; i++)
                {
                    args.Add(new Register(inst.Address, inst.Args[0].Value + i));
                }
                var name = new Register(inst.Address, inst.Args[0].Value);
                var call = new FunctionCall(inst.Address, name, args);
                var rets = new List<Expression> { call };
                var stm = new ReturnStatement(inst.Address, rets);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.TESTSET: // A B C   if (R(B) <=> C) then R(A) := R(B) else PC++
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new Register(inst.Address, inst.Args[1].Value);
                if (inst.Args[2].Value == 1)
                {
                    var exp = new UnaryExpression(inst.Address, rhs, "not ");
                    var stm = new Test(inst.Address, exp);
                    currSrc.body.statements.Add(stm);
                }
                else
                {
                    var stm = new Test(inst.Address, rhs);
                    currSrc.body.statements.Add(stm);
                }
                break;
            }
            case HksOpCode.NEWSTRUCT:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new TableConstructor(inst.Address, new List<Expression>(), new List<Expression>());
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
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
                if (inst.Args[1].Value == 1)
                {
                    // RETURN R(A), 1 = implicit return (compiler-added) — skip
                }
                else if (inst.Args[1].Value > 1)
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
            case HksOpCode.CALL_C:
            case HksOpCode.CALL_M:
            {
                var args = new List<Expression>();

                if (inst.Args[1].Value > 1)
                {
                    for (var i = 1; i < inst.Args[1].Value; i++)
                    {
                        args.Add(new Register(inst.Address, inst.Args[0].Value + i));
                    }
                }

                var name = new Register(inst.Address, inst.Args[0].Value);
                var call = new FunctionCall(inst.Address, name, args);

                if (inst.Args[2].Value == 0)
                {
                    // C=0: variable returns
                    var rets = new List<Expression>();
                    rets.Add(new Register(inst.Address, inst.Args[0].Value));
                    var stm = new AssignmentStatement(inst.Address, rets, call);
                    currSrc.body.statements.Add(stm);
                }
                else if (inst.Args[2].Value == 1)
                {
                    // C=1: no return values captured - bare call statement
                    var stm = new ExpressionStatement(inst.Address, call);
                    currSrc.body.statements.Add(stm);
                }
                else
                {
                    var rets = new List<Expression>();
                    for (var i = 0; i < inst.Args[2].Value - 1; i++)
                    {
                        rets.Add(new Register(inst.Address, inst.Args[0].Value + i));
                    }
                    var stm = new AssignmentStatement(inst.Address, rets, call);
                    currSrc.body.statements.Add(stm);
                }
                break;
            }
            case HksOpCode.CLOSURE:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);
                var rhs = new Closure(inst.Address, inst.Args[1].Value);
                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);

                // start collecting DATA opcodes for upvalue bindings
                currentClosureIdx = inst.Args[1].Value;
                if (currentClosureIdx < currAsm.Closures.Count)
                {
                    remainingUpvals = (int)currAsm.Closures[currentClosureIdx].UpvalCount;
                    if (remainingUpvals > 0)
                        closureUpvalMaps[currentClosureIdx] = new List<int>();
                }
                break;
            }
            case HksOpCode.GETFIELD_R1:
            {
                var lhs = new Register(inst.Address, inst.Args[0].Value);

                var key = DecompileGlobal(inst.Address, inst.Args[2].Value); // decompile NAME
                var obj = new Register(inst.Address, inst.Args[1].Value);
                var rhs = new TableAccess(inst.Address, obj, key, isDotAccess: true);

                var stm = new AssignmentStatement(inst.Address, lhs, rhs);
                currSrc.body.statements.Add(stm);
                break;
            }
            case HksOpCode.DATA:
            {
                if (remainingUpvals > 0 && closureUpvalMaps.ContainsKey(currentClosureIdx))
                {
                    closureUpvalMaps[currentClosureIdx].Add(inst.Args[0].Value);
                    remainingUpvals--;
                }
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
        DecompileConditionals(stmts);
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

    private void DecompileConditionals(List<Statement> stmts)
    {
        for (var i = 0; i < stmts.Count; i++)
        {
            if (stmts[i] is Test test && i + 1 < stmts.Count && stmts[i + 1] is Jump jmp && jmp.offset > 0)
            {
                DecompileIf(stmts, i);
                i--;
                continue;
            }
        }
    }

    private void DecompileIf(List<Statement> stmts, int testIdx)
    {
        var test = (Test)stmts[testIdx];
        var jmp = (Jump)stmts[testIdx + 1];
        var addr = test.Address;

        // the condition is inverted (test skips over the if-body when condition is false)
        var condition = test.expression;

        // find where the jump lands
        var targetAddr = (jmp.Address + 4) + (jmp.offset * 4);
        var endIdx = FindLocationOrEnd(stmts, targetAddr);

        // remove test and jump
        stmts.RemoveAt(testIdx);
        stmts.RemoveAt(testIdx);
        endIdx -= 2;

        // check if the last statement before endIdx is a forward jump (else branch)
        Block elseBlock = null;

        if (endIdx > testIdx && endIdx - 1 < stmts.Count && stmts[endIdx - 1] is Jump elseJmp && elseJmp.offset > 0)
        {
            var elseEndAddr = (elseJmp.Address + 4) + (elseJmp.offset * 4);

            // remove the else jump from the if-body end
            endIdx--;
            stmts.RemoveAt(endIdx);

            // extract if body
            var ifBodyCount = endIdx - testIdx;
            var ifBody = new List<Statement>();
            for (var j = 0; j < ifBodyCount; j++)
            {
                ifBody.Add(stmts[testIdx]);
                stmts.RemoveAt(testIdx);
            }
            DecompileStatements(ifBody);

            // now find where the else branch ends (after if body was removed)
            var elseEndIdx = FindLocationOrEnd(stmts, elseEndAddr);

            // extract else body
            var elseBody = new List<Statement>();
            var elseCount = elseEndIdx - testIdx;
            for (var j = 0; j < elseCount; j++)
            {
                elseBody.Add(stmts[testIdx]);
                stmts.RemoveAt(testIdx);
            }
            DecompileStatements(elseBody);

            var ifBlock = new Block(addr, ifBody);
            elseBlock = new Block(addr, elseBody);

            stmts.Insert(testIdx, new IfStatement(addr, condition, ifBlock, new List<ElseIfBlock>(), elseBlock));
        }
        else
        {
            // no else — just extract the if body
            var ifBody = new List<Statement>();
            for (var j = 0; j < endIdx - testIdx; j++)
            {
                ifBody.Add(stmts[testIdx]);
                stmts.RemoveAt(testIdx);
            }
            DecompileStatements(ifBody);

            var ifBlock = new Block(addr, ifBody);
            stmts.Insert(testIdx, new IfStatement(addr, condition, ifBlock, new List<ElseIfBlock>(), null));
        }
    }

    private int FindLocation(List<Statement> stmts, int loc)
    {
        for (var i = 0; i < stmts.Count; i++)
        {
            if (stmts[i].Address == loc) return i;
        }

        throw new DecompileException("location not found " + loc.ToString());
    }

    private int FindLocationOrEnd(List<Statement> stmts, int loc)
    {
        for (var i = 0; i < stmts.Count; i++)
        {
            if (stmts[i].Address == loc) return i;
        }

        return stmts.Count;
    }

    private void ResolveClosures(Block block, List<FunctionStatement> subfuncs)
    {
        foreach (var stmt in block.statements)
        {
            if (stmt is AssignmentStatement asn)
            {
                for (var i = 0; i < asn.values.Count; i++)
                {
                    if (asn.values[i] is Closure closure && closure.index < subfuncs.Count)
                    {
                        var func = subfuncs[closure.index];
                        List<int> upvalMap = null;
                        if (closureUpvalMaps.ContainsKey(closure.index))
                            upvalMap = closureUpvalMaps[closure.index];
                        asn.values[i] = new FunctionExpression(func.Address, func.parameters, func.body, upvalMap);
                    }
                }
            }
            else if (stmt is Block blk)
            {
                ResolveClosures(blk, subfuncs);
            }
            else if (stmt is ForStatement fs)
            {
                ResolveClosures(fs.body, subfuncs);
            }
            else if (stmt is WhileStatement ws)
            {
                ResolveClosures(ws.body, subfuncs);
            }
            else if (stmt is RepeatUntilStatement rus)
            {
                ResolveClosures(rus.body, subfuncs);
            }
            else if (stmt is IfStatement ifs)
            {
                ResolveClosures(ifs.ifBlock, subfuncs);
                foreach (var elif in ifs.elseifBlocks)
                    ResolveClosures(elif.block, subfuncs);
                if (ifs.elseBlock != null)
                    ResolveClosures(ifs.elseBlock, subfuncs);
            }
            else if (stmt is DoStatement ds)
            {
                ResolveClosures(ds.body, subfuncs);
            }
        }
    }

    private void BuildTableConstructorsTree(Chunk chunk)
    {
        BuildTableConstructors(chunk.statements);
    }

    private void BuildTableConstructors(List<Statement> stmts)
    {
        for (var i = 0; i < stmts.Count; i++)
        {
            // recurse into child blocks
            if (stmts[i] is IfStatement ifs)
            {
                BuildTableConstructors(ifs.ifBlock.statements);
                foreach (var elif in ifs.elseifBlocks)
                    BuildTableConstructors(elif.block.statements);
                if (ifs.elseBlock != null)
                    BuildTableConstructors(ifs.elseBlock.statements);
            }
            else if (stmts[i] is WhileStatement ws)
                BuildTableConstructors(ws.body.statements);
            else if (stmts[i] is RepeatUntilStatement rus)
                BuildTableConstructors(rus.body.statements);
            else if (stmts[i] is ForStatement fs)
                BuildTableConstructors(fs.body.statements);
            else if (stmts[i] is ForInStatement fis)
                BuildTableConstructors(fis.body.statements);
            else if (stmts[i] is DoStatement ds)
                BuildTableConstructors(ds.body.statements);
            else if (stmts[i] is Block blk)
                BuildTableConstructors(blk.statements);

            // recurse into function expressions in assignment values
            if (stmts[i] is AssignmentStatement asnFunc
                && asnFunc.values.Count == 1 && asnFunc.values[0] is FunctionExpression fe)
                BuildTableConstructors(fe.body.statements);
            else if (stmts[i] is LocalVariableDeclaration lvd
                && lvd.values.Count == 1 && lvd.values[0] is FunctionExpression fe2)
                BuildTableConstructors(fe2.body.statements);

            // look for R(X) = {} followed by R(X).key = value (possibly with gaps)
            if (stmts[i] is AssignmentStatement asn
                && asn.variables.Count == 1 && asn.variables[0] is Register reg
                && asn.values.Count == 1 && asn.values[0] is TableConstructor tc
                && tc.keys.Count == 0)
            {
                for (var j = i + 1; j < stmts.Count; )
                {
                    if (stmts[j] is AssignmentStatement setField
                        && setField.variables.Count == 1
                        && setField.variables[0] is TableAccess ta
                        && ta.table is Register tableReg && tableReg.index == reg.index
                        && setField.values.Count == 1)
                    {
                        tc.keys.Add(ta.key);
                        tc.values.Add(setField.values[0]);
                        stmts.RemoveAt(j);
                    }
                    else if (StmtReferencesRegister(stmts[j], reg.index))
                    {
                        // R(X) is used in this statement — stop
                        break;
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }
    }

    private bool StmtReferencesRegister(Statement stmt, int regIndex)
    {
        if (stmt is AssignmentStatement asn)
        {
            foreach (var v in asn.variables)
                if (v is Register r && r.index == regIndex) return true;
            foreach (var v in asn.values)
                if (ExprReferencesRegister(v, regIndex)) return true;
        }
        else if (stmt is ExpressionStatement es)
            return ExprReferencesRegister(es.expression, regIndex);
        else if (stmt is ReturnStatement rs)
        {
            foreach (var e in rs.expressions)
                if (ExprReferencesRegister(e, regIndex)) return true;
        }
        return false;
    }

    private bool ExprReferencesRegister(Expression expr, int regIndex)
    {
        if (expr is Register r) return r.index == regIndex;
        if (expr is BinaryExpression be)
            return ExprReferencesRegister(be.left, regIndex) || ExprReferencesRegister(be.right, regIndex);
        if (expr is UnaryExpression ue)
            return ExprReferencesRegister(ue.operand, regIndex);
        if (expr is FunctionCall fc)
        {
            if (ExprReferencesRegister(fc.function, regIndex)) return true;
            foreach (var a in fc.arguments)
                if (ExprReferencesRegister(a, regIndex)) return true;
        }
        if (expr is TableAccess ta)
            return ExprReferencesRegister(ta.table, regIndex) || ExprReferencesRegister(ta.key, regIndex);
        return false;
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
                return new BooleanLiteral(addr, Convert.ToBoolean(k.Value));
            case HksType.TNUMBER:
                return new NumberLiteral(addr, Convert.ToDouble(k.Value));
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
