#[macro_use]
extern crate lazy_static;

mod lexer;
mod parser;
mod utils;
mod ast;

fn main() {
    let args: Vec<String> =std::env::args().collect();

    if args.len() != 2 {
        panic!("Expected 1 input file path!");
    }

    let input_file_path = &args[1];
    let input = std::fs::read_to_string(input_file_path)
        .expect(&format!("Unable to open file: {}", input_file_path));

    let tokens = lexer::lex(&input);
    let node  = parser::parse(tokens);

    println!("{:#?}", node);
}
