using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;

public class TypeChecker : PLCBaseVisitor<string>
{
    private readonly Dictionary<string, string> symbolTable = new();
    public List<string> Errors { get; } = new();

    public override string VisitDeclaration(PLCParser.DeclarationContext context)
    {
        string type = context.TYPE().GetText();

        foreach (var id in context.ID())
        {
            string varName = id.GetText();

            if (symbolTable.ContainsKey(varName))
            {
                Errors.Add($"[Line {id.Symbol.Line}, Pos {id.Symbol.Column}] Variable '{varName}' is already declared.");
            }
            else
            {
                symbolTable[varName] = type;
            }
        }

        return null;
    }

    public override string VisitAssignment(PLCParser.AssignmentContext context)
    {
        if (context.ChildCount != 3) return Visit(context.GetChild(0));

        var leftId = context.GetChild(0).GetText();
        var right = context.assignment();

        if (!symbolTable.TryGetValue(leftId, out string varType))
        {
            Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Variable '{leftId}' is not declared.");
            return null;
        }

        string rightType = Visit(right);

        if (varType == "float" && rightType == "int")
        {
            return "float";
        }

        if (varType != rightType)
        {
            Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Cannot assign type '{rightType}' to variable '{leftId}' of type '{varType}'.");
        }

        return varType;
    }

    public override string VisitPrimary(PLCParser.PrimaryContext context)
    {
        if (context.INT() != null) return "int";
        if (context.FLOAT() != null) return "float";
        if (context.BOOL() != null) return "bool";
        if (context.STRING() != null) return "string";
        if (context.ID() != null)
        {
            string id = context.ID().GetText();
            if (!symbolTable.TryGetValue(id, out string type))
            {
                Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Variable '{id}' is not declared.");
                return "error";
            }
            return type;
        }
        if (context.expression() != null)
            return Visit(context.expression());
        return "error";
    }

    public override string VisitAddition(PLCParser.AdditionContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string leftType = Visit(context.GetChild(0));
        string op = context.GetChild(1).GetText();
        string rightType = Visit(context.GetChild(2));

        if (op == "." && leftType == "string" && rightType == "string")
            return "string";

        if ((leftType == "int" || leftType == "float") && (rightType == "int" || rightType == "float"))
            return (leftType == "float" || rightType == "float") ? "float" : "int";

        Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Operator '{op}' not supported for types '{leftType}' and '{rightType}'.");
        return "error";
    }

    public override string VisitMultiplication(PLCParser.MultiplicationContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string left = Visit(context.GetChild(0));
        string right = Visit(context.GetChild(2));
        string op = context.GetChild(1).GetText();

        if ((left == "int" || left == "float") && (right == "int" || right == "float"))
        {
            if (op == "%" && (left != "int" || right != "int"))
            {
                Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Modulo '%' is only valid for integers.");
                return "error";
            }
            return (left == "float" || right == "float") ? "float" : "int";
        }

        Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Operator '{op}' not supported for types '{left}' and '{right}'.");
        return "error";
    }

    public override string VisitComparison(PLCParser.ComparisonContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string left = Visit(context.GetChild(0));
        string right = Visit(context.GetChild(2));

        if ((left == "int" || left == "float") && (right == "int" || right == "float"))
            return "bool";

        Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Comparison not valid for types '{left}' and '{right}'.");
        return "error";
    }

    public override string VisitEquality(PLCParser.EqualityContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string left = Visit(context.GetChild(0));
        string right = Visit(context.GetChild(2));

        if (left == right && (left == "int" || left == "float" || left == "string"))
            return "bool";

        Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Equality operator not valid between '{left}' and '{right}'.");
        return "error";
    }

    public override string VisitLogic_and(PLCParser.Logic_andContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string left = Visit(context.GetChild(0));
        string right = Visit(context.GetChild(2));

        if (left == "bool" && right == "bool")
            return "bool";

        Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Logical AND requires boolean operands.");
        return "error";
    }

    public override string VisitLogic_or(PLCParser.Logic_orContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string left = Visit(context.GetChild(0));
        string right = Visit(context.GetChild(2));

        if (left == "bool" && right == "bool")
            return "bool";

        Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Logical OR requires boolean operands.");
        return "error";
    }

    public override string VisitUnary(PLCParser.UnaryContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string op = context.GetChild(0).GetText();
        string operandType = Visit(context.GetChild(1));

        if (op == "-" && (operandType == "int" || operandType == "float"))
            return operandType;

        if (op == "!" && operandType == "bool")
            return "bool";

        Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Unary operator '{op}' not applicable to type '{operandType}'.");
        return "error";
    }

    public override string VisitExpressionStatement(PLCParser.ExpressionStatementContext context)
    {
        Visit(context.expression());
        return null;
    }

    public override string VisitWriteStatement(PLCParser.WriteStatementContext context)
    {
        foreach (var expr in context.expression())
        {
            Visit(expr);
        }
        return null;
    }

    public override string VisitIfStatement(PLCParser.IfStatementContext context)
    {
        string condType = Visit(context.expression());
        if (condType != "bool")
        {
            Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Condition in 'if' must be of type bool, got '{condType}'.");
        }

        Visit(context.statement(0));
        if (context.statement().Length > 1)
            Visit(context.statement(1));

        return null;
    }

    public override string VisitWhileStatement(PLCParser.WhileStatementContext context)
    {
        string condType = Visit(context.expression());
        if (condType != "bool")
        {
            Errors.Add($"[Line {context.Start.Line}, Pos {context.Start.Column}] Condition in 'while' must be of type bool, got '{condType}'.");
        }

        Visit(context.statement());
        return null;
    }

    public override string VisitForStatement(PLCParser.ForStatementContext context)
    {
        string type = context.TYPE().GetText();
        string name = context.ID().GetText();

        if (symbolTable.ContainsKey(name))
            Errors.Add($"[Line {context.Start.Line}] Variable '{name}' already declared.");

        symbolTable[name] = type;

        Visit(context.expression(0));

        string condType = Visit(context.expression(1));

        if (condType != "bool")
            Errors.Add($"[Line {context.Start.Line}] Condition in 'for' must be of type bool, got '{condType}'.");

        Visit(context.expression(2));

        Visit(context.statement());

        return null;
    }


    public override string VisitBlock(PLCParser.BlockContext context)
    {
        foreach (var stmt in context.statement())
            Visit(stmt);

        return null;
    }

    public override string VisitProgram(PLCParser.ProgramContext context)
    {
        foreach (var stmt in context.statement())
            Visit(stmt);

        return null;
    }
}
