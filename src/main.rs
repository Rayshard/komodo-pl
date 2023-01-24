use komodo::{
    compiler::{lexer, parser},
    runtime::interpreter,
};

fn main() {
    let result = lexer::lex("5; 7; 9+3;");

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

    println!("{:?}", interpreter::interpret_module(&result));
}
