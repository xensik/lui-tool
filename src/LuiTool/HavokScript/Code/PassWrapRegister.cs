
using System.Text;

namespace LuiTool.Code;

public class PassWrapRegister : Visitor
{
    private List<Expression> stack;
    private bool remove;
    private Statement replace;
    private bool asn_to_call;
    private bool preserveEmptyTables;
    private int funcIndex;
    private int nextFuncIndex;
    private int localCount;
    private Dictionary<string, Expression> upvalMap;

    public PassWrapRegister(bool preserveEmptyTables = false)
    {
        stack = new List<Expression>();
        remove = false;
        replace = null;
        asn_to_call = false;
        this.preserveEmptyTables = preserveEmptyTables;
        funcIndex = 0;
        nextFuncIndex = 1;
        localCount = 0;
        upvalMap = null;
    }

    public void Visit(Chunk node)
    {
        for (var i = 0; i < node.statements.Count; i++)
        {
            remove = false;
            replace = null;
            node.statements[i].Accept(this);

            if (remove)
            {
                node.statements.RemoveAt(i);
                i--;
            }
            else if (replace != null)
            {
                node.statements[i] = replace;
                replace = null;
            }
        }
    }

    public void Visit(Block node)
    {
        for (var i = 0; i < node.statements.Count; i++)
        {
            remove = false;
            replace = null;
            node.statements[i].Accept(this);

            if (remove)
            {
                node.statements.RemoveAt(i);
                i--;
            }
            else if (replace != null)
            {
                node.statements[i] = replace;
                replace = null;
            }
        }
    }

    public void Visit(DoStatement node)
    {
        node.body.Accept(this);
    }

    public void Visit(WhileStatement node)
    {
        node.test = Swap(node.test);
        node.body.Accept(this);
    }

    public void Visit(RepeatUntilStatement node)
    {
        node.body.Accept(this);
        node.test = Swap(node.test);
    }

    public void Visit(IfStatement node)
    {
        node.test = Swap(node.test);
        node.ifBlock.Accept(this);
        foreach (var elseif in node.elseifBlocks)
        {
            elseif.condition = Swap(elseif.condition);
            elseif.block.Accept(this);
        }
        node.elseBlock?.Accept(this);
    }

    public void Visit(ElseIfBlock node)
    {
        node.condition = Swap(node.condition);
        node.block.Accept(this);
    }

    public void Visit(ForStatement node)
    {
        node.start.Accept(this);

        if (remove)
        {
            var asn = node.start as AssignmentStatement;
            node.start = asn.values[0];
            remove = false;
        }

        node.limit.Accept(this);

        if (remove)
        {
            var asn = node.limit as AssignmentStatement;
            node.limit = asn.values[0];
            remove = false;
        }

        node.step.Accept(this);

        if (remove)
        {
            var asn = node.step as AssignmentStatement;
            node.step = asn.values[0];
            remove = false;
        }

        var reg = node.variable as Register;

        while (stack.Count <= reg!.index)
            stack.Add(null);
        stack[reg!.index] = node.variable as Register;

        node.body.Accept(this);
    }

    public void Visit(ForInStatement node)
    {
        for (var i = 0; i < node.expressions.Count; i++)
            node.expressions[i] = Swap(node.expressions[i]);
        node.body.Accept(this);
    }

    public void Visit(FunctionStatement node)
    {
        var prevStack = stack;
        var prevRemove = remove;
        var prevReplace = replace;
        var prevFuncIndex = funcIndex;
        var prevLocalCount = localCount;
        stack = new List<Expression>();
        remove = false;
        replace = null;
        funcIndex = nextFuncIndex++;
        localCount = 0;
        for (var i = 0; i < node.parameters.Count; i++)
        {
            node.parameters[i] = $"arg_{i}";
            stack.Add(new Identifier(node.Address, node.parameters[i]));
        }
        node.body.Accept(this);
        stack = prevStack;
        remove = prevRemove;
        replace = prevReplace;
        funcIndex = prevFuncIndex;
        localCount = prevLocalCount;
    }

    public void Visit(LocalFunctionStatement node)
    {
        node.body.Accept(this);
    }

    public void Visit(LocalVariableDeclaration node)
    {
        for (var i = 0; i < node.values.Count; i++)
            node.values[i] = Swap(node.values[i]);
    }

    public void Visit(AssignmentStatement node)
    {
        // join rvalues
        for (var i = 0; i < node.values.Count; i++)
        {
            node.values[i] = Swap(node.values[i]);
        }

        // swap inside table access lvalues
        for (var i = 0; i < node.variables.Count; i++)
        {
            if (node.variables[i] is TableAccess ta)
            {
                ta.table = Swap(ta.table);
                ta.key = Swap(ta.key);
            }
        }

        if (node.values.Count == 1 && node.values[0] is FunctionCall)
        {
            if (node.variables.Count == 1 && node.variables[0] is Register callReg)
            {
                if (callReg.refcount > 1)
                {
                    var name = $"var_{funcIndex}_{localCount++}";
                    var id = new Identifier(callReg.Address, name);
                    while (stack.Count <= callReg.index)
                        stack.Add(null);
                    stack[callReg.index] = id;
                    replace = new LocalVariableDeclaration(node.Address, new List<Identifier> { id }, node.values);
                }
                else
                {
                    while (stack.Count <= callReg.index)
                        stack.Add(null);
                    stack[callReg.index] = node.variables[0];
                }
            }
            return;
        }

        // function expressions (closures) — always create named locals (may be referenced via upvalues)
        if (node.values.Count == 1 && node.values[0] is FunctionExpression)
        {
            if (node.variables.Count == 1 && node.variables[0] is Register funcReg)
            {
                var name = $"var_{funcIndex}_{localCount++}";
                var id = new Identifier(funcReg.Address, name);
                while (stack.Count <= funcReg.index)
                    stack.Add(null);
                stack[funcReg.index] = id;
                replace = new LocalVariableDeclaration(node.Address, new List<Identifier> { id }, node.values);
            }
            return;
        }

        // don't eliminate empty table constructors — they are merging anchors for table building
        if (preserveEmptyTables && node.values.Count == 1 && node.values[0] is TableConstructor tc && tc.keys.Count == 0)
        {
            return;
        }

        if (node.variables.Count == 1 && node.variables[0] is Register)
        {
            var reg = node.variables[0] as Register;

            if (reg!.refcount <= 1)
            {
                remove = true;
                while (stack.Count <= reg!.index)
                    stack.Add(null);
                stack[reg!.index] = node.values[0];
            }
            else
            {
                var name = $"var_{funcIndex}_{localCount++}";
                var id = new Identifier(reg!.Address, name);
                while (stack.Count <= reg!.index)
                    stack.Add(null);
                stack[reg!.index] = id;
                replace = new LocalVariableDeclaration(node.Address, new List<Identifier> { id }, node.values);
            }
        }
    }

    public void Visit(ReturnStatement node)
    {
        for (var i = 0; i < node.expressions.Count; i++)
        {
            node.expressions[i] = Swap(node.expressions[i]);
        }
    }

    public void Visit(ExpressionStatement node)
    {
        node.expression = Swap(node.expression);
        node.expression.Accept(this);
    }

    public void Visit(Closure node)
    {
    }

    public void Visit(Register node)
    {
    }

    public void Visit(Identifier node)
    {
    }

    public void Visit(FunctionCall node)
    {
        node.function.Accept(this);
        node.function = Swap(node.function);

        for (var i = 0; i < node.arguments.Count; i++)
        {
            node.arguments[i] = Swap(node.arguments[i]);
        }

        // detect method call pattern: obj["method"](obj, ...) or obj.method(obj, ...)
        if (node.function is TableAccess ta && node.arguments.Count > 0)
        {
            if (ExpressionsEqual(ta.table, node.arguments[0]))
            {
                node.isMethodCall = true;
            }
        }
    }

    private bool ExpressionsEqual(Expression a, Expression b)
    {
        if (a is Identifier idA && b is Identifier idB)
            return idA.name == idB.name;
        if (a is Register regA && b is Register regB)
            return regA.index == regB.index;
        return false;
    }

    public void Visit(NilLiteral node)
    {
    }

    public void Visit(BooleanLiteral node)
    {
    }

    public void Visit(NumberLiteral node)
    {
    }

    public void Visit(StringLiteral node)
    {
    }

    public void Visit(VarargsLiteral node)
    {
    }

    public void Visit(BinaryExpression node)
    {
        node.left = Swap(node.left);
        node.right = Swap(node.right);
    }

    public void Visit(UnaryExpression node)
    {
        node.operand = Swap(node.operand);
    }

    public void Visit(TableConstructor node)
    {
        for (var i = 0; i < node.values.Count; i++)
        {
            node.values[i] = Swap(node.values[i]);
            if (node.keys[i] != null)
                node.keys[i] = Swap(node.keys[i]);
        }
    }

    public void Visit(FunctionExpression node)
    {
        var prevStack = stack;
        var prevRemove = remove;
        var prevReplace = replace;
        var prevFuncIndex = funcIndex;
        var prevLocalCount = localCount;
        var prevUpvalMap = upvalMap;

        // build upvalue resolution map from parent stack
        if (node.upvalueMapping != null)
        {
            upvalMap = new Dictionary<string, Expression>();
            for (var i = 0; i < node.upvalueMapping.Count; i++)
            {
                var parentRegIdx = node.upvalueMapping[i];
                if (parentRegIdx < prevStack.Count && prevStack[parentRegIdx] != null)
                    upvalMap[$"upval{i}"] = prevStack[parentRegIdx];
            }
        }
        else
        {
            upvalMap = null;
        }

        stack = new List<Expression>();
        remove = false;
        replace = null;
        funcIndex = nextFuncIndex++;
        localCount = 0;
        for (var i = 0; i < node.parameters.Count; i++)
        {
            node.parameters[i] = $"arg_{i}";
            stack.Add(new Identifier(node.Address, node.parameters[i]));
        }
        node.body.Accept(this);
        stack = prevStack;
        remove = prevRemove;
        replace = prevReplace;
        funcIndex = prevFuncIndex;
        localCount = prevLocalCount;
        upvalMap = prevUpvalMap;
    }

    public void Visit(TableAccess node)
    {
        node.table = Swap(node.table);
        node.key = Swap(node.key);
    }

    public Expression Swap(Expression exp)
    {
        if (exp is Register reg)
        {
            if (reg.index < stack.Count && stack[reg.index] != null)
                return stack[reg.index];
            return exp;
        }

        if (exp is Identifier id && upvalMap != null && upvalMap.TryGetValue(id.name, out var resolved))
            return resolved;

        exp.Accept(this);
        return exp;
    }


    public void Visit(AsmAssign node)
    {
        // join rvalues
        for (var i = 0; i < node.rhs.Count; i++)
        {
            node.rhs[i] = Swap(node.rhs[i]);
        }

        if (node.rhs.Count == 1 && node.rhs[0] is FunctionCall)
        {
            if (node.lhs.Count == 1)
                asn_to_call = true;
            return;
        }

        if (node.lhs.Count == 1 && node.lhs[0] is Register)
        {
            var reg = node.lhs[0] as Register;

            if (reg!.refcount <= 1)
            {
                remove = true;
                while (stack.Count <= reg!.index)
                    stack.Add(null);
                stack[reg!.index] = node.rhs[0];
            }
            else
            {
                while (stack.Count <= reg!.index)
                    stack.Add(null);
                stack[reg!.index] = node.lhs[0];
            }
        }
    }

    public void Visit(Test node)
    {
        node.expression = Swap(node.expression);
    }
    
    public void Visit(Jump node)
    {
    }

    public void Visit(AsmForPrep node)
    {
    }

    public void Visit(AsmForLoop node)
    {
    }

    public void Visit(AsmTForLoop node)
    {
    }
}
