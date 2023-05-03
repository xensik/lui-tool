using System.Collections.Generic;

namespace LuiTool.Code;

public abstract class Node
{
    public int Address;

    public abstract void Accept(Visitor visitor);
}

public abstract class Statement : Node
{
}

public abstract class Expression : Node
{
}

public class Chunk : Node
{
    public List<Statement> statements;

    public Chunk(int addr, List<Statement> statements)
    {
        this.Address = addr;
        this.statements = statements;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class Block : Statement
{
    public List<Statement> statements;

    public Block(int addr, List<Statement> statements)
    {
        this.Address = addr;
        this.statements = statements;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class DoStatement : Statement
{
    public Block body;

    public DoStatement(int addr, Block body)
    {
        this.Address = addr;
        this.body = body;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class WhileStatement : Statement
{
    public Expression test;
    public Block body;

    public WhileStatement(int addr, Expression test, Block body)
    {
        this.Address = addr;
        this.test = test;
        this.body = body;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class RepeatUntilStatement : Statement
{
    public Expression test;
    public Block body;

    public RepeatUntilStatement(int addr, Expression test, Block body)
    {
        this.Address = addr;
        this.test = test;
        this.body = body;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class IfStatement : Statement
{
    public Expression test;
    public Block ifBlock;
    public List<ElseIfBlock> elseifBlocks;
    public Block elseBlock;

    public IfStatement(int addr, Expression test, Block ifBlock, List<ElseIfBlock> elseifBlocks, Block elseBlock)
    {
        this.Address = addr;
        this.test = test;
        this.ifBlock = ifBlock;
        this.elseifBlocks = elseifBlocks;
        this.elseBlock = elseBlock;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ElseIfBlock : Node
{
    public Expression condition;
    public Block block;

    public ElseIfBlock(int addr, Expression condition, Block block)
    {
        this.Address = addr;
        this.condition = condition;
        this.block = block;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ForStatement : Statement
{
    public Node variable;
    public Node start;
    public Node limit;
    public Node step;
    public Block body;

    public ForStatement(int addr, Node variable, Node start, Node limit, Node step, Block body)
    {
        this.Address = addr;
        this.variable = variable;
        this.start = start;
        this.limit = limit;
        this.step = step;
        this.body = body;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ForInStatement : Statement
{
    public List<string> variables;
    public List<Expression> expressions;
    public Block body;

    public ForInStatement(int addr, List<string> variables, List<Expression> expressions, Block body)
    {
        this.Address = addr;
        this.variables = variables;
        this.expressions = expressions;
        this.body = body;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class FunctionStatement : Statement
{
    public Identifier name;
    public List<string> parameters;
    public Block body;

    public FunctionStatement(int addr, Identifier name, List<string> parameters, Block body)
    {
        this.Address = addr;
        this.name = name;
        this.parameters = parameters;
        this.body = body;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class LocalFunctionStatement : Statement
{
    public Identifier name;
    public List<string> parameters;
    public Block body;

    public LocalFunctionStatement(int addr, Identifier name, List<string> parameters, Block body)
    {
        this.Address = addr;
        this.name = name;
        this.parameters = parameters;
        this.body = body;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class LocalVariableDeclaration : Statement
{
    public List<Identifier> variables;
    public List<Expression> values;

    public LocalVariableDeclaration(int addr, List<Identifier> variables, List<Expression> values)
    {
        this.Address = addr;
        this.variables = variables;
        this.values = values;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class AssignmentStatement : Statement
{
    public List<Expression> variables;
    public List<Expression> values;

    public AssignmentStatement(int addr, List<Expression> variables, List<Expression> values)
    {
        this.Address = addr;
        this.variables = variables;
        this.values = values;
    }

    public AssignmentStatement(int addr, List<Expression> variables, Expression value)
    {
        this.Address = addr;
        this.variables = variables;
        this.values = new List<Expression>();
        values.Add(value);
    }

    public AssignmentStatement(int addr, Expression variable, Expression value)
    {
        this.Address = addr;
        this.variables = new List<Expression>();
        this.values = new List<Expression>();
        variables.Add(variable);
        values.Add(value);
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class ReturnStatement : Statement
{
    public List<Expression> expressions;

    public ReturnStatement(int addr, List<Expression> expressions)
    {
        this.Address = addr;
        this.expressions = expressions;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class Closure : Expression
{
    public int index;

    public Closure(int addr, int index)
    {
        this.Address = addr;
        this.index = index;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class Register : Expression
{
    public int index;
    public int refcount;

    public Register(int addr, int index)
    {
        this.Address = addr;
        this.index = index;
        this.refcount = 0;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class Identifier : Expression
{
    public string name;

    public Identifier(int addr, string name)
    {
        this.Address = addr;
        this.name = name;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class FunctionCall : Expression
{
    public Expression function;
    public List<Expression> arguments;

    public FunctionCall(int addr, Expression function, List<Expression> arguments)
    {
        this.Address = addr;
        this.function = function;
        this.arguments = arguments;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class VarargsLiteral : Expression
{
    public VarargsLiteral(int addr)
    {
        this.Address = addr;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class NilLiteral : Expression
{
    public NilLiteral(int addr)
    {
        this.Address = addr;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class BooleanLiteral : Expression
{
    public bool value;

    public BooleanLiteral(int addr, bool value)
    {
        this.Address = addr;
        this.value = value;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class NumberLiteral : Expression
{
    public double value;

    public NumberLiteral(int addr, double value)
    {
        this.Address = addr;
        this.value = value;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class StringLiteral : Expression
{
    public string value;

    public StringLiteral(int addr, string value)
    {
        this.Address = addr;
        this.value = value;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}


public class BinaryExpression : Expression
{
    public Expression left;
    public Expression right;
    public string op;

    public BinaryExpression(int addr, Expression left, Expression right, string op)
    {
        this.Address = addr;
        this.left = left;
        this.right = right;
        this.op = op;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class UnaryExpression : Expression
{
    public Expression operand;
    public string op;

    public UnaryExpression(int addr, Expression operand, string op)
    {
        this.Address = addr;
        this.operand = operand;
        this.op = op;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class TableConstructor : Expression
{
    public List<Expression> keys;
    public List<Expression> values;

    public TableConstructor(int addr, List<Expression> keys, List<Expression> values)
    {
        this.Address = addr;
        this.keys = keys;
        this.values = values;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class TableAccess : Expression
{
    public Expression table;
    public Expression key;

    public TableAccess(int addr, Expression table, Expression key)
    {
        this.Address = addr;
        this.table = table;
        this.key = key;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}



///
public class AsmAssign : Statement
{
    public List<Expression> lhs;
    public List<Expression> rhs;

    public AsmAssign(int addr, List<Expression> lhs, List<Expression> rhs)
    {
        this.Address = addr;
        this.lhs = lhs;
        this.rhs = rhs;
    }

    public AsmAssign(int addr, List<Expression> lhs, Expression rhs)
    {
        this.Address = addr;
        this.lhs = lhs;
        this.rhs = new List<Expression>();
        this.rhs.Add(rhs);
    }

    public AsmAssign(int addr, Expression lhs, Expression rhs)
    {
        this.Address = addr;
        this.lhs = new List<Expression>();
        this.rhs = new List<Expression>();
        this.lhs.Add(lhs);
        this.rhs.Add(rhs);
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class Test : Statement
{
    public Expression expression;

    public Test(int addr, Expression expression)
    {
        this.Address = addr;
        this.expression = expression;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class Jump : Statement
{
    public int offset;

    public Jump(int addr, int offset)
    {
        this.Address = addr;
        this.offset = offset;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class AsmForPrep : Statement
{
    public Register rinit;
    public Register rlimit;
    public Register rstep;
    public Register rvar;

    public AsmForPrep(int addr, Register rinit, Register rlimit, Register rstep, Register rvar)
    {
        this.Address = addr;
        this.rinit = rinit;
        this.rlimit = rlimit;
        this.rstep = rstep;
        this.rvar = rvar;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class AsmForLoop : Statement
{
    public int offset;
    public Expression exp;

    public AsmForLoop(int addr, Expression exp, int offset)
    {
        this.Address = addr;
        this.exp = exp;
        this.offset = offset;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public class AsmTForLoop : Statement
{

    public AsmTForLoop(int addr)
    {
        this.Address = addr;
    }

    public override void Accept(Visitor visitor)
    {
        visitor.Visit(this);
    }
}

public interface Visitor
{
    void Visit(Chunk node);
    void Visit(Block node);
    void Visit(DoStatement node);
    void Visit(WhileStatement node);
    void Visit(RepeatUntilStatement node);
    void Visit(IfStatement node);
    void Visit(ElseIfBlock node);
    void Visit(ForStatement node);
    void Visit(ForInStatement node);
    void Visit(FunctionStatement node);
    void Visit(LocalFunctionStatement node);
    void Visit(LocalVariableDeclaration node);
    void Visit(AssignmentStatement node);
    void Visit(ReturnStatement node);

    void Visit(VarargsLiteral node);
    void Visit(NilLiteral node);
    void Visit(BooleanLiteral node);
    void Visit(NumberLiteral node);
    void Visit(StringLiteral node);
    void Visit(Closure node);
    void Visit(Register node);
    void Visit(Identifier node);
    void Visit(FunctionCall node);
    void Visit(BinaryExpression node);
    void Visit(UnaryExpression node);
    void Visit(TableConstructor node);
    void Visit(TableAccess node);


    void Visit(AsmAssign node);
    void Visit(Test node);
    void Visit(Jump node);
    void Visit(AsmForPrep node);
    void Visit(AsmForLoop node);
    void Visit(AsmTForLoop node);
}

