use serde::Serialize;

use crate::compiler::lexing::token::Token;

use super::expression::Expression;

#[derive(Debug, Serialize)]
pub enum ImportPath<'a> {
    Simple(Token<'a>),
    Complex {
        head: Box<ImportPath<'a>>,
        dot: Token<'a>,
        member: Token<'a>,
    },
}

#[derive(Debug, Serialize)]
pub enum Statement<'a> {
    Import {
        keyword_import: Token<'a>,
        import_path: ImportPath<'a>,
        from_path: Option<(Token<'a>, ImportPath<'a>)>,
        semicolon: Token<'a>,
    },
    Expression {
        expression: Expression<'a>,
        semicolon: Token<'a>,
    },
}