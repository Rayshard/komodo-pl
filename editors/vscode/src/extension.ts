// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import * as child_process from 'child_process';

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
	const workbenchConfig = vscode.workspace.getConfiguration('workbench')
	console.log(workbenchConfig);

	let command1 = vscode.languages.registerDocumentFormattingEditProvider('komodo-lang', {
        provideDocumentFormattingEdits(document: vscode.TextDocument): vscode.TextEdit[] {
			const textRange = new vscode.Range(document.lineAt(0).range.start, document.lineAt(document.lineCount - 1).range.end);
			const text = document.getText();

			// child_process.exec("ls -la", (error, stdout, stderr) => {
			// 	if (error) {
			// 		console.log(`error: ${error.message}`);
			// 		return;
			// 	}
			// 	if (stderr) {
			// 		console.log(`stderr: ${stderr}`);
			// 		return;
			// 	}
			// 	console.log(`stdout: ${stdout}`);
			// });

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

// this method is called when your extension is deactivated
export function deactivate() {}
