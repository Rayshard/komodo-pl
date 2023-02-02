use serde::Serialize;

use crate::compiler::{
    lexing::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use super::{expression::Expression, Node};

#[derive(Debug, Serialize)]
pub enum ImportPath<'a> {
    Simple(Token<'a>),
    Complex {
        head: Box<ImportPath<'a>>,
        dot: Token<'a>,
        member: Token<'a>,
    },
}

impl<'a> Node<'a> for ImportPath<'a> {
    fn range(&self) -> Range {
        match self {
            ImportPath::Simple(token) => token.range().clone(),
            ImportPath::Complex { head, dot, member } => {
                Range::new(head.range().start(), member.range().end())
            }
        }
    }

    fn source(&self) -> &'a TextSource {
        match self {
            ImportPath::Simple(token) => token.source(),
            ImportPath::Complex { head, dot, member } => head.source(),
        }
    }
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

impl<'a> Node<'a> for Statement<'a> {
    fn range(&self) -> Range {
        match self {
            Statement::Import {
                keyword_import,
                import_path: _,
                from_path: _,
                semicolon,
            } => Range::new(keyword_import.range().start(), semicolon.range().end()),
            Statement::Expression {
                expression,
                semicolon,
            } => Range::new(expression.range().start(), semicolon.range().end()),
        }
    }

    fn source(&self) -> &'a TextSource {
        match self {
            Statement::Import {
                keyword_import,
                import_path: _,
                from_path: _,
                semicolon: _,
            } => keyword_import.source(),
            Statement::Expression {
                expression,
                semicolon: _,
            } => expression.source(),
        }
    }
}
