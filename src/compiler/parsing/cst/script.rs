use serde::{Serialize, ser::SerializeMap};

use crate::compiler::utilities::text_source::TextSource;

use super::statement::Statement;

#[derive(Debug)]
pub struct Script<'a> {
    source: &'a TextSource,
    statements: Vec<Statement<'a>>,
}

impl<'a> Script<'a> {
    pub fn new(source: &'a TextSource, statements: Vec<Statement<'a>>) -> Script<'a> {
        Script { source, statements }
    }

    pub fn statements(&self) -> &[Statement] {
        &self.statements
    }

    pub fn source(&self) -> &TextSource {
        &self.source
    }
}

impl<'a> Serialize for Script<'a> {
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