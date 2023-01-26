use std::fs;

use colored::Colorize;
use escape_string::escape;
use komodo::{compiler::{parser, lexing::lexer}, runtime::interpreter};

fn main() {
    let input = fs::read_to_string("tests/e2e/hello-world.kmd").expect("Unable to read file");

    // Lex
    let (tokens, errors) = lexer::lex(&input);

    for token in tokens.iter() {
        println!("{token:?} = {}", escape(token.value(&input)))
    }

    if !errors.is_empty() {
        for error in errors {
            println!("{}", error.to_string().red())
        }

        return;
    }

    // Parse
    let result = match parser::parse_module(&tokens) {
        Ok((module, _)) => module,
        Err((error, _)) => {
            println!("{}", error.to_string().red());
            return;
        }
    };

    // Interpret
    let result = interpreter::interpret_module(&result);
    println!("{result:?}");
}
