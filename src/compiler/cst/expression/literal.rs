use serde::Serialize;

use crate::compiler::{cst::Node, lexer::token::Token, utilities::location::Location};

#[derive(Serialize, Clone)]
pub enum LiteralKind {
    Integer,
    String,
}

#[derive(Serialize, Clone)]
pub struct Literal<'source> {
    kind: LiteralKind,
    token: Token<'source>,
}

impl<'source> Literal<'source> {
    pub fn new(kind: LiteralKind, token: Token<'source>) -> Literal {
        Literal { kind, token }
    }

    pub fn kind(&self) -> &LiteralKind {
        &self.kind
    }

    pub fn token(&self) -> &Token<'source> {
        &self.token
    }
}

impl<'source> Node<'source> for Literal<'source> {
    fn location(&self) -> Location<'source> {
        self.token.location().clone()
    }
}
