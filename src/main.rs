use std::fmt::Display;

use colored::Colorize;
use komodo::{
    compiler::{
        cst::Module,
        lexing::{
            lexer::{self, LexError},
            token::Token,
        },
        parser::{self, ParseError},
        utilities::text_source::TextSource,
    },
    runtime::interpreter,
};

enum CompilationError<'a> {
    Lexer(&'a TextSource, Vec<LexError>),
    Parser(&'a TextSource, ParseError),
}

impl<'a> Display for CompilationError<'a> {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
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

fn lex(source: &TextSource) -> Result<(&TextSource, Vec<Token>), CompilationError> {
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

fn parse<'a>(
    (source, tokens): (&'a TextSource, &'a Vec<Token>),
) -> Result<Module, CompilationError> {
    parser::parse_module(&tokens).map_or_else(
        |(error, _)| Err(CompilationError::Parser(source, error)),
        |(module, _)| Ok(module),
    )
}

fn main() {
    let text_source = match TextSource::from_file("tests/e2e/hello-world.kmd") {
        Ok(ts) => ts,
        Err(error) => {
            println!(
                "{}",
                format!("ERROR | Unable to load file: {}", error.to_string()).red()
            );
            return;
        }
    };

    // Lex
    let result = match lex(&text_source) {
        Ok(result) => result,
        Err(error) => {
            println!("{error}");
            return;
        }
    };

    // Parse
    let result = match parse((result.0, &result.1)) {
        Ok(module) => module,
        Err(error) => {
            println!("{error}");
            return;
        }
    };

    // Interpret
    let result = interpreter::interpret_module(&result);
    println!("{result:?}");
}
