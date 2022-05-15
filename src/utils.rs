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
