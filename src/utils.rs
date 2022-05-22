use serde_json;
use std::fmt;

#[derive(Debug, Copy, Clone, PartialEq)]
pub struct Position {
    pub line: usize,
    pub column: usize,
}

impl Position {
    pub fn new(line: usize, column: usize) -> Position {
        Position {
            line: line,
            column: column,
        }
    }

    pub fn to_json(&self) -> serde_json::Value {
        serde_json::json!({
            "line": self.line,
            "column": self.column,
        })
    }
}

#[derive(Debug, Clone, PartialEq)]
pub struct Span<'a> {
    pub start: Position,
    pub end: Position,
    pub file_name: &'a String
}

impl<'a> Span<'a> {
    pub fn new(start: Position, end: Position, file_name: &String) -> Span {
        Span {
            start: start,
            end: end,
            file_name: file_name
        }
    }

    pub fn to_json(&self) -> serde_json::Value {
        serde_json::json!({
            "start": self.start.to_json(),
            "end": self.end.to_json(),
        })
    }
}

pub trait Error {
    fn get_span(&self) -> Span;
    fn get_message(&self) -> String;
}

impl fmt::Display for dyn Error {
    fn fmt(&self, fmt: &mut fmt::Formatter) -> fmt::Result {
        let span = self.get_span();

        fmt.write_str(&format!(
            "Error at {}:{}:{}\n\t{}",
            span.file_name,
            span.start.line,
            span.start.column,
            self.get_message()
        ))
    }
}
