use std::{fmt::Display, io};

use colored::Colorize;
use komodo::{
    compiler::{
        lexing::{
            lexer::{self, LexError},
            token::Token,
        },
        parsing::{
            cst::script::Script,
            parser::{self, ParseError},
        },
        utilities::text_source::TextSource,
    },
    runtime::interpreter,
};

enum CompilationError<'a> {
    File(io::Error),
    Lexer(Vec<LexError<'a>>),
    Parser(ParseError<'a>),
}

impl<'a> Display for CompilationError<'a> {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            CompilationError::File(error) => write!(
                f,
                "{}",
                format!("ERROR | Unable to load file: {error}").red()
            ),
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

type CompilationResult<'a, T> = Result<T, CompilationError<'a>>;

fn lex<'a>(source: &'a TextSource) -> CompilationResult<Vec<Token<'a>>> {
    let (tokens, errors) = lexer::lex(source);

    // for token in tokens.iter() {
    //     println!("{token:?} = {}", escape(token.value(&input)))
    // }

    if errors.is_empty() {
        Ok(tokens)
    } else {
        Err(CompilationError::Lexer(errors))
    }
}

fn parse<'a>(source: &'a TextSource, tokens: Vec<Token<'a>>) -> CompilationResult<'a, Script<'a>> {
    parser::parse_script(source, &tokens).map_or_else(
        |error| Err(CompilationError::Parser(error)),
        |script| Ok(script),
    )
}

fn compile<'a>(source: &'a TextSource) -> CompilationResult<'a, Script<'a>> {
    let tokens = lex(&source)?;
    parse(source, tokens)
}

fn main() {
    let source = match TextSource::from_file("tests/e2e/hello-world.kmd")
        .map_err(|error| CompilationError::File(error))
    {
        Ok(source) => source,
        Err(error) => {
            println!("{error}");
            std::process::exit(1);
        }
    };

    match compile(&source) {
        Ok(script) => {
            println!("{}", serde_yaml::to_string(&script).unwrap());
            let result = interpreter::interpret_script(&script);
            println!("{result:?}");
        }
        Err(error) => {
            println!("{error}");
            std::process::exit(1);
        }
    }
}
