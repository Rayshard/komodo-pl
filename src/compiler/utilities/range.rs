use std::{fmt::Display};

use serde::Serialize;

#[derive(Debug, PartialEq, Eq, Clone, Serialize)]
pub struct Range {
    start: usize,
    end: usize,
}

impl Range {
    pub fn new(start: usize, end: usize) -> Self {
        assert!(end >= start, "end must be >= start");
        Self { start, end }
    }

    pub fn start(&self) -> usize {
        self.start
    }

    pub fn end(&self) -> usize {
        self.end
    }

    pub fn length(&self) -> usize {
        self.end - self.start
    }
}

impl Display for Range {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "[{}, {})", self.start(), self.end())
    }
}