pub mod binary;
pub mod binary_operator;
pub mod call;
pub mod identifier;
pub mod literal;
pub mod member_access;
pub mod parenthesized;
pub mod unary;
pub mod unary_operator;
pub mod operator;

use serde::Serialize;

use crate::compiler::utilities::location::Location;

use self::{
    binary::Binary, call::Call, identifier::Identifier, literal::Literal,
    member_access::MemberAccess, parenthesized::Parenthesized, unary::Unary,
};

use super::Node;

#[derive(Serialize, Clone)]
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
    fn location(&self) -> Location<'source> {
        match self {
            Expression::Literal(literal) => literal.location(),
            Expression::Identifier(identifier) => identifier.location(),
            Expression::MemberAccess(member_access) => member_access.location(),
            Expression::Call(call) => call.location(),
            Expression::Unary(unary) => unary.location(),
            Expression::Binary(binary) => binary.location(),
            Expression::Parenthesized(parenthesized) => parenthesized.location(),
        }
    }
}
