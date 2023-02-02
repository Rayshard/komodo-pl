use serde::Serialize;

use crate::compiler::utilities::range::Range;

use super::{node::Nodeable, statement::Statement, Node};

#[derive(Serialize)]
pub struct Script<'a> {
    statements: Vec<Node<'a, Statement<'a>>>,
}

impl<'a> Script<'a> {
    pub fn new(statements: Vec<Node<'a, Statement<'a>>>) -> Script<'a> {
        Script { statements }
    }

    pub fn statements(&self) -> &[Node<'a, Statement<'a>>] {
        &self.statements
    }
}

impl<'a> Nodeable for Script<'a> {
    fn range(&self) -> Range {
        todo!()
    }
}
