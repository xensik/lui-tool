
using System.Text;

namespace LuiTool.Code;

public class PassWrapRegister : Visitor
{
    private List<Expression> stack;
    private bool remove;
    private bool asn_to_call;

    public PassWrapRegister()
    {
        stack = new List<Expression>();
        remove = false;
        asn_to_call = false;
    }

    public void Visit(Chunk node)
    {
        for (var i = 0; i < node.statements.Count; i++)
        {
            remove = false;
            node.statements[i].Accept(this);

            if (remove)
            {
                node.statements.RemoveAt(i);
                i--;
            }

            // if (asn_to_call)
            // {
            //     asn_to_call = false;
            //     var asn = node.statements[i] as AssignmentStatement;
            //     node.statements[i] = asn
            // }
        }
    }

    public void Visit(Block node)
    {
        for (var i = 0; i < node.statements.Count; i++)
        {
            remove = false;
            node.statements[i].Accept(this);

            if (remove)
            {
                node.statements.RemoveAt(i);
                i--;
            }
        }
    }

    public void Visit(DoStatement node)
    {
    }

    public void Visit(WhileStatement node)
    {
    }

    public void Visit(RepeatUntilStatement node)
    {
    }

    public void Visit(IfStatement node)
    {
    }

    public void Visit(ElseIfBlock node)
    {
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

        if (stack.Count == reg!.index)
            stack.Add(node.variable as Register);
        else if (stack.Count > reg!.index)
            stack[reg!.index] = node.variable as Register;
        else
            throw new Exception();

        node.body.Accept(this);
    }

    public void Visit(ForInStatement node)
    {
    }

    public void Visit(FunctionStatement node)
    {
        node.body.Accept(this);
    }

    public void Visit(LocalFunctionStatement node)
    {
    }

    public void Visit(LocalVariableDeclaration node)
    {
    }

    public void Visit(AssignmentStatement node)
    {
        // join rvalues
        for (var i = 0; i < node.values.Count; i++)
        {
            node.values[i] = Swap(node.values[i]);
        }

        if (node.values.Count == 1 && node.values[0] is FunctionCall)
        {
            if (node.variables.Count == 1)
                asn_to_call = true;
            return;
        }

        if (node.variables.Count == 1 && node.variables[0] is Register)
        {
            var reg = node.variables[0] as Register;

            if (reg!.refcount <= 1)
            {
                remove = true;

                if (stack.Count == reg!.index)
                    stack.Add(node.values[0]);
                else if (stack.Count > reg!.index)
                    stack[reg!.index] = node.values[0];
                else
                    throw new Exception();
            }
            else
            {
                if (stack.Count == reg!.index)
                    stack.Add(node.variables[0]);
                else if (stack.Count > reg!.index)
                    stack[reg!.index] = node.variables[0];
                else
                    throw new Exception();
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

    }

    public void Visit(TableAccess node)
    {
        node.table = Swap(node.table);
        node.key = Swap(node.key);
    }

    public Expression Swap(Expression exp)
    {
        if (exp is Register)
        {
            var reg = exp as Register;
            return stack[reg!.index];
        }

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

                if (stack.Count == reg!.index)
                    stack.Add(node.rhs[0]);
                else if (stack.Count > reg!.index)
                    stack[reg!.index] = node.rhs[0];
                else
                    throw new Exception();
            }
            else
            {
                if (stack.Count == reg!.index)
                    stack.Add(node.lhs[0]);
                else if (stack.Count > reg!.index)
                    stack[reg!.index] = node.lhs[0];
                else
                    throw new Exception();
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
