use serde::Serialize;

use crate::compiler::{
    cst::{
        expression::{identifier::Identifier, member_access::MemberAccess},
        Node,
    },
    utilities::location::Location,
};

#[derive(Serialize, Clone)]
pub enum ImportPath<'source> {
    Simple(Identifier<'source>),
    Complex(MemberAccess<'source>),
}

impl<'source> Node<'source> for ImportPath<'source> {
    fn location(&self) -> &Location<'source> {
        match self {
            ImportPath::Simple(node) => node.location(),
            ImportPath::Complex(node) => node.location(),
        }
    }
}
