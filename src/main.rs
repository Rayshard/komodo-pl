use std::fs;

use colored::Colorize;
use escape_string::escape;
use komodo::{
    compiler::{lexer, parser},
    runtime::interpreter,
};

fn main() {
    let input = fs::read_to_string("tests/e2e/hello-world.kmd").expect("Unable to read file");
    
    // Lex
    let result = lexer::lex(&input);

    for token in result.tokens() {
        println!("{token:?} = {}", escape(token.value(&input)))
    }

    if result.has_errors() {
        for error in result.errors() {
            println!("{}", error.to_string().red())
        }

        return;
    }

    // Parse
    let result = match parser::parse_module(result.tokens()) {
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
