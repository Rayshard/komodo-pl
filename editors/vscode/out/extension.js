"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.deactivate = exports.activate = void 0;
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
const vscode = require("vscode");
// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
function activate(context) {
    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    console.log('Congratulations, your extension "komodo-formatter" is now active!');
    let command1 = vscode.languages.registerDocumentFormattingEditProvider('komodo-lang', {
        provideDocumentFormattingEdits(document) {
            const textRange = new vscode.Range(document.lineAt(0).range.start, document.lineAt(document.lineCount - 1).range.end);
            const text = document.getText();
            return [vscode.TextEdit.replace(textRange, text + "\nThis is appended")];
        }
    });
    // The command has been defined in the package.json file
    // Now provide the implementation of the command with registerCommand
    // The commandId parameter must match the command field in package.json
    let command2 = vscode.commands.registerCommand('komodo-formatter.helloWorld', () => {
        // The code you place here will be executed every time your command is executed
        // Display a message box to the user
        vscode.window.showInformationMessage('Hello World from KomodoFormatter!');
    });
    context.subscriptions.push(command1, command2);
}
exports.activate = activate;
// this method is called when your extension is deactivated
function deactivate() { }
exports.deactivate = deactivate;
//# sourceMappingURL=extension.js.map