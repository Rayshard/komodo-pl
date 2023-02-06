use serde::Serialize;

use crate::compiler::{
    cst::{expression::identifier::Identifier, Node},
    lexer::token::Token,
    utilities::{location::Location, range::Range},
};

#[derive(Serialize, Clone)]
pub enum ImportPathKind<'source> {
    Simple(Identifier<'source>),
    Complex {
        root: Box<ImportPath<'source>>,
        dot: Token<'source>,
        member: Identifier<'source>,
    },
}

#[derive(Serialize, Clone)]
pub struct ImportPath<'source> {
    kind: ImportPathKind<'source>,
    location: Location<'source>,
}

impl<'source> ImportPath<'source> {
    pub fn new(kind: ImportPathKind<'source>) -> Self {
        let location = match &kind {
            ImportPathKind::Simple(identifier) => identifier.location().clone(),
            ImportPathKind::Complex { root, dot, member } => Location::new(
                dot.location().source(),
                Range::new(
                    root.location().range().start(),
                    member.location().range().end(),
                ),
            ),
        };

        Self { kind, location }
    }

    pub fn kind(&self) -> &ImportPathKind<'source> {
        &self.kind
    }
}

impl<'source> Node<'source> for ImportPath<'source> {
    fn location(&self) -> &Location<'source> {
        &self.location
    }
}
