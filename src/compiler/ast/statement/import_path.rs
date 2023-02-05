use serde::Serialize;

use crate::compiler::{
    ast::{
        expression::{identifier::Identifier, member_access::MemberAccess},
        Node,
    },
    typesystem::ts_type::TSType,
    utilities::location::Location,
};

#[derive(Serialize)]
pub enum ImportPath<'source> {
    Simple(Identifier<'source>),
    Complex(MemberAccess<'source>),
}

impl<'source> Node<'source> for ImportPath<'source> {
    fn ts_type(&self) -> &TSType {
        match self {
            ImportPath::Simple(node) => node.ts_type(),
            ImportPath::Complex(node) => node.ts_type(),
        }
    }

    fn location(&self) -> Location<'source> {
        match self {
            ImportPath::Simple(node) => node.location(),
            ImportPath::Complex(node) => node.location(),
        }
    }
}
