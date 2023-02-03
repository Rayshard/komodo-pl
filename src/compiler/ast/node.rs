use serde::{ser::SerializeMap, Serialize};

use crate::compiler::{
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};

pub struct Node<'source, T>
where
    T: Serialize,
{
    instance: T,
    ts_type: TSType,
    source: &'source TextSource,
    range: Range,
}

impl<'source, T> Node<'source, T>
where
    T: Serialize,
{
    pub fn new(
        instance: T,
        ts_type: TSType,
        source: &'source TextSource,
        range: Range,
    ) -> Node<'source, T> {
        Node {
            instance,
            ts_type,
            source,
            range,
        }
    }

    pub fn instance(&self) -> &T {
        &self.instance
    }

    pub fn ts_type(&self) -> &TSType {
        &self.ts_type
    }

    pub fn range(&self) -> &Range {
        &self.range
    }

    pub fn source(&self) -> &'source TextSource {
        self.source
    }
}

impl<'source, T> Serialize for Node<'source, T>
where
    T: Serialize,
{
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        let mut map = serializer.serialize_map(None)?;

        map.serialize_entry("source", self.source.name())?;
        map.serialize_entry("range", &self.range)?;
        map.serialize_entry("instance", &self.instance)?;
        map.serialize_entry("ts_type", &self.ts_type)?;

        map.end()
    }
}
