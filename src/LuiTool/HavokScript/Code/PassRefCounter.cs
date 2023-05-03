
using System.Text;

namespace LuiTool.Code;

public class PassRefCounter : Visitor
{
    private List<Node> stack;
    private bool lvalue;

    public PassRefCounter()
    {
        stack = new List<Node>();
        lvalue = false;
    }

    public void Visit(Chunk node)
    {
        foreach (var statement in node.statements)
        {
            statement.Accept(this);
        }
    }

    public void Visit(Block node)
    {
        foreach (var statement in node.statements)
        {
            statement.Accept(this);
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
        node.limit.Accept(this);
        node.step.Accept(this);

        lvalue = true;
        node.variable.Accept(this);
        lvalue = false;

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
        foreach (var expression in node.values)
        {
            expression.Accept(this);
        }

        foreach (var variable in node.variables)
        {
            lvalue = true;
            variable.Accept(this);
            lvalue = false;
        }
    }

    public void Visit(ReturnStatement node)
    {
        foreach (var expression in node.expressions)
        {
            expression.Accept(this);
        }
    }

    public void Visit(Closure node)
    {
    }

    public void Visit(Register node)
    {
        if (lvalue)
        {
            if (stack.Count == node.index)
                stack.Add(node);
            else if (stack.Count > node.index)
            {
                stack[node.index] = node;
            }
            else
            {
                throw new Exception();
            }
        }
        else
        {
            var reg = stack[node.index] as Register;
            reg!.refcount++;
        }
    }

    public void Visit(Identifier node)
    {
    }

    public void Visit(FunctionCall node)
    {
        node.function.Accept(this);

        foreach (var variable in node.arguments)
        {
            variable.Accept(this);
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
        node.left.Accept(this);
        node.right.Accept(this);
    }

    public void Visit(UnaryExpression node)
    {
        node.operand.Accept(this);
    }


    public void Visit(TableConstructor node)
    {

    }

    public void Visit(TableAccess node)
    {
        node.table.Accept(this);
        node.key.Accept(this);
    }

    public void Visit(AsmAssign node)
    {
        foreach (var exp in node.rhs)
        {
            exp.Accept(this);
        }

        foreach (var exp in node.lhs)
        {
            lvalue = true;
            exp.Accept(this);
            lvalue = false;
        }
    }

    public void Visit(Test node)
    {
        node.expression.Accept(this);
    }

    public void Visit(Jump node)
    {
    }

    public void Visit(AsmForPrep node)
    {
        node.rinit.Accept(this);
        node.rlimit.Accept(this);
        node.rstep.Accept(this);

        lvalue = true;
        node.rvar.Accept(this);
        lvalue = false;
    }

    public void Visit(AsmForLoop node)
    {
    }

    public void Visit(AsmTForLoop node)
    {
    }
}
