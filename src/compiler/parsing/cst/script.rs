use serde::{Serialize, ser::SerializeMap};

use crate::compiler::utilities::text_source::TextSource;

use super::statement::Statement;

#[derive(Debug)]
pub struct Script<'source> {
    source: &'source TextSource,
    statements: Vec<Statement<'source>>,
}

impl<'source> Script<'source> {
    pub fn new(source: &'source TextSource, statements: Vec<Statement<'source>>) -> Script<'source> {
        Script { source, statements }
    }

    pub fn statements(&self) -> &[Statement<'source>] {
        &self.statements
    }

    pub fn source(&self) -> &'source TextSource {
        &self.source
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