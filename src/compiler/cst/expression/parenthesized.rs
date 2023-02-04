use serde::Serialize;

use crate::compiler::{
    cst::Node,
    lexer::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use super::Expression;

#[derive(Serialize)]
pub struct Parenthesized<'source> {
    open_parenthesis: Token<'source>,
    expression: Box<Expression<'source>>,
    close_parenthesis: Token<'source>,
}

impl<'source> Parenthesized<'source> {
    pub fn open_parenthesis(&self) -> &Token<'source> {
        &self.open_parenthesis
    }

    pub fn expression(&self) -> &Expression<'source> {
        self.expression.as_ref()
    }

    pub fn close_parenthesis(&self) -> &Token<'source> {
        &self.close_parenthesis
    }
}

impl<'source> Node<'source> for Parenthesized<'source> {
    fn range(&self) -> Range {
        Range::new(
            self.open_parenthesis.range().start(),
            self.close_parenthesis.range().end(),
        )
    }

    fn source(&self) -> &'source TextSource {
        self.expression.source()
    }
}
