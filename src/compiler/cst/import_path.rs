use serde::Serialize;

use crate::compiler::{
    lexer::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use super::Node;

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
