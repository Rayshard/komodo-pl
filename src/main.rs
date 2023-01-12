use compiler::lexer;

fn main() {
    let lex_result =lexer::lex("123.4");

    for token in lex_result.tokens() {
        println!("{token:?}")
    }

    for error in lex_result.errors() {
        println!("{error:?}")
    }
}
