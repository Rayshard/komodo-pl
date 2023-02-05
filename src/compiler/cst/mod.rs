use super::{lexer::token::Token, utilities::location::Location};

pub mod expression;
pub mod script;
pub mod statement;

pub trait Node<'source> {
    fn location(&self) -> &Location<'source>;
}

impl<'source> Node<'source> for Token<'source> {
    fn location(&self) -> &Location<'source> {
        self.location()
    }
}
