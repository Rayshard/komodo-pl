"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.deactivate = exports.activate = void 0;
const vscode = require("vscode");
const fs = require("fs");
const child_process = require("child_process");
const util = require("util");
const tmp = require("tmp");
const path = require("path");
const exec = util.promisify(child_process.exec);
const tmpFile = tmp.fileSync();
const DEFAULT_SETTINGS = {
    executablePath: ""
};
async function runCommand(executablePath, args) {
    const command = `${executablePath} ${args.join(" ")}`;
    console.log(`Running command: ${command}`);
    try {
        const { stdout, stderr } = await exec(command);
        if (stderr) {
            console.error(stderr);
        }
        return stdout;
    }
    catch (e) {
        console.error(`Command failed with code ${e.code}: ${e.cmd}`);
        console.error(e.stderr);
        return null;
    }
}
function isValidSettings(settings) {
    if (!fs.statSync(settings.executablePath).isFile()) {
        console.error(`Invalid executable path set: ${settings.executablePath}`);
        return false;
    }
    return true;
}
function activate(context) {
    const settings = vscode.workspace.getConfiguration('').get('komodo', DEFAULT_SETTINGS);
    console.log(`Settings = ${JSON.stringify(settings, null, 4)}`);
    if (!isValidSettings(settings)) {
        return;
    }
    context.subscriptions.push(vscode.languages.registerDocumentFormattingEditProvider('komodo-lang', {
        async provideDocumentFormattingEdits(document) {
            const textRange = new vscode.Range(document.lineAt(0).range.start, document.lineAt(document.lineCount - 1).range.end);
            const extension = path.extname(document.fileName).substring(1);
            // We run formatter command on temp file instead of actual file so we can get actual
            // file contents whether or not the document is saved
            try {
                fs.writeFileSync(tmpFile.name, document.getText(textRange));
                const stdout = await runCommand(settings.executablePath, ["format", `--type=${extension}`, tmpFile.name]);
                return stdout ? [vscode.TextEdit.replace(textRange, stdout)] : [];
            }
            catch (e) {
                console.error(e);
                return [];
            }
        }
    }));
}
exports.activate = activate;
function deactivate() {
    tmpFile.removeCallback();
}
exports.deactivate = deactivate;
//# sourceMappingURL=extension.js.map