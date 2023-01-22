use compiler::{lexer, parser};

fn main() {
    let result = lexer::lex("(5-123) + 456 * 6");

    if result.has_errors() {
        for error in result.errors() {
            println!("{error:?}")
        }

        return;
    }

    let result = match parser::parse_module(result.tokens()) {
        Ok((module, _)) => module,
        Err((error, state)) => {
            println!("{}", error.message);
            return;
        }
    };
    
    for element in result.elements() {
        println!("{element:?}")
    }
}
