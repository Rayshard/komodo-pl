use crate::compiler::utilities::{range::Range, text_source::TextSource};

pub mod script;
pub mod statement;
pub mod expression;
pub mod binary_operator;
pub mod unary_operator;

pub trait Node<'source> {
    fn range(&self) -> Range;
    fn source(&self) -> &'source TextSource;
}