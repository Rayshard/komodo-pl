use std::{fmt::Display, io};

use colored::Colorize;
use komodo::{
    compiler::{
        cst::Script,
        lexing::{
            lexer::{self, LexError},
            token::Token,
        },
        parser::{self, ParseError},
        utilities::text_source::TextSource,
    },
    runtime::interpreter,
};

enum CompilationError {
    File(io::Error),
    Lexer(TextSource, Vec<LexError>),
    Parser(TextSource, ParseError),
}

impl Display for CompilationError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            CompilationError::File(error) => write!(
                f,
                "{}",
                format!("ERROR | Unable to load file: {error}").red()
            ),
            CompilationError::Lexer(source, errors) => {
                let concated_errors = errors
                    .iter()
                    .map(|e| {
                        format!(
                            "ERROR ({}) {}",
                            source.get_terminal_link(e.range.start()).unwrap(),
                            e.to_string()
                        )
                    })
                    .collect::<Vec<String>>()
                    .join("\n");

                write!(f, "{}", concated_errors.red())
            }
            CompilationError::Parser(source, error) => write!(
                f,
                "{}",
                format!(
                    "ERROR ({}) {}",
                    source.get_terminal_link(error.range.start()).unwrap(),
                    error.to_string()
                )
                .red()
            ),
        }
    }
}

type CompilationResult = Result<Script, CompilationError>;

fn lex<'a>(source: TextSource) -> Result<(TextSource, Vec<Token>), CompilationError> {
    let (tokens, errors) = lexer::lex(source.text());

    // for token in tokens.iter() {
    //     println!("{token:?} = {}", escape(token.value(&input)))
    // }

    if errors.is_empty() {
        Ok((source, tokens))
    } else {
        Err(CompilationError::Lexer(source, errors))
    }
}

fn parse((source, tokens): (TextSource, Vec<Token>)) -> Result<Script, CompilationError> {
    parser::parse_script(source.clone(), &tokens).map_or_else(
        |(error, _)| Err(CompilationError::Parser(source, error)),
        |(script, _)| Ok(script),
    )
}

fn compile(source: TextSource) -> CompilationResult {
    let lex_result = lex(source)?;
    parse(lex_result)
}

fn compile_file(path: &str) -> CompilationResult {
    let source = TextSource::from_file(path).map_err(|error| CompilationError::File(error))?;
    compile(source)
}

fn main() {
    match compile_file("tests/e2e/hello-world.kmd") {
        Ok(script) => {
            println!("{script:#?}");
            let result = interpreter::interpret_script(&script);
            println!("{result:?}");
        }
        Err(error) => {
            println!("{error}");
            std::process::exit(1);
        }
    }
}
