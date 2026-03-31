
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
        node.body.Accept(this);
    }

    public void Visit(WhileStatement node)
    {
        node.test.Accept(this);
        node.body.Accept(this);
    }

    public void Visit(RepeatUntilStatement node)
    {
        node.body.Accept(this);
        node.test.Accept(this);
    }

    public void Visit(IfStatement node)
    {
        node.test.Accept(this);
        node.ifBlock.Accept(this);
        foreach (var elseif in node.elseifBlocks)
            elseif.Accept(this);
        node.elseBlock?.Accept(this);
    }

    public void Visit(ElseIfBlock node)
    {
        node.condition.Accept(this);
        node.block.Accept(this);
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
        foreach (var exp in node.expressions)
            exp.Accept(this);
        node.body.Accept(this);
    }

    public void Visit(FunctionStatement node)
    {
        var prevStack = stack;
        var prevLvalue = lvalue;
        stack = new List<Node>();
        lvalue = false;
        for (var i = 0; i < node.parameters.Count; i++)
            stack.Add(new Register(node.Address, i));
        node.body.Accept(this);
        stack = prevStack;
        lvalue = prevLvalue;
    }

    public void Visit(LocalFunctionStatement node)
    {
        node.body.Accept(this);
    }

    public void Visit(LocalVariableDeclaration node)
    {
        foreach (var val in node.values)
            val.Accept(this);
        foreach (var v in node.variables)
        {
            lvalue = true;
            v.Accept(this);
            lvalue = false;
        }
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

    public void Visit(ExpressionStatement node)
    {
        node.expression.Accept(this);
    }

    public void Visit(Closure node)
    {
    }

    public void Visit(Register node)
    {
        if (lvalue)
        {
            while (stack.Count <= node.index)
                stack.Add(null);
            node.refcount = 0;
            stack[node.index] = node;
        }
        else
        {
            if (node.index < stack.Count && stack[node.index] is Register reg)
                reg.refcount++;
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
        for (var i = 0; i < node.values.Count; i++)
        {
            node.keys[i]?.Accept(this);
            node.values[i].Accept(this);
        }
    }

    public void Visit(FunctionExpression node)
    {
        var prevStack = stack;
        var prevLvalue = lvalue;
        stack = new List<Node>();
        lvalue = false;
        for (var i = 0; i < node.parameters.Count; i++)
            stack.Add(new Register(node.Address, i));
        node.body.Accept(this);
        stack = prevStack;
        lvalue = prevLvalue;
    }

    public void Visit(TableAccess node)
    {
        var prevLvalue = lvalue;
        lvalue = false;
        node.table.Accept(this);
        node.key.Accept(this);
        lvalue = prevLvalue;
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
