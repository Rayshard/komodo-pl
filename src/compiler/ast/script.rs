use serde::Serialize;

use super::{StatementNode};

#[derive(Serialize)]
pub struct Script<'source> {
    statements: Vec<StatementNode<'source>>,
}

impl<'source> Script<'source> {
    pub fn new(statements: Vec<StatementNode<'source>>) -> Script<'source> {
        Script { statements }
    }

    pub fn statements(&self) -> &[StatementNode<'source>] {
        &self.statements
    }
}
