import * as vscode from 'vscode';
import * as fs from 'fs';
import * as child_process from 'child_process';
import * as util from 'util';
import * as tmp from 'tmp';
import * as path from 'path';

const exec = util.promisify(child_process.exec);
const tmpFile = tmp.fileSync();

interface Settings {
	executablePath: string;
}

const DEFAULT_SETTINGS: Settings = {
	executablePath: ""
};

async function runCommand(executablePath: string, args: string[]) {
	const command = `${executablePath} ${args.join(" ")}`;
	console.log(`Running command: ${command}`);

	try {
		const { stdout, stderr } = await exec(command);

		if (stderr) {
			console.error(stderr);
		}

		return stdout;
	}
	catch (e: any) {
		console.error(`Command failed with code ${e.code}: ${e.cmd}`);
		console.error(e.stderr);
		return null;
	}
}

function isValidSettings(settings: Settings): boolean {
	if (!fs.statSync(settings.executablePath).isFile()) {
		console.error(`Invalid executable path set: ${settings.executablePath}`);
		return false;
	}

	return true;
}

export function activate(context: vscode.ExtensionContext) {
	const settings = vscode.workspace.getConfiguration('').get<Settings>('komodo', DEFAULT_SETTINGS);
	console.log(`Settings = ${JSON.stringify(settings, null, 4)}`);

	if (!isValidSettings(settings)) {
		return;
	}

	context.subscriptions.push(vscode.languages.registerDocumentFormattingEditProvider('komodo-lang', {
		async provideDocumentFormattingEdits(document: vscode.TextDocument): Promise<vscode.TextEdit[]> {
			const textRange = new vscode.Range(document.lineAt(0).range.start, document.lineAt(document.lineCount - 1).range.end);
			const extension = path.extname(document.fileName).substring(1);

			// We run formatter command on temp file instead of actual file so we can get actual
			// file contents whether or not the document is saved
			try {
				fs.writeFileSync(tmpFile.name, document.getText(textRange));

				const stdout = await runCommand(settings.executablePath, ["format", `--type=${extension}`, tmpFile.name]);
				return stdout ? [vscode.TextEdit.replace(textRange, stdout)] : [];
			} catch (e: any) {
				console.error(e);
				return [];
			}
		}
	}));
}

export function deactivate()
{
	tmpFile.removeCallback();
}
