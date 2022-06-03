#[macro_use]
extern crate lazy_static;

mod lexer;
mod parser;
mod utils;
mod ast;

use utils::Error;

fn main() {
    let args: Vec<String> =std::env::args().collect();

    if args.len() != 2 {
        panic!("Expected 1 input file path!");
    }

    let input_file_path = args[1].clone();
    let input = std::fs::read_to_string(&input_file_path)
        .expect(&format!("Unable to open file: {}", &input_file_path));

    let tokens = lexer::lex(&input, &input_file_path);

    match parser::parse(tokens) {
        Ok(node) => println!("{:#?}", node),
        Err(e) => println!("{}", e)
    };
    
}
