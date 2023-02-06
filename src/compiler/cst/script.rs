use serde::Serialize;

use crate::compiler::utilities::{location::Location, text_source::TextSource};

use super::{statement::Statement, Node};

#[derive(Serialize)]
pub struct Script<'source> {
    source: &'source TextSource,
    statements: Vec<Statement<'source>>,
    location: Location<'source>,
}

impl<'source> Script<'source> {
    pub fn new(source: &'source TextSource, statements: Vec<Statement<'source>>) -> Self {
        Self {
            source,
            statements,
            location: source.as_location(),
        }
    }

    pub fn statements(&self) -> &[Statement<'source>] {
        &self.statements
    }

    pub fn source(&self) -> &'source TextSource {
        &self.source
    }
}

impl<'source> Node<'source> for Script<'source> {
    fn location(&self) -> &Location<'source> {
        &self.location
    }
}
