using Komodo.Utilities;

namespace Komodo.Compilation;

public static class Compiler
{
    public static Output? RunPass<Input, Output>(string name, Func<Input, Diagnostics?, Output?> Function, Input input, CompilationContext context, Diagnostics diagnostics) where Output : class
    {
        var stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();
        var output = Function(input, diagnostics);
        stopwatch.Stop();

        if (output == null || diagnostics.HasError)
        {
            diagnostics.Print(context.SourceMap);
            return null;
        }

        Logger.Info($"{name} pass finished in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");
        return output;
    }

    public static AST.Module? Compile(Dictionary<string, TextSource> sourceMap, string entry, bool printTokens = false, bool printCST = false)
    {
        // TODO: Lex files in parallel
        // TODO: Create a module dependency graph to check for conflicts
        // TODO: Typecheck modules from bottom up (i.e. modules with no depencies get typechecked first)

        var context = new CompilationContext(sourceMap, entry, printTokens, printCST);
        var diagnostics = new Diagnostics();

        var typecheckEnvironment = new Compilation.TypeSystem.Environment();
        var typecheckFunction = (CST.Module input, Diagnostics? diagnostics) => TypeChecker.TypeCheck(input, typecheckEnvironment, diagnostics);
        var operatorOverloads = new Compilation.TypeSystem.TSOperator[]
        {
            new Compilation.TypeSystem.TSOperator.BinaryAdd(new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64()),
            new Compilation.TypeSystem.TSOperator.BinarySubtract(new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64()),
            new Compilation.TypeSystem.TSOperator.BinaryMultipy(new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64()),
            new Compilation.TypeSystem.TSOperator.BinaryDivide(new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64(), new Compilation.TypeSystem.TSInt64()),
        };

        foreach (var overload in operatorOverloads)
            typecheckEnvironment.AddOperatorOverload(new Compilation.TypeSystem.Environment.OperatorOverload(overload, new TextLocation("std", 0, 0)), new TextLocation("std", 0, 0), diagnostics);

        if (!sourceMap.TryGetValue(entry, out var source))
        {
            //TODO: Add diagnostic
            throw new NotImplementedException();
        }

        // Lexer Pass
        var lexPass = RunPass("Lexing", Lexer.Lex, source, context, diagnostics);
        if (lexPass is null)
            return null;

        // Parser Pass
        var parsePass = RunPass("Parsing", Parser.ParseModule, lexPass, context, diagnostics);
        if (parsePass is null)
            return null;

        // Typecheck Pass
        var tcPass = RunPass("TypeChecking", typecheckFunction, parsePass, context, diagnostics);
        if (tcPass is null)
            return null;

        if (context.PrintTokens)
        {
            foreach (var token in lexPass)
                Console.WriteLine(token);
        }

        if (context.PrintCST)
            Console.WriteLine(JsonSerializer.Serialize(parsePass));

        diagnostics.Print(context.SourceMap);
        return tcPass;
    }
}
