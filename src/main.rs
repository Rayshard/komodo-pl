use compiler::lexer;

fn main() {
    for token in lexer::lex("123 + 456") {
        println!("{token:?}")
    }
}
