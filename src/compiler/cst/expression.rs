use serde::Serialize;

use crate::compiler::{
    lexing::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use super::{binary_operator::BinaryOperator, unary_operator::UnaryOperator, Node};

#[derive(Debug, Serialize)]
pub enum Expression<'source> {
    IntegerLiteral(Token<'source>),
    StringLiteral(Token<'source>),
    Identifier(Token<'source>),
    MemberAccess {
        head: Box<Expression<'source>>,
        dot: Token<'source>,
        member: Token<'source>,
    },
    Call {
        head: Box<Expression<'source>>,
        open_parenthesis: Token<'source>,
        args: Vec<Expression<'source>>,
        close_parenthesis: Token<'source>,
    },
    Unary {
        operand: Box<Expression<'source>>,
        op: UnaryOperator,
    },
    Binary {
        left: Box<Expression<'source>>,
        op: BinaryOperator<'source>,
        right: Box<Expression<'source>>,
    },
    Parenthesized {
        open_parenthesis: Token<'source>,
        expression: Box<Expression<'source>>,
        close_parenthesis: Token<'source>,
    },
}

impl<'source> Node<'source> for Expression<'source> {
    fn range(&self) -> Range {
        match self {
            Expression::IntegerLiteral(token) => token.range().clone(),
            Expression::StringLiteral(token) => token.range().clone(),
            Expression::Identifier(token) => token.range().clone(),
            Expression::MemberAccess { head, dot: _, member } => Range::new(head.range().start(), member.range().end()),
            Expression::Call {
                head,
                open_parenthesis: _,
                args: _,
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

    fn source(&self) -> &'source TextSource {
        match self {
            Expression::IntegerLiteral(token) => token.source(),
            Expression::StringLiteral(token) => token.source(),
            Expression::Identifier(token) => token.source(),
            Expression::MemberAccess { head, dot: _, member: _ } => head.source(),
            Expression::Call {
                head,
                open_parenthesis: _,
                args: _,
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
