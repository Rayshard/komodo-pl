use std::fmt::Display;

use super::{
    lexer::LexError,
    parser::error::ParseError,
    typesystem::{context::ContextError, typechecker::result::TypecheckError},
};

pub enum CompilationError<'source> {
    Lexer(Vec<LexError<'source>>),
    Parser(ParseError<'source>),
    Typechecker(TypecheckError<'source>),
    Context(ContextError<'source>),
}

impl<'source> Display for CompilationError<'source> {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            CompilationError::Lexer(errors) => {
                let concated_errors = errors
                    .iter()
                    .map(|e| e.to_string())
                    .collect::<Vec<String>>()
                    .join("\n");

                write!(f, "{}", concated_errors)
            }
            CompilationError::Parser(error) => write!(f, "{}", error.to_string()),
            CompilationError::Typechecker(error) => write!(f, "{}", error.to_string()),
            CompilationError::Context(error) => write!(f, "{}", error.to_string()),
        }
    }
}

pub type CompilationResult<'source, T> = Result<T, CompilationError<'source>>;
