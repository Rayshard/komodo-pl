use std::fmt::Display;

use serde::Serialize;

use super::{position::Position, range::Range, text_source::TextSource};

#[derive(Debug, PartialEq, Clone, Serialize)]
pub struct Location<'source> {
    source: &'source TextSource,
    range: Range,
}

impl<'source> Location<'source> {
    pub fn new(source: &'source TextSource, range: Range) -> Self {
        Self { source, range }
    }

    pub fn source(&self) -> &'source TextSource {
        self.source
    }

    pub fn range(&self) -> &Range {
        &self.range
    }

    pub fn start_position(&self) -> Position {
        self.source.get_position(self.range.start()).unwrap()
    }

    pub fn start_terminal_link(&self) -> String {
        self.source.get_terminal_link(self.range.start()).unwrap()
    }

    pub fn end_position(&self) -> Position {
        self.source.get_position(self.range.end()).unwrap()
    }

    pub fn text(&self) -> &'source str {
        self.source.text_from_range(&self.range)
    }
}

impl<'source> Display for Location<'source> {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}:{}", self.source.name(), self.range)
    }
}
