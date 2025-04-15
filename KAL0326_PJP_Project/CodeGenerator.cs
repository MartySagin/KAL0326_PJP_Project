using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.IO;

public class CodeGenerator : PLCBaseVisitor<string>
{
    private readonly List<string> instructions = new();
    private readonly Dictionary<string, string> symbolTable = new();

    private int labelCounter = -1;

    /// <summary>
    /// Metoda, kterou zavoláme po dokončení generování – zapíše instrukce do souboru.
    /// </summary>
    public void WriteToFile(string path)
    {
        File.WriteAllLines(path, instructions);
    }

    /// <summary>
    /// Pomocná metoda pro přidání instrukce do seznamu.
    /// </summary>
    private void Emit(string instruction)
    {
        instructions.Add(instruction);
    }

    /// <summary>
    /// Vygeneruje nové unikátní ID labelu a vrátí jej.
    /// </summary>
    private int GetNewLabel()
    {
        return ++labelCounter;
    }

    // ------------------------------------------------------------------------
    // Program, blok
    // ------------------------------------------------------------------------

    public override string VisitProgram(PLCParser.ProgramContext context)
    {
        // Všechny vrcholové statementy zpracujeme sekvenčně.
        foreach (var stmt in context.statement())
        {
            Visit(stmt);
        }
        return null;
    }

    public override string VisitBlock(PLCParser.BlockContext context)
    {
        // Blok je sekvence příkazů obalená { }. Normálně jen projdeme příkazy uvnitř.
        foreach (var stmt in context.statement())
        {
            Visit(stmt);
        }
        return null;
    }

    // ------------------------------------------------------------------------
    // Deklarace - součást "obdobnosti" k vzoru:
    //  Každou proměnnou inicializujeme default hodnotou (0, 0.0, "", false).
    // ------------------------------------------------------------------------

    public override string VisitDeclaration(PLCParser.DeclarationContext context)
    {
        string type = context.TYPE().GetText();

        foreach (var id in context.ID())
        {
            string varName = id.GetText();
            symbolTable[varName] = type;

            // Default hodnota: 0 pro int, 0.0 pro float, false pro bool, "" pro string
            string defaultPush = type switch
            {
                "int" => "push I 0",
                "float" => "push F 0.0",
                "bool" => "push B false",
                "string" => "push S \"\"",
                _ => null
            };
            if (defaultPush != null)
            {
                Emit(defaultPush);
                Emit($"save {varName}");
            }
        }
        return null;
    }

    // ------------------------------------------------------------------------
    // Přiřazení
    //  Kromě standardní logiky (push expr -> save var),
    //  ještě přidáme load var + pop, aby se to trochu podobně "ukazovalo" jako ve vzoru.
    // ------------------------------------------------------------------------

    public override string VisitAssignment(PLCParser.AssignmentContext ctx)
    {
        if (ctx.ChildCount == 3 && ctx.assignment() != null)
        {
            string leftVar = ctx.GetChild(0).GetText();

            string rightType = Visit(ctx.assignment());

            if (symbolTable[leftVar] == "float" && rightType == "int")
            {
                Emit("itof");
            }

            Emit($"save {leftVar}");

            Emit($"load {leftVar}");

            return symbolTable[leftVar];
        }
        else if (ctx.ChildCount == 3)
        {
            string leftVar = ctx.GetChild(0).GetText();
            string rightType = Visit(ctx.GetChild(2));

            if (symbolTable[leftVar] == "float" && rightType == "int")
            {
                Emit("itof");
            }

            Emit($"save {leftVar}");

            Emit($"load {leftVar}");
            Emit("pop");

            return symbolTable[leftVar];
        }
        else
        {
            string exprType = Visit(ctx.GetChild(0));

            return exprType;
        }
    }



    // ------------------------------------------------------------------------
    // Statementy: read, write, if, while, ...
    // ------------------------------------------------------------------------

    public override string VisitReadStatement(PLCParser.ReadStatementContext context)
    {
   
        foreach (var varNode in context.ID())
        {
            string varName = varNode.GetText();

            if (symbolTable.TryGetValue(varName, out string type))
            {
               
                string codeType = TypeToCode(type); 

                Emit($"read {codeType}");
                Emit($"save {varName}");
            }
        }

        return null;
    }

    public override string VisitWriteStatement(PLCParser.WriteStatementContext context)
    {
        int exprCount = context.expression().Length;

        foreach (var expr in context.expression())
        {
            Visit(expr);
        }
      
        Emit($"print {exprCount}");

        return null;
    }

    public override string VisitIfStatement(PLCParser.IfStatementContext context)
    {
       
        Visit(context.expression());

        int labelElse = GetNewLabel();
        int labelEnd = GetNewLabel();

        Emit($"fjmp {labelElse}");

        Visit(context.statement(0));
        
        Emit($"jmp {labelEnd}");

        Emit($"label {labelElse}");
 
        if (context.statement().Length > 1)
        {
            Visit(context.statement(1));
        }

        Emit($"label {labelEnd}");

        return null;
    }

    public override string VisitWhileStatement(PLCParser.WhileStatementContext context)
    {
        int labelStart = GetNewLabel();
        int labelEnd = GetNewLabel();

        Emit($"label {labelStart}");

        Visit(context.expression());

        Emit($"fjmp {labelEnd}");

        Visit(context.statement());

        Emit($"jmp {labelStart}");

        Emit($"label {labelEnd}");

        return null;
    }

    public override string VisitForStatement(PLCParser.ForStatementContext context)
    {
        string varType = context.TYPE().GetText();
        string varName = context.ID().GetText();

        symbolTable[varName] = varType;

        Emit(varType switch
        {
            "int" => "push I 0",
            "float" => "push F 0.0",
            "bool" => "push B false",
            "string" => "push S \"\"",
            _ => throw new Exception("Unknown type")
        });

        Emit($"save {varName}");

        int labelStart = GetNewLabel();
        int labelEnd = GetNewLabel();

        Emit($"label {labelStart}");

        string condType = Visit(context.expression(0));
        Emit($"fjmp {labelEnd}");

        Visit(context.statement());

        Visit(context.expression(1));

        Emit($"jmp {labelStart}");
        Emit($"label {labelEnd}");

        return null;
    }


    // ------------------------------------------------------------------------
    // Výrazy
    // ------------------------------------------------------------------------

    public override string VisitPrimary(PLCParser.PrimaryContext context)
    {
        if (context.INT() != null)
        {
            string val = context.INT().GetText();

            Emit($"push I {val}");

            return "int";
        }
        else if (context.FLOAT() != null)
        {
            string val = context.FLOAT().GetText();

            Emit($"push F {val}");

            return "float";
        }
        else if (context.BOOL() != null)
        {
            string val = context.BOOL().GetText();

            Emit($"push B {val}");

            return "bool";
        }
        else if (context.STRING() != null)
        {
            string rawText = context.STRING().GetText();

            string val = rawText.Substring(1, rawText.Length - 2);

            Emit($"push S \"{val}\"");

            return "string";
        }
        else if (context.ID() != null)
        {
            string varName = context.ID().GetText();

            Emit($"load {varName}");

            symbolTable.TryGetValue(varName, out string varType);

            return varType;
        }
        else if (context.expression() != null)
        {
            return Visit(context.expression());
        }

        return "error";
    }

    public override string VisitAddition(PLCParser.AdditionContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string leftType = Visit(context.GetChild(0));
        string op = context.GetChild(1).GetText();
        string rightType = Visit(context.GetChild(2));

        if (op == ".")
        {
            Emit("concat");

            return "string";
        }

        bool floatResult = (leftType == "float" || rightType == "float");

        if (floatResult && rightType == "int")
        {
            Emit("itof");

        }

        if (floatResult && leftType == "int")
        {
            instructions.Insert(instructions.Count - 2, "itof");
        }

        string instr = (op == "+") ? "add" : "sub";

        instr += floatResult ? " F" : " I";

        Emit(instr);

        return floatResult ? "float" : "int";
    }


    public override string VisitMultiplication(PLCParser.MultiplicationContext ctx)
    {
        string resultType = Visit(ctx.unary(0));

        int count = ctx.unary().Length;

        for (int i = 1; i < count; i++)
        {
            int opIndex = 2 * i - 1;
            string op = ctx.GetChild(opIndex).GetText(); 

            string rightType = Visit(ctx.unary(i));

            bool floatResult = (resultType == "float" || rightType == "float");

            if (op == "%")
            {
      
                Emit("mod");
              
                resultType = "int";
            }
            else
            {
                if (floatResult && rightType == "int")
                {
                    Emit("itof");
                }

                string instr = (op == "*") ? "mul" : "div";

                instr += floatResult ? " F" : " I";

                Emit(instr);

                resultType = floatResult ? "float" : "int";
            }
        }

        return resultType;
    }


    public override string VisitComparison(PLCParser.ComparisonContext ctx)
    {
        if (ctx.ChildCount == 1)
            return Visit(ctx.GetChild(0));

        string leftType = Visit(ctx.GetChild(0));
        string op = ctx.GetChild(1).GetText();
        string rightType = Visit(ctx.GetChild(2));

        if (leftType == "int" && rightType == "float")
        {
            instructions.Insert(instructions.Count - 1, "itof");

            leftType = "float";
        }
        else if (leftType == "float" && rightType == "int")
        {
            Emit("itof");

            rightType = "float";
        }

        string instr = (op == "<") ? "lt" : "gt";
        instr += (leftType == "float" || rightType == "float") ? " F" : " I";

        Emit(instr);

        return "bool";
    }




    public override string VisitEquality(PLCParser.EqualityContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string leftType = Visit(context.GetChild(0));
        string op = context.GetChild(1).GetText(); 
        string rightType = Visit(context.GetChild(2));

        if (leftType == "float" && rightType == "int")
        {
            Emit("itof");

            rightType = "float";
        }

        string eqType = "I";

        if (leftType == "float" || rightType == "float") eqType = "F";

        else if (leftType == "string") eqType = "S";

        Emit($"eq {eqType}");

        if (op == "!=")
        {
            Emit("not");
        }

        return "bool";
    }


    public override string VisitExpressionStatement(PLCParser.ExpressionStatementContext context)
    {
        var exprCtx = context.expression();

        if (exprCtx.GetChild(0) is PLCParser.AssignmentContext)
        {
            Visit(exprCtx); 

            Emit("pop");
        }
        else
        {
            Visit(exprCtx);

        }

        return null;
    }


    public override string VisitLogic_and(PLCParser.Logic_andContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        Visit(context.GetChild(0));
        Visit(context.GetChild(2));

        Emit("and");

        return "bool";
    }

    public override string VisitLogic_or(PLCParser.Logic_orContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        Visit(context.GetChild(0));
        Visit(context.GetChild(2));

        Emit("or");

        return "bool";
    }

    public override string VisitUnary(PLCParser.UnaryContext context)
    {
        if (context.ChildCount == 1)
            return Visit(context.GetChild(0));

        string op = context.GetChild(0).GetText();
        string operandType = Visit(context.GetChild(1));

        if (op == "!")
        {
            Emit("not");

            return "bool";
        }
        else if (op == "-")
        {
            string instr = (operandType == "float") ? "uminus F" : "uminus I";

            Emit(instr);

            return operandType;
        }

        return "error";
    }

    /// <summary>
    /// Pomocná funkce pro převod klíčového slova typu do jednopísmenné značky pro read/push atd.
    /// </summary>
    private string TypeToCode(string type)
    {
        return type switch
        {
            "int" => "I",
            "float" => "F",
            "bool" => "B",
            "string" => "S",
            _ => "?"  // neznámý
        };
    }
}
