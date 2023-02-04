use crate::compiler::{utilities::{range::Range, text_source::TextSource}, lexer::token::Token};

pub mod script;
pub mod expression;
pub mod statement;

pub trait Node<'source> {
    fn range(&self) -> Range;
    fn source(&self) -> &'source TextSource;
}

impl<'source> Node<'source> for Token<'source> {
    fn range(&self) -> Range {
        self.range().clone()
    }

    fn source(&self) -> &'source TextSource {
        self.source()
    }
}