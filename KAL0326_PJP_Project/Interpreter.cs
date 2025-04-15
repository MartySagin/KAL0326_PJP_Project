using System;
using System.Collections.Generic;
using System.IO;

public class Interpreter
{
    private readonly List<string> instructions = new();
    private readonly Dictionary<string, object> variables = new();
    private readonly Dictionary<int, int> labels = new();  // label -> line index
    private readonly Stack<object> stack = new();

    public void Run(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        instructions.AddRange(lines);

        PreprocessLabels();

        int ip = 0;  

        while (ip >= 0 && ip < instructions.Count)
        {
            string line = instructions[ip].Trim();
            if (string.IsNullOrEmpty(line))
            {
                ip++;
                continue;
            }

            bool jumped = ExecuteInstruction(line, ref ip);

            if (!jumped)
            {
                ip++;
            }
        }
    }

    /// <summary>
    /// Najde všechny řádky typu "label N" a uloží do slovníku labels[N] = lineIndex
    /// </summary>
    private void PreprocessLabels()
    {
        for (int i = 0; i < instructions.Count; i++)
        {
            string line = instructions[i].Trim();
            if (line.StartsWith("label "))
            {
                var parts = line.Split(new char[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2 && int.TryParse(parts[1], out int lbl))
                {
                    labels[lbl] = i;
                }
            }
        }
    }

    /// <summary>
    /// Provede jednu instrukci (příkaz) a případně upraví ip (pokud jde o skok).
    /// Vrátí true, pokud se provedl skok (tj. volající nesmí dělat ip++).
    /// </summary>
    private bool ExecuteInstruction(string line, ref int ip)
    {

        var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return false;

        string instr = parts[0];

        switch (instr)
        {
            case "label":
                return false;

            case "jmp":
                if (parts.Length > 1 && int.TryParse(parts[1], out int jmpLabel))
                {
                    if (labels.TryGetValue(jmpLabel, out int lineIndex))
                    {
                        ip = lineIndex;

                        return true; 
                    }
                }

                return false;

            case "fjmp":

                if (parts.Length > 1 && int.TryParse(parts[1], out int fjmpLabel))
                {
                    bool cond = PopBool();
                    if (cond == false)
                    {
                        if (labels.TryGetValue(fjmpLabel, out int lineIndex))
                        {
                            ip = lineIndex;
                            return true;
                        }
                    }
                }

                return false;

            case "push":
                if (parts.Length < 3) return false;
                string pushType = parts[1];
                string rawValue = line.Substring(line.IndexOf(pushType) + pushType.Length).Trim();
               
                string val = "";

                if (pushType == "S")
                {
       
                    val = line.Substring(line.IndexOf(pushType) + 1).Trim();

                    int firstQuote = val.IndexOf('"');
                    int lastQuote = val.LastIndexOf('"');

                    if (firstQuote >= 0 && lastQuote > firstQuote)
                    {
                        val = val.Substring(firstQuote + 1, lastQuote - (firstQuote + 1));
                    }

                    stack.Push(val);
                }
                else
                {
                    string rawVal = parts[2];

                    if (pushType == "I")
                    {
                        if (int.TryParse(rawVal, out int iVal))
                            stack.Push(iVal);
                        else
                            stack.Push(0);
                    }
                    else if (pushType == "F")
                    {
                        if (double.TryParse(rawVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                            stack.Push(dVal);
                        else
                            stack.Push(0.0);
                    }
                    else if (pushType == "B")
                    {
                        bool bVal = (rawVal == "true");

                        stack.Push(bVal);
                    }
                }

                return false;

            case "pop":
                if (stack.Count > 0)
                    stack.Pop();

                return false;

            case "load":
                if (parts.Length > 1)
                {
                    string varName = parts[1];

                    if (variables.TryGetValue(varName, out object value))
                    {
                        stack.Push(value);
                    }
                    else
                    {
                        stack.Push(0);
                    }
                }
                return false;

            case "save":
                if (parts.Length > 1)
                {
                    string varName = parts[1];

                    object top = stack.Pop();

                    variables[varName] = top;
                }

                return false;

            case "add":
                {
                    if (parts.Length < 2) return false;

                    string t = parts[1]; 

                    object right = stack.Pop();
                    object left = stack.Pop();

                    if (t == "I")
                    {
                        int a = Convert.ToInt32(left);
                        int b = Convert.ToInt32(right);

                        stack.Push(a + b);
                    }
                    else
                    {
                        double a = Convert.ToDouble(left);
                        double b = Convert.ToDouble(right);

                        stack.Push(a + b);
                    }

                    return false;
                }

            case "sub":
                {
                    string t = parts[1];

                    object right = stack.Pop();
                    object left = stack.Pop();

                    if (t == "I")
                    {
                        int a = Convert.ToInt32(left);
                        int b = Convert.ToInt32(right);

                        stack.Push(a - b);
                    }
                    else
                    {
                        double a = Convert.ToDouble(left);
                        double b = Convert.ToDouble(right);

                        stack.Push(a - b);
                    }

                    return false;
                }

            case "mul":
                {
                    string t = parts[1];

                    object right = stack.Pop();
                    object left = stack.Pop();

                    if (t == "I")
                    {
                        int a = Convert.ToInt32(left);
                        int b = Convert.ToInt32(right);

                        stack.Push(a * b);
                    }
                    else
                    {
                        double a = Convert.ToDouble(left);
                        double b = Convert.ToDouble(right);

                        stack.Push(a * b);
                    }

                    return false;
                }

            case "div":
                {
                    string t = parts[1];

                    object right = stack.Pop();
                    object left = stack.Pop();

                    if (t == "I")
                    {
                        int a = Convert.ToInt32(left);
                        int b = Convert.ToInt32(right);

                        stack.Push(a / b); 
                    }
                    else
                    {
                        double a = Convert.ToDouble(left);
                        double b = Convert.ToDouble(right);

                        stack.Push(a / b);
                    }
                    return false;
                }

            case "mod":
                {
                    int r = Convert.ToInt32(stack.Pop());
                    int l = Convert.ToInt32(stack.Pop());

                    stack.Push(l % r);

                    return false;
                }

            case "uminus":
                {
                    if (parts.Length < 2) return false;

                    string t = parts[1];

                    object value = stack.Pop();

                    if (t == "I")
                    {
                        int x = Convert.ToInt32(value);
                        stack.Push(-x);
                    }
                    else
                    {
                        double x = Convert.ToDouble(value);
                        stack.Push(-x);
                    }

                    return false;
                }

            case "concat":
                {
                    string r = Convert.ToString(stack.Pop());
                    string l = Convert.ToString(stack.Pop());

                    stack.Push(l + r);

                    return false;
                }

            case "and":
                {
                    bool r = PopBool();
                    bool l = PopBool();

                    stack.Push(l && r);

                    return false;
                }

            case "or":
                {
                    bool r = PopBool();
                    bool l = PopBool();

                    stack.Push(l || r);

                    return false;
                }

            case "gt":
                {
                    string t = parts[1];

                    object r = stack.Pop();
                    object l = stack.Pop();

                    if (t == "I")
                    {
                        int a = Convert.ToInt32(l);
                        int b = Convert.ToInt32(r);

                        stack.Push(a > b);
                    }
                    else
                    {
                        double a = Convert.ToDouble(l);
                        double b = Convert.ToDouble(r);

                        stack.Push(a > b);
                    }

                    return false;
                }

            case "lt":
                {
                    string t = parts[1];

                    object r = stack.Pop();
                    object l = stack.Pop();

                    if (t == "I")
                    {
                        int a = Convert.ToInt32(l);
                        int b = Convert.ToInt32(r);

                        stack.Push(a < b);
                    }
                    else
                    {
                        double a = Convert.ToDouble(l);
                        double b = Convert.ToDouble(r);

                        stack.Push(a < b);
                    }

                    return false;
                }

            case "eq":
                {
                    string t = parts[1];

                    object r = stack.Pop();
                    object l = stack.Pop();

                    bool result;

                    if (t == "I")
                    {
                        result = (Convert.ToInt32(l) == Convert.ToInt32(r));
                    }
                    else if (t == "F")
                    {
                        result = (Convert.ToDouble(l) == Convert.ToDouble(r));
                    }
                    else if (t == "S")
                    {
                        result = (Convert.ToString(l) == Convert.ToString(r));
                    }
                    else
                    {
                        result = l.Equals(r);
                    }

                    stack.Push(result);

                    return false;
                }

            case "not":
                {
                    bool value = PopBool();

                    stack.Push(!value);

                    return false;
                }

            case "itof":
                {
                    int value = Convert.ToInt32(stack.Pop());

                    stack.Push((double)value);

                    return false;
                }

            case "print":
                {
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int n))
                    {
                        object[] vals = new object[n];

                        for (int i = n - 1; i >= 0; i--)
                        {
                            vals[i] = stack.Pop();
                        }

                        for (int i = 0; i < n; i++)
                        {
                            object value = vals[i];

                            if (value is double d)
                            {
                                Console.Write(d.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                Console.Write(value);
                            }
                        }

                        Console.WriteLine(); 
                    }

                    return false;
                }

            case "read":
                {
                
                    if (parts.Length < 2) return false;

                    string t = parts[1];

                    string input = Console.ReadLine()?.Trim() ?? "";

                    switch (t)
                    {
                        case "I":
                            if (int.TryParse(input, out int iVal))
                                stack.Push(iVal);
                            else
                                stack.Push(0);
                            break;
                        case "F":
                            if (double.TryParse(input, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                                stack.Push(dVal);
                            else
                                stack.Push(0.0);
                            break;
                        case "S":
                            stack.Push(input);
                            break;
                        case "B":
                            bool bval = (input.ToLower() == "true");
                            stack.Push(bval);
                            break;
                    }
                    return false;
                }

            default:
                return false;
        }
    }

    /// <summary>
    /// Pomocná funkce, popne bool z vrcholu stacku.
    /// </summary>
    private bool PopBool()
    {
        if (stack.Count == 0) return false;

        var top = stack.Pop();

        return Convert.ToBoolean(top);
    }
}
