use serde::Serialize;

use crate::compiler::{utilities::{range::Range, text_source::TextSource}, cst::{expression::{member_access::MemberAccess, identifier::Identifier}, Node}};

#[derive(Serialize)]
pub enum ImportPath<'source> {
    Simple(Identifier<'source>),
    Complex(MemberAccess<'source>),
}

impl<'source> Node<'source> for ImportPath<'source> {
    fn range(&self) -> Range {
        match self {
            ImportPath::Simple(identifier) => identifier.range().clone(),
            ImportPath::Complex(member_access) => member_access.range(),
        }
    }

    fn source(&self) -> &'source TextSource {
        match self {
            ImportPath::Simple(identifier) => identifier.source(),
            ImportPath::Complex(member_access) => member_access.source(),
        }
    }
}
