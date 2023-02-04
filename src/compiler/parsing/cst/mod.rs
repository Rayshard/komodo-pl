use crate::compiler::{utilities::{range::Range, text_source::TextSource}, lexing::token::Token};

pub mod script;
pub mod statement;
pub mod import_path;
pub mod expression;
pub mod binary_operator;
pub mod unary_operator;

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