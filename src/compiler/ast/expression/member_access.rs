use serde::Serialize;

use crate::compiler::{ast::Node, typesystem::ts_type::TSType, utilities::location::Location};

use super::{identifier::Identifier, Expression};

#[derive(Serialize)]
pub struct MemberAccess<'source> {
    root: Box<Expression<'source>>,
    member: Identifier<'source>,
}

impl<'source> MemberAccess<'source> {
    pub fn new(root: Expression<'source>, member: Identifier<'source>) -> Self {
        Self {
            root: Box::new(root),
            member,
        }
    }

    pub fn root(&self) -> &Expression<'source> {
        self.root.as_ref()
    }

    pub fn member(&self) -> &Identifier<'source> {
        &self.member
    }
}

impl<'source> Node<'source> for MemberAccess<'source> {
    fn ts_type(&self) -> &TSType {
        self.member.ts_type()
    }

    fn location(&self) -> Location<'source> {
        todo!()
    }
}
