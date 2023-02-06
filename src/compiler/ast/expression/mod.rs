pub mod binary;
pub mod call;
pub mod identifier;
pub mod literal;
pub mod member_access;
pub mod binary_operator;

use self::{
    binary::Binary, call::Call, identifier::Identifier, literal::Literal,
    member_access::MemberAccess,
};

use super::Node;
use crate::compiler::{typesystem::ts_type::TSType, utilities::location::Location};
use serde::Serialize;

#[derive(Serialize)]
pub enum Expression<'source> {
    Literal(Literal<'source>),
    Binary(Binary<'source>),
    Call(Call<'source>),
    MemberAccess(MemberAccess<'source>),
    Identifier(Identifier<'source>),
}

impl<'source> Node<'source> for Expression<'source> {
    fn ts_type(&self) -> &TSType {
        match self {
            Expression::Literal(node) => node.ts_type(),
            Expression::Binary(node) => node.ts_type(),
            Expression::Call(node) => node.ts_type(),
            Expression::MemberAccess(node) => node.ts_type(),
            Expression::Identifier(node) => node.ts_type(),
        }
    }

    fn location(&self) -> Location<'source> {
        match self {
            Expression::Literal(node) => node.location(),
            Expression::Binary(node) => node.location(),
            Expression::Call(node) => node.location(),
            Expression::MemberAccess(node) => node.location(),
            Expression::Identifier(node) => node.location(),
        }
    }
}
