use serde::Serialize;

use crate::compiler::{lexer::token::Token, utilities::location::Location};

use super::Node;

#[derive(Debug, Serialize, Clone)]
pub enum BinaryOperatorKind {
    Add,
    Subtract,
    Multiply,
    Divide,
}

#[derive(Serialize, Clone)]
pub struct BinaryOperator<'source> {
    kind: BinaryOperatorKind,
    token: Token<'source>,
}

impl<'source> BinaryOperator<'source> {
    pub fn new(kind: BinaryOperatorKind, token: Token<'source>) -> BinaryOperator {
        BinaryOperator { kind, token }
    }

    pub fn kind(&self) -> &BinaryOperatorKind {
        &self.kind
    }

    pub fn precedence(&self) -> u32 {
        self.kind.precedence()
    }

    pub fn is_right_associative(&self) -> bool {
        self.kind.is_right_associative()
    }

    pub fn is_left_associative(&self) -> bool {
        self.kind.is_left_associative()
    }
}

impl BinaryOperatorKind {
    pub fn precedence(&self) -> u32 {
        match self {
            BinaryOperatorKind::Add => 0,
            BinaryOperatorKind::Subtract => 0,
            BinaryOperatorKind::Multiply => 1,
            BinaryOperatorKind::Divide => 1,
        }
    }

    pub fn is_right_associative(&self) -> bool {
        match self {
            BinaryOperatorKind::Add => false,
            BinaryOperatorKind::Subtract => false,
            BinaryOperatorKind::Multiply => false,
            BinaryOperatorKind::Divide => false,
        }
    }

    pub fn is_left_associative(&self) -> bool {
        match self {
            BinaryOperatorKind::Add => true,
            BinaryOperatorKind::Subtract => true,
            BinaryOperatorKind::Multiply => true,
            BinaryOperatorKind::Divide => true,
        }
    }
}

impl<'source> Node<'source> for BinaryOperator<'source> {
    fn location(&self) -> &Location<'source> {
        self.token.location()
    }
}
