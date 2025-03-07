using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Language
{
    public class Variable
    {
        public string type { get; set; }
        public string value { get; set; }
        public string name { get; set; }
    }
    public class Function
    {
        public string name { get; set; }
        public bool isMain { get; set; }
        public bool isRecursive { get; set; }
        public string returnType { get; set; }
        public List<string> parameters { get; set; }
        public List<Variable> localVariables { get; set; }
        public List<string> controlBlocks { get; set; }
    }
    public class GrammarVisitor : myGrammarBaseVisitor<object>
    {

        private readonly List<(string Type, string Name, string Value)> globalVariables = new List<(string Type, string Name, string Value)>();
        private List<Function> functions = new List<Function>();
        public override object VisitProgram([NotNull] myGrammarParser.ProgramContext context)
        {
            return Visit(context);
        }
        public override object VisitGlobalVariable([NotNull] myGrammarParser.GlobalVariableContext context)
        {
            string type = context.dataType().GetText();
            string name = context.ID().GetText();
            string value = context.constant().GetText();

            globalVariables.Add((type, name, value));

            return null;
        }
        public List<string> ExtractParameters(myGrammarParser.ParameterListContext context)
        {
            if (context == null) return new List<string>();
            var parameters = new List<string>();
            foreach (var parameter in context.parameter())
            {
                parameters.Add($"{parameter.dataType().GetText()} {parameter.ID().GetText()}");
            }
            return parameters;
        }

        public override object VisitControlBlock(myGrammarParser.ControlBlockContext context)
        {
            if (context.ifBlock() != null)
            {
                return $"<if, {context.ifBlock().Start.Line}>";
            }
            if (context.forBlock() != null)
            {
                return $"<for, {context.forBlock().Start.Line}>";
            }
            if (context.whileBlock() != null)
            {
                return $"<while, {context.whileBlock().Start.Line}>";
            }
            if (context.ifElseBlock() != null)
            {
                return $"<if...else, {context.ifElseBlock().Start.Line}>";
            }
            return null ;
        }

        private void VisitExpression(myGrammarParser.ExpressionContext context, Function function)
        {
            if (context.variableDeclaration() != null)
            {
                var varDecl = context.variableDeclaration();
                foreach (var varContext in varDecl.variable())
                {
                    var variable = new Variable
                    {
                        type = varDecl.dataType().GetText(),
                        name = varContext.GetText(),
                        value = varDecl.constant()?.GetText() ?? "null"
                    };
                    function.localVariables.Add(variable);
                }
            }
            else if (context.controlBlock() != null)
            {
                var controlBlock = VisitControlBlock(context.controlBlock())?.ToString();
                if (controlBlock != null)
                {
                    function.controlBlocks.Add(controlBlock);
                }
            }
        }
        public override object VisitFunction([NotNull] myGrammarParser.FunctionContext context)
        {
            Function function = new Function()
            {
                parameters = new List<string>(),
                localVariables = new List<Variable>(),
                controlBlocks = new List<string>()
            };

            if (context.mainFunction() != null)
            {
                function.name = "main";
                function.isMain = true;
                function.isRecursive = false;
                function.returnType = context.mainFunction().INT()?.GetText() ?? context.mainFunction().VOID()?.GetText(); // int otherwise void!

                foreach(var expr in context.mainFunction().expression())
                {
                    VisitExpression(expr, function);
                }
            }
            else
            {
                function.name = context.ID().GetText();
                function.isMain = false;
                function.returnType = context.returnType().GetText();
                function.parameters = ExtractParameters(context.parameterList());

                bool isRecursive = false;

                foreach(var expr in context.expression())
                {
                    if(Regex.Matches(context.GetText(), context.ID().GetText()).Count > 1)
                    {
                        isRecursive = true;
                    }

                    VisitExpression(expr, function);
                }
                function.isRecursive = isRecursive;
            }
            
            functions.Add(function);
            return null;
        }

        public void WriteGlobalVariablesToFile(string path)
        {
            using (StreamWriter output = new StreamWriter(path))
            {
                foreach (var variable in globalVariables)
                {
                    output.WriteLine($"Variable: {variable.Name} Value: {variable.Value} Type: {variable.Type}");
                }
            }
        }

        public void WriteFunctionsToFile(string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                foreach (var function in functions)
                {
                    writer.WriteLine($"Name: {function.name}");
                    writer.WriteLine($"Type: {(function.isMain ? "Main" : "Regular")}, {(function.isRecursive ? "Recursive" : "Non-recursive")}");
                    writer.WriteLine($"Return Type: {function.returnType}");
                    writer.WriteLine($"Parameters: {(function.parameters.Any() ? string.Join(", ", function.parameters) : "None")}");

                    writer.WriteLine("Local Variables:");
                    if (function.localVariables.Any())
                    {
                        foreach (var variable in function.localVariables)
                        {
                            writer.WriteLine($"\t{variable.type} {variable.name} = {variable.value}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("\tNone");
                    }
                    writer.WriteLine("Control Structures:");
                    if (function.controlBlocks.Any())
                    {
                        foreach (var control in function.controlBlocks)
                        {
                            writer.WriteLine($"\t{control}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("\tNone");
                    }
                    writer.WriteLine();
                }
            }
        }
    }
    public class Program
    {
        private static void WriteLexemes(myGrammarLexer lexer, string path)
        {
            IEnumerable<IToken> tokens = lexer.GetAllTokens();
            lexer.Reset();

            using (StreamWriter streamWriter = new StreamWriter(path))
            {
                foreach (var token in tokens)
                {
                    string lexeme = myGrammarLexer.DefaultVocabulary.GetSymbolicName(token.Type);
                    streamWriter.WriteLine($"<{lexeme}, '{token.Text}', {token.Line}>");
                }
            }
        }
        private static myGrammarLexer SetupLexer(string text)
        {
            AntlrInputStream inputStream = new AntlrInputStream(text);
            myGrammarLexer expressionLexer = new myGrammarLexer(inputStream);
            return expressionLexer;
        }

        private static myGrammarParser SetupParser(myGrammarLexer lexer)
        {
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            myGrammarParser parser = new myGrammarParser(tokens);
            return parser;
        }

        private static void DetectLexicalErrors(myGrammarLexer lexer)
        {
            IEnumerable<IToken> tokens = lexer.GetAllTokens();
            lexer.Reset();

            foreach (var token in tokens) 
            {
                if(token.Type == myGrammarLexer.ERROR)
                {
                    Console.WriteLine($"Lexical error detected: {token.Text} at line {token.Line}");
                }
            }
        }
        public static void Main()
        {
            string input = File.ReadAllText("../../Input.txt");
            string globalVariablesOutput = "../../OutputGlobalVariables.txt";
            string lexemesOutput = "../../Lexemes.txt";
            string functionsOutput = "../../Functions.txt";

            myGrammarLexer lexer = SetupLexer(input);
            myGrammarParser parser = SetupParser(lexer);

            DetectLexicalErrors(lexer);

            WriteLexemes(lexer, lexemesOutput);

            var programContext = parser.program();
            var globalVariableContext = programContext.globalVariable();
            var functionContext = programContext.function();

            GrammarVisitor visitor = new GrammarVisitor();

            foreach (var globalVar in globalVariableContext)
            {
                visitor.VisitGlobalVariable(globalVar);
            }

            visitor.WriteGlobalVariablesToFile(globalVariablesOutput);

            foreach(var function in functionContext)
            {
                visitor.VisitFunction(function);
            }

            visitor.WriteFunctionsToFile(functionsOutput);
        }
    }
}
