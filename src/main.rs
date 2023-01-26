use std::{fmt::Display, fs};

use colored::Colorize;
use escape_string::escape;
use komodo::{
    compiler::{
        cst::Module,
        lexing::{
            lexer::{self, LexError},
            token::Token,
        },
        parser::{self, ParseError},
    },
    runtime::interpreter,
};

enum CompilationError {
    Lexer(Vec<LexError>),
    Parser(ParseError),
}

impl Display for CompilationError {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            CompilationError::Lexer(errors) => {
                let concated_errors = errors
                    .iter()
                    .map(|e| e.to_string())
                    .collect::<Vec<String>>()
                    .join("\n");

                write!(f, "{}", concated_errors.red())
            }
            CompilationError::Parser(error) => write!(f, "{}", error.to_string().red()),
        }
    }
}

fn lex(input: &str) -> Result<Vec<Token>, CompilationError> {
    let (tokens, errors) = lexer::lex(&input);

    // for token in tokens.iter() {
    //     println!("{token:?} = {}", escape(token.value(&input)))
    // }

    if errors.is_empty() {
        Ok(tokens)
    } else {
        Err(CompilationError::Lexer(errors))
    }
}

fn parse(input: &Vec<Token>) -> Result<Module, CompilationError> {
    parser::parse_module(input).map_or_else(
        |(error, _)| Err(CompilationError::Parser(error)),
        |(module, _)| Ok(module),
    )
}

fn main() {
    let result = fs::read_to_string("tests/e2e/hello-world.kmd").expect("Unable to read file");

    // Lex
    let result = match lex(&result) {
        Ok(tokens) => tokens,
        Err(error) => {
            println!("{error}");
            return;
        }
    };

    // Parse
    let result = match parse(&result) {
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
