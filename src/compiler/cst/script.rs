use serde::{ser::SerializeMap, Serialize};

use crate::compiler::utilities::{location::Location, text_source::TextSource};

use super::{statement::Statement, Node};

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

impl<'source> Serialize for Script<'source> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        let mut map = serializer.serialize_map(None)?;

        map.serialize_entry("source", self.source.name())?;
        map.serialize_entry("statements", &self.statements)?;

        map.end()
    }
}
