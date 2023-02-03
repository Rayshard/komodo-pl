use serde::Serialize;

use crate::compiler::{
    lexing::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use super::{expression::Expression, Node};

#[derive(Debug, Serialize)]
pub enum ImportPath<'source> {
    Simple(Token<'source>),
    Complex {
        head: Box<ImportPath<'source>>,
        dot: Token<'source>,
        member: Token<'source>,
    },
}

impl<'source> Node<'source> for ImportPath<'source> {
    fn range(&self) -> Range {
        match self {
            ImportPath::Simple(token) => token.range().clone(),
            ImportPath::Complex {
                head,
                dot: _,
                member,
            } => Range::new(head.range().start(), member.range().end()),
        }
    }

    fn source(&self) -> &'source TextSource {
        match self {
            ImportPath::Simple(token) => token.source(),
            ImportPath::Complex {
                head,
                dot: _,
                member: _,
            } => head.source(),
        }
    }
}

#[derive(Debug, Serialize)]
pub enum Statement<'source> {
    Import {
        keyword_import: Token<'source>,
        import_path: ImportPath<'source>,
        from_path: Option<(Token<'source>, ImportPath<'source>)>,
        semicolon: Token<'source>,
    },
    Expression {
        expression: Expression<'source>,
        semicolon: Token<'source>,
    },
}

impl<'source> Node<'source> for Statement<'source> {
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

    fn source(&self) -> &'source TextSource {
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
