use serde::Serialize;

use crate::compiler::lexing::token::Token;

use super::{unary_operator::UnaryOperator, binary_operator::BinaryOperator};

#[derive(Debug, Serialize)]
pub enum Expression<'a> {
    IntegerLiteral(Token<'a>),
    StringLiteral(Token<'a>),
    Identifier(Token<'a>),
    MemberAccess {
        head: Box<Expression<'a>>,
        dot: Token<'a>,
        member: Token<'a>,
    },
    Call {
        head: Box<Expression<'a>>,
        open_parenthesis: Token<'a>,
        arg: Box<Expression<'a>>,
        close_parenthesis: Token<'a>,
    },
    Unary {
        operand: Box<Expression<'a>>,
        op: UnaryOperator,
    },
    Binary {
        left: Box<Expression<'a>>,
        op: BinaryOperator<'a>,
        right: Box<Expression<'a>>,
    },
    Parenthesized {
        open_parenthesis: Token<'a>,
        expression: Box<Expression<'a>>,
        close_parenthesis: Token<'a>,
    },
}