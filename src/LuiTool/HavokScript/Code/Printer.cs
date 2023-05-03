
using System.Text;

namespace LuiTool.Code;

public class CodePrinterVisitor : Visitor
{
    private StringBuilder stringBuilder;
    private bool lvalue;
    private int indent;

    public CodePrinterVisitor()
    {
        stringBuilder = new StringBuilder();
        lvalue = false;
        indent = 0;
    }

    public void Visit(Chunk node)
    {
        foreach (var statement in node.statements)
        {
            statement.Accept(this);
            stringBuilder.Append("\n");
        }
    }

    public void Visit(Block node)
    {
        indent += 4;

        // stringBuilder.Remove(stringBuilder.Length - 4, 4);
        // stringBuilder.Append("\n");

        foreach (var statement in node.statements)
        {
            stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
            statement.Accept(this);
            stringBuilder.Append("\n");
        }

        stringBuilder.Remove(stringBuilder.Length - 1, 1);

        indent -= 4;
    }

    public void Visit(DoStatement node)
    {
        stringBuilder.Append("do\n");
        node.body.Accept(this);
        stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
        stringBuilder.Append("end");
    }

    public void Visit(WhileStatement node)
    {
        stringBuilder.Append("while ");
        node.test.Accept(this);
        stringBuilder.Append(" do\n");
        node.body.Accept(this);
        stringBuilder.Append("\n");
        stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
        stringBuilder.Append("end");
    }

    public void Visit(RepeatUntilStatement node)
    {
        stringBuilder.Append("repeat\n");
        node.body.Accept(this);
        stringBuilder.Append("\n");
        stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
        stringBuilder.Append("until ");
        node.test.Accept(this);
    }

    public void Visit(IfStatement node)
    {
        stringBuilder.Append("if ");
        node.test.Accept(this);
        stringBuilder.Append(" then\n");
        node.ifBlock.Accept(this);

        foreach (var elseifBlock in node.elseifBlocks)
        {
            stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
            stringBuilder.Append("elseif ");
            elseifBlock.condition.Accept(this);
            stringBuilder.Append(" then\n");
            elseifBlock.block.Accept(this);
        }

        if (node.elseBlock != null)
        {
            stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
            stringBuilder.Append("else\n");
            node.elseBlock.Accept(this);
        }

        stringBuilder.Append("\nend");
    }

    public void Visit(ElseIfBlock node)
    {
        stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
        stringBuilder.Append("elseif ");
        node.condition.Accept(this);
        stringBuilder.Append(" then\n");
        node.block.Accept(this);
    }

    public void Visit(ForStatement node)
    {
        stringBuilder.Append("for ");
        node.variable.Accept(this);
        stringBuilder.Append(" = ");
        node.start.Accept(this);
        stringBuilder.Append(", ");
        node.limit.Accept(this);
    
        if (node.step != null)
        {
            stringBuilder.Append(", ");
            node.step.Accept(this);
        }

        stringBuilder.Append(" do\n");
        node.body.Accept(this);
        stringBuilder.Append("\n");
        stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
        stringBuilder.Append("end");
    }

    public void Visit(ForInStatement node)
    {
        stringBuilder.Append("for ");

        foreach (var variable in node.variables)
        {
            stringBuilder.Append(variable);
            stringBuilder.Append(", ");
        }
        stringBuilder.Append("in ");

        foreach (var expression in node.expressions)
        {
            expression.Accept(this);
            stringBuilder.Append(", ");
        }

        stringBuilder.Remove(stringBuilder.Length - 2, 2);
        stringBuilder.Append(" do\n");
        node.body.Accept(this);
        stringBuilder.Append("\n");
        stringBuilder.AppendFormat("{0, " + indent.ToString() + "}", string.Empty);
        stringBuilder.Append("end");
    }

    public void Visit(FunctionStatement node)
    {
        stringBuilder.Append("function ");

        if (node.name != null)
        {
            node.name.Accept(this);
            //stringBuilder.Append(".");
        }

        stringBuilder.Append("(");

        foreach (var parameter in node.parameters)
        {
            stringBuilder.Append(parameter);
            stringBuilder.Append(", ");
        }

        if (node.parameters.Count > 0)
        {
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
        }

        stringBuilder.Append(")\n");
        node.body.Accept(this);
        stringBuilder.Append("\nend");
    }

    public void Visit(LocalFunctionStatement node)
    {
        stringBuilder.Append("local function ");
        node.name.Accept(this);
        stringBuilder.Append("(");

        foreach (var parameter in node.parameters)
        {
            stringBuilder.Append(parameter);
            stringBuilder.Append(", ");
        }
    
        if (node.parameters.Count > 0)
        {
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
        }
        stringBuilder.Append(")\n");
        node.body.Accept(this);
        stringBuilder.Append("\nend");
    }

    public void Visit(LocalVariableDeclaration node)
    {
        stringBuilder.Append("local ");

        foreach (var variable in node.variables)
        {
            variable.Accept(this);
            stringBuilder.Append(", ");
        }
    
        stringBuilder.Remove(stringBuilder.Length - 2, 2);

        if (node.values.Count > 0)
        {
            stringBuilder.Append(" = ");
        
            foreach (var expression in node.values)
            {
                expression.Accept(this);
                stringBuilder.Append(", ");
            }
    
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
        }
    }

    public void Visit(AssignmentStatement node)
    {
        foreach (var variable in node.variables)
        {
            lvalue = true;
            variable.Accept(this);
            stringBuilder.Append(", ");
            lvalue = false;
        }

        stringBuilder.Remove(stringBuilder.Length - 2, 2);
        stringBuilder.Append(" = ");

        foreach (var expression in node.values)
        {
            expression.Accept(this);
            stringBuilder.Append(", ");
        }

        stringBuilder.Remove(stringBuilder.Length - 2, 2);
    }

    public void Visit(ReturnStatement node)
    {
        stringBuilder.Append("return");

        foreach (var expression in node.expressions)
        {
            expression.Accept(this);
            stringBuilder.Append(", ");
        }

        if (node.expressions.Count > 0)
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
    }

    public void Visit(Identifier node)
    {
        stringBuilder.Append(node.name);
    }

    public void Visit(FunctionCall node)
    {
        node.function.Accept(this);
        stringBuilder.Append("(");

        foreach (var variable in node.arguments)
        {
            variable.Accept(this);
            stringBuilder.Append(", ");
        }

        stringBuilder.Remove(stringBuilder.Length - 2, 2);
        stringBuilder.Append(")");
    }

    public void Visit(NilLiteral node)
    {
        stringBuilder.Append("nil");
    }

    public void Visit(BooleanLiteral node)
    {
        stringBuilder.Append(node.value ? "true" : "false");
    }

    public void Visit(NumberLiteral node)
    {
        stringBuilder.Append(node.value);
    }

    public void Visit(StringLiteral node)
    {
        stringBuilder.Append("\"");
        stringBuilder.Append(node.value.Replace("\\", "\\\\").Replace("\"", "\\\""));
        stringBuilder.Append("\"");
    }

    public void Visit(VarargsLiteral node)
    {
        stringBuilder.Append("...");
    }

    public void Visit(BinaryExpression node)
    {
        node.left.Accept(this);
        stringBuilder.Append(" ");
        stringBuilder.Append(node.op);
        stringBuilder.Append(" ");
        node.right.Accept(this);
    }

    public void Visit(UnaryExpression node)
    {
        stringBuilder.Append(node.op);
        node.operand.Accept(this);
    }


    public void Visit(TableConstructor node)
    {
        // stringBuilder.Append("{");
        // foreach (var field in node.fields) {
        //     field.Accept(this);
        //     stringBuilder.Append(", ");
        // }
        // stringBuilder.Append("}");
    }

    public void Visit(TableAccess node)
    {
        node.table.Accept(this);
        stringBuilder.Append(".");
        node.key.Accept(this);
        //stringBuilder.Append("]");
    }


    public void Visit(Closure node)
    {
        stringBuilder.AppendFormat("C{0}", node.index);
    }

    public void Visit(Register node)
    {
        if (lvalue)
            stringBuilder.AppendFormat("R{0}({1})", node.index, node.refcount);
        else
            stringBuilder.AppendFormat("R{0}", node.index);
    }


    public void Visit(AsmAssign node)
    {
        foreach (var variable in node.lhs)
        {
            lvalue = true;
            variable.Accept(this);
            stringBuilder.Append(", ");
            lvalue = false;
        }

        stringBuilder.Remove(stringBuilder.Length - 2, 2);
        stringBuilder.Append(" = ");

        foreach (var expression in node.rhs)
        {
            expression.Accept(this);
            stringBuilder.Append(", ");
        }

        stringBuilder.Remove(stringBuilder.Length - 2, 2);
    }

    public void Visit(Test node)
    {
        stringBuilder.Append("asm_test( ");
        node.expression.Accept(this);
        stringBuilder.Append(" )");
    }

    public void Visit(Jump node)
    {
        stringBuilder.AppendFormat("asm_jump( {0} )", node.offset);
    }

    public void Visit(AsmForPrep node)
    {
        stringBuilder.Append("asm_forprep( ");
        node.rinit.Accept(this);
        stringBuilder.Append(", ");
        node.rlimit.Accept(this);
        stringBuilder.Append(", ");
        node.rstep.Accept(this);
        stringBuilder.Append(", ");
        node.rvar.Accept(this);
        stringBuilder.Append(" )");
    }

    public void Visit(AsmForLoop node)
    {
        stringBuilder.AppendFormat("asm_forloop( {0} )", node.offset);
    }

    public void Visit(AsmTForLoop node)
    {
        stringBuilder.Append("asm_tforloop()");
    }

    public string GetCode()
    {
        return stringBuilder.ToString();
    }
}
