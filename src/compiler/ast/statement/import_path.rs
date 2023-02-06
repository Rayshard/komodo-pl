use serde::Serialize;

use crate::compiler::{
    ast::{expression::identifier::Identifier, Node},
    typesystem::ts_type::TSType,
    utilities::{location::Location, range::Range},
};

#[derive(Serialize)]
pub enum ImportPath<'source> {
    Simple(Identifier<'source>),
    Complex {
        root: Box<ImportPath<'source>>,
        member: Identifier<'source>,
    },
}

impl<'source> Node<'source> for ImportPath<'source> {
    fn ts_type(&self) -> &TSType {
        match self {
            ImportPath::Simple(node) => node.ts_type(),
            ImportPath::Complex { root: _, member } => member.ts_type(),
        }
    }

    fn location(&self) -> Location<'source> {
        match self {
            ImportPath::Simple(identifier) => identifier.location().clone(),
            ImportPath::Complex { root, member } => Location::new(
                root.location().source(),
                Range::new(
                    root.location().range().start(),
                    member.location().range().end(),
                ),
            ),
        }
    }
}
