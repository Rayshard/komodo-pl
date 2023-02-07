use serde::Serialize;

use crate::compiler::{cst::Node, lexer::token::Token, utilities::location::Location};

#[derive(Serialize, Clone)]
pub struct Identifier<'source> {
    value: &'source str,
    location: Location<'source>,
}

impl<'source> Identifier<'source> {
    pub fn new(token: Token<'source>) -> Self {
        Self {
            value: token.value(),
            location: token.location().clone(),
        }
    }

    pub fn value(&self) -> &'source str {
        self.value
    }
}

impl<'source> Node<'source> for Identifier<'source> {
    fn location(&self) -> Location<'source> {
        self.location.clone()
    }
}
