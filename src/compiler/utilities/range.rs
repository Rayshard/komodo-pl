use std::{fmt::Display};

#[derive(Debug, PartialEq)]
pub struct Range {
    start: usize,
    length: usize,
}

impl Range {
    pub fn new(start: usize, length: usize) -> Self {
        Self { start, length }
    }

    pub fn start(&self) -> usize {
        self.start
    }

    pub fn end(&self) -> usize {
        self.start + self.length
    }

    pub fn length(&self) -> usize {
        self.length
    }
}

impl Display for Range {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "[{}, {})", self.start(), self.end())
    }
}