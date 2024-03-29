﻿using System.Text;

namespace BlazorApp.Pages;

public class StandardOutput : TextWriter
{
    public string Value { get; private set; } = "";

    public override void Write(char value) => Value += value;

    public override Encoding Encoding => Encoding.Default;
}

public partial class Test
{
    private StandardOutput standardOutput = new StandardOutput();

    private string TextArea_Text_Source { get; set; } = @"
(program MyProgram
    (entry MyModule Main)
    (module MyModule
        (function Main
            (args)
            (locals)
            (returns)
            (basicBlock __entry__
                (Dump 123)
                (LoadConstant (I64 0))
                (Exit)
            )
        )
    )
)".Trim();
    private string TextArea_Text_Output => standardOutput.Value;

    private void OnClick_Button_Run()
    {
        var interpreterConfig = new Komodo.Core.Interpretation.InterpreterConfig(standardOutput);
        Komodo.Core.Utilities.Logger.Callback = (level, log) => standardOutput.WriteLine(log);

        var result = Komodo.Core.Commands.RunIR(new Komodo.Core.Utilities.TextSource("source", TextArea_Text_Source), interpreterConfig);

        if (result != 0)
            Console.WriteLine($"Command returned code: {result}");
    }
}