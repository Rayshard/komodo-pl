using Komodo.Utilities;

namespace Komodo.Compilation;

public enum ErrorCode
{
    ParserExpectedToken,
    ParserUnexpectedToken,

    TSSymbolAleadyDefined,
    TSSymbolDoesNotExist,
    TSSymbolIsNotAVariable,
}

public record Error(ErrorCode Code, string Message, LineHint[]? LineHints, TextLocation Location) : Diagnostic(DiagnosticType.Error, Location, $"{Code}: {Message}", LineHints)
{
    public static Error ParserExpectedToken(CST.TokenType expected, CST.Token found)
    {
        var message = $"Expected {expected} but found {found.Type}({found.Value})";
        var lineHints = new LineHint[] { new LineHint(found.Location, $"expected {expected}") };

        return new Error(ErrorCode.ParserExpectedToken, message, lineHints, found.Location);
    }

    public static Diagnostic ParserUnexpectedToken(CST.Token token)
    {
        var message = $"Encountered unexpected token: {token.Type}({token.Value})";
        var lineHints = new LineHint[] { new LineHint(token.Location, "unexpected token") };

        return new Error(ErrorCode.ParserUnexpectedToken, message, lineHints, token.Location);
    }

    public static Error TSSymbolAlreadyDefined(string name, TextLocation initialDefinitionLocation, TextLocation attemptedDefinitionLocation)
    {
        var message = $"Symbol named '{name}' has already been defined!";
        var lineHints = new LineHint[]
        {
            new LineHint(initialDefinitionLocation, $"symbol was already defined here"),
            new LineHint(attemptedDefinitionLocation, $"cannot redefine symbol here"),
        };

        return new Error(ErrorCode.TSSymbolAleadyDefined, message, lineHints, attemptedDefinitionLocation);
    }

    public static Error TSSymbolDoesNotExist(string name, TextLocation location)
    {
        var message = $"Symbol named '{name}' does not exist!";
        var lineHints = new LineHint[] { new LineHint(location, $"symbol was never defined") };

        return new Error(ErrorCode.TSSymbolDoesNotExist, message, lineHints, location);
    }

    public static Error TSSymbolIsNotAVariable(string name, TextLocation symbolDefinitionLocation, TextLocation errorLocation)
    {
        var message = $"Symbol named '{name}' is not a variable!";
        var lineHints = new LineHint[] { new LineHint(symbolDefinitionLocation, $"symbol was defined here") };

        return new Error(ErrorCode.TSSymbolDoesNotExist, message, lineHints, errorLocation);
    }
}