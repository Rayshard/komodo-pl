pub mod binary;
pub mod call;
pub mod identifier;
pub mod literal;
pub mod member_access;

use self::{
    binary::Binary, call::Call, identifier::Identifier, literal::Literal,
    member_access::MemberAccess,
};

use super::Node;
use crate::compiler::{
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};
use serde::Serialize;

#[derive(Serialize)]
pub enum Expression<'source> {
    Literal(Literal<'source>),
    Binary(Binary<'source>),
    Call(Call<'source>),
    MemberAccess(MemberAccess<'source>),
    Identifier(Identifier<'source>),
}

impl<'source> Node for Expression<'source> {
    fn ts_type(&self) -> &TSType {
        match self {
            Expression::Literal(literal) => literal.ts_type(),
            Expression::Binary(binary) => binary.ts_type(),
            Expression::Call(call) => call.ts_type(),
            Expression::MemberAccess(member_access) => member_access.ts_type(),
            Expression::Identifier(identifier) => identifier.ts_type(),
        }
    }

    fn range(&self) -> &Range {
        match self {
            Expression::Literal(literal) => literal.range(),
            Expression::Binary(binary) => binary.range(),
            Expression::Call(call) => call.range(),
            Expression::MemberAccess(member_access) => member_access.range(),
            Expression::Identifier(identifier) => identifier.range(),
        }
    }

    fn source(&self) -> &TextSource {
        match self {
            Expression::Literal(literal) => literal.source(),
            Expression::Binary(binary) => binary.source(),
            Expression::Call(call) => call.source(),
            Expression::MemberAccess(member_access) => member_access.source(),
            Expression::Identifier(identifier) => identifier.source(),
        }
    }
}
