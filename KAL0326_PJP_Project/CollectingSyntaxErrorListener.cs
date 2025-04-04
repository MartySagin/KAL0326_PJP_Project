using Antlr4.Runtime;
using System.Collections.Generic;
using System.IO;

public class CollectingSyntaxErrorListener : IAntlrErrorListener<IToken>
{
    public List<string> Errors { get; } = new();

    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {

        string error = $"Syntax error at line {line}:{charPositionInLine} – {msg}";

        Errors.Add(error);
    }
}
