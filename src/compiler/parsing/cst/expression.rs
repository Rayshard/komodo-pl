use serde::Serialize;

use crate::compiler::{
    lexing::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use super::{binary_operator::BinaryOperator, unary_operator::UnaryOperator, Node};

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

impl<'a> Node<'a> for Expression<'a> {
    fn range(&self) -> Range {
        match self {
            Expression::IntegerLiteral(token) => token.range().clone(),
            Expression::StringLiteral(token) => token.range().clone(),
            Expression::Identifier(_) => todo!(),
            Expression::MemberAccess { head, dot: _, member } => Range::new(head.range().start(), member.range().end()),
            Expression::Call {
                head,
                open_parenthesis: _,
                arg: _,
                close_parenthesis,
            } => Range::new(head.range().start(), close_parenthesis.range().end()),
            Expression::Unary { operand: _, op: _ } => todo!(),
            Expression::Binary { left, op: _, right } => {
                Range::new(left.range().start(), right.range().end())
            }
            Expression::Parenthesized {
                open_parenthesis,
                expression: _,
                close_parenthesis,
            } => Range::new(
                open_parenthesis.range().start(),
                close_parenthesis.range().end(),
            ),
        }
    }

    fn source(&self) -> &'a TextSource {
        match self {
            Expression::IntegerLiteral(token) => token.source(),
            Expression::StringLiteral(token) => token.source(),
            Expression::Identifier(token) => token.source(),
            Expression::MemberAccess { head, dot: _, member: _ } => head.source(),
            Expression::Call {
                head,
                open_parenthesis: _,
                arg: _,
                close_parenthesis: _,
            } => head.source(),
            Expression::Unary { operand, op: _ } => operand.source(),
            Expression::Binary { left, op: _, right: _ } => left.source(),
            Expression::Parenthesized {
                open_parenthesis: _,
                expression,
                close_parenthesis: _,
            } => expression.source(),
        }
    }
}
