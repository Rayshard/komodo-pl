use serde::Serialize;

use crate::compiler::{
    cst::Node,
    lexer::token::Token,
    utilities::{location::Location, range::Range},
};

use super::Expression;

#[derive(Serialize, Clone)]
pub struct Parenthesized<'source> {
    open_parenthesis: Token<'source>,
    expression: Box<Expression<'source>>,
    close_parenthesis: Token<'source>,
    location: Location<'source>,
}

impl<'source> Parenthesized<'source> {
    pub fn new(
        open_parenthesis: Token<'source>,
        expression: Expression<'source>,
        close_parenthesis: Token<'source>,
    ) -> Self {
        let location = Location::new(
            expression.location().source(),
            Range::new(
                open_parenthesis.location().range().start(),
                close_parenthesis.location().range().end(),
            ),
        );

        Self {
            open_parenthesis,
            expression: Box::new(expression),
            close_parenthesis,
            location,
        }
    }

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
    fn location(&self) -> &Location<'source> {
        &self.location
    }
}
