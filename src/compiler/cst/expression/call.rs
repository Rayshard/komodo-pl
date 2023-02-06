use serde::Serialize;

use crate::compiler::{
    cst::Node,
    lexer::token::Token,
    utilities::{location::Location, range::Range},
};

use super::Expression;

#[derive(Serialize, Clone)]
pub struct Call<'source> {
    head: Box<Expression<'source>>,
    open_parenthesis: Token<'source>,
    args: Vec<Expression<'source>>,
    close_parenthesis: Token<'source>,
    location: Location<'source>,
}

impl<'source> Call<'source> {
    pub fn new(
        head: Expression<'source>,
        open_parenthesis: Token<'source>,
        args: Vec<Expression<'source>>,
        close_parenthesis: Token<'source>,
    ) -> Self {
        let location = Location::new(
            head.location().source(),
            Range::new(
                head.location().range().start(),
                close_parenthesis.location().range().end(),
            ),
        );

        Self {
            head: Box::new(head),
            open_parenthesis,
            args,
            close_parenthesis,
            location,
        }
    }

    pub fn head(&self) -> &Expression<'source> {
        self.head.as_ref()
    }

    pub fn open_parenthesis(&self) -> &Token<'source> {
        &self.open_parenthesis
    }

    pub fn args(&self) -> &[Expression<'source>] {
        &self.args
    }

    pub fn close_parenthesis(&self) -> &Token<'source> {
        &self.close_parenthesis
    }
}

impl<'source> Node<'source> for Call<'source> {
    fn location(&self) -> &Location<'source> {
        &self.location
    }
}
