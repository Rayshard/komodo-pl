use regex::Regex;

struct Position {
    line : u64,
    column : u64
}

enum TokenType {
    IntLit(String),
    Plus,
    Minus,
    Asterisk,
    ForwardSlash
}

struct Token {
    ttype : TokenType,
    position : Position,
}

fn main() {
    // let args: Vec<String> =std::env::args().collect();

    // if args.len() != 2 {
    //     panic!("Expected 1 input file path!");
    // }

    // let input_file_path = &args[1];
    // let input = std::fs::read_to_string(input_file_path)
    //     .expect(&format!("Unable to open file: {}", input_file_path));

    let re = Regex::new(r"^a123").unwrap();
    let input = " a1234";
    let start = 1;
    assert!(re.is_match(&input[start..]));
}
