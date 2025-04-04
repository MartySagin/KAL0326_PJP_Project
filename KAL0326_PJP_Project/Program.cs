using Antlr4.Runtime;
using System;
using System.IO;

namespace KAL0326_PJP_Project
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Zadej cestu ke vstupnímu souboru:");
            string? inputPath = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
            {
                Console.WriteLine("❌ Soubor neexistuje.");
                return;
            }

            string inputCode = File.ReadAllText(inputPath);

            // Lexer a parser
            AntlrInputStream inputStream = new AntlrInputStream(inputCode);
            PLCLexer lexer = new PLCLexer(inputStream);
            CommonTokenStream tokenStream = new CommonTokenStream(lexer);
            PLCParser parser = new PLCParser(tokenStream);

            // Zachytávání syntax chyb
            var errorListener = new CollectingSyntaxErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            // Parsování programu
            var tree = parser.program();

            if (errorListener.Errors.Count > 0)
            {
                Console.WriteLine("Nalezeny syntaktické chyby:");
                foreach (var err in errorListener.Errors)
                {
                    Console.WriteLine(err);
                }
                return;
            }

            Console.WriteLine("Program je syntakticky správný.");

            var typeChecker = new TypeChecker();
            typeChecker.Visit(tree);

            if (typeChecker.Errors.Count > 0)
            {
                Console.WriteLine("Nalezeny typové chyby:");
                foreach (var err in typeChecker.Errors)
                {
                    Console.WriteLine(err);
                }
                return;
            }

            Console.WriteLine("Program prošel typovou kontrolou.");
        }
    }
}
