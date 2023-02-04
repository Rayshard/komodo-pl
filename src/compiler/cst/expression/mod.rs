pub mod binary;
pub mod binary_operator;
pub mod call;
pub mod identifier;
pub mod literal;
pub mod member_access;
pub mod parenthesized;
pub mod unary_operator;
pub mod unary;

use serde::Serialize;

use crate::compiler::utilities::{range::Range, text_source::TextSource};

use self::{
    binary::Binary, call::Call, identifier::Identifier, literal::Literal,
    member_access::MemberAccess, parenthesized::Parenthesized, unary::Unary,
};

use super::Node;

#[derive(Serialize)]
pub enum Expression<'source> {
    Literal(Literal<'source>),
    Identifier(Identifier<'source>),
    MemberAccess(MemberAccess<'source>),
    Call(Call<'source>),
    Unary(Unary<'source>),
    Binary(Binary<'source>),
    Parenthesized(Parenthesized<'source>),
}

impl<'source> Node<'source> for Expression<'source> {
    fn range(&self) -> Range {
        match self {
            Expression::Literal(literal) => literal.range(),
            Expression::Identifier(identifier) => identifier.range().clone(),
            Expression::MemberAccess(member_access) => member_access.range(),
            Expression::Call(call) => call.range(),
            Expression::Unary(unary) => unary.range(),
            Expression::Binary(binary) => binary.range(),
            Expression::Parenthesized(parenthesized) => parenthesized.range(),
        }
    }

    fn source(&self) -> &'source TextSource {
        match self {
            Expression::Literal(literal) => literal.source(),
            Expression::Identifier(token) => token.source(),
            Expression::MemberAccess(member_access) => member_access.source(),
            Expression::Call(call) => call.source(),
            Expression::Unary(unary) => unary.source(),
            Expression::Binary(binary) => binary.source(),
            Expression::Parenthesized(parenthesized) => parenthesized.source(),
        }
    }
}
