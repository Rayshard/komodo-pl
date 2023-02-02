use serde::{ser::SerializeMap, Serialize};

use crate::compiler::{
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};

pub trait Nodeable: Serialize {
    fn range(&self) -> Range;
}

pub struct Node<'a, T>
where
    T: Nodeable,
{
    instance: T,
    ts_type: TSType,
    source: &'a TextSource,
}

impl<'a, T> Node<'a, T>
where
    T: Nodeable,
{
    pub fn new(instance: T, ts_type: TSType, source: &'a TextSource) -> Node<'a, T> {
        Node {
            instance,
            ts_type,
            source,
        }
    }

    pub fn instance(&self) -> &T {
        &self.instance
    }

    pub fn ts_type(&self) -> &TSType {
        &self.ts_type
    }

    pub fn range(&self) -> Range {
        self.instance.range()
    }

    pub fn source(&self) -> &'a TextSource {
        self.source
    }
}

impl<'a, T> Serialize for Node<'a, T>
where
    T: Nodeable,
{
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        let mut map = serializer.serialize_map(None)?;

        map.serialize_entry("source", self.source.name())?;
        map.serialize_entry("instance", &self.instance)?;
        map.serialize_entry("ts_type", &self.ts_type)?;

        map.end()
    }
}
