use serde::Serialize;

use crate::compiler::{
    ast::{
        expression::{identifier::Identifier, member_access::MemberAccess},
        Node,
    },
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};

#[derive(Serialize)]
pub enum ImportPath<'source> {
    Simple(Identifier<'source>),
    Complex(MemberAccess<'source>),
}

impl<'source> Node for ImportPath<'source> {
    fn ts_type(&self) -> &TSType {
        match self {
            ImportPath::Simple(identifier) => identifier.ts_type(),
            ImportPath::Complex(member_access) => member_access.ts_type(),
        }
    }

    fn range(&self) -> &Range {
        match self {
            ImportPath::Simple(identifier) => identifier.range(),
            ImportPath::Complex(member_access) => member_access.range(),
        }
    }

    fn source(&self) -> &TextSource {
        match self {
            ImportPath::Simple(identifier) => identifier.source(),
            ImportPath::Complex(member_access) => member_access.source(),
        }
    }
}
