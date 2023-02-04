use serde::Serialize;

use crate::compiler::{ast::Node, typesystem::ts_type::TSType, utilities::{range::Range, text_source::TextSource}};

use super::{Expression, identifier::Identifier};

#[derive(Serialize)]
pub struct MemberAccess<'source> {
    root: Box<Expression<'source>>,
    member: Identifier<'source>,
}

impl<'source> MemberAccess<'source> {
    pub fn root(&self) -> &Expression<'source> {
        self.root.as_ref()
    }

    pub fn member(&self) -> &Identifier<'source> {
        &self.member
    }
}

impl<'source> Node for MemberAccess<'source> {
    fn ts_type(&self) -> &TSType {
        self.member.ts_type()
    }

    fn range(&self) -> &Range {
        todo!()
    }

    fn source(&self) -> &TextSource {
        todo!()
    }
}