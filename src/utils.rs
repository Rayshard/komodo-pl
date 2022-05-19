use serde_json;

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

#[derive(Debug, Copy, Clone, PartialEq)]
pub struct Span {
    pub start: Position,
    pub end: Position,
}

impl Span {
    pub fn new(start: Position, end: Position) -> Span {
        Span {
            start: start,
            end: end,
        }
    }

    pub fn to_json(&self) -> serde_json::Value {
        serde_json::json!({
            "start": self.start.to_json(),
            "end": self.end.to_json(),
        })
    }
}
