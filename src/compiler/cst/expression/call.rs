use serde::Serialize;

use crate::compiler::{lexer::token::Token, cst::Node, utilities::{range::Range, text_source::TextSource}};

use super::Expression;

#[derive(Serialize)]
pub struct Call<'source> {
    head: Box<Expression<'source>>,
    open_parenthesis: Token<'source>,
    args: Vec<Expression<'source>>,
    close_parenthesis: Token<'source>,
}

impl<'source> Node<'source> for Call<'source> {
    fn range(&self) -> Range {
        Range::new(self.head.range().start(), self.close_parenthesis.range().end())
    }

    fn source(&self) -> &'source TextSource {
        self.head.source()
    }
}
