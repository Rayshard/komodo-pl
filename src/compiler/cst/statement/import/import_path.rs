use serde::Serialize;

use crate::compiler::{
    cst::{expression::identifier::Identifier, Node},
    lexer::token::Token,
    utilities::{location::Location, range::Range},
};

#[derive(Serialize, Clone)]
pub enum ImportPath<'source> {
    Simple(Identifier<'source>),
    Complex {
        root: Box<ImportPath<'source>>,
        dot: Token<'source>,
        member: Identifier<'source>,
    },
}

impl<'source> Node<'source> for ImportPath<'source> {
    fn location(&self) -> Location<'source> {
        match self {
            ImportPath::Simple(identifier) => identifier.location().clone(),
            ImportPath::Complex { root, dot, member } => Location::new(
                dot.location().source(),
                Range::new(
                    root.location().range().start(),
                    member.location().range().end(),
                ),
            ),
        }
    }
}
