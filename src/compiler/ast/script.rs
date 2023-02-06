use serde::Serialize;

use crate::compiler::{typesystem::ts_type::TSType, utilities::location::Location};

use super::{statement::Statement, Node};

#[derive(Serialize, Clone)]
pub struct Script<'source> {
    statements: Vec<Statement<'source>>,
}

impl<'source> Script<'source> {
    pub fn new(statements: Vec<Statement<'source>>) -> Script<'source> {
        Script { statements }
    }

    pub fn statements(&self) -> &[Statement<'source>] {
        &self.statements
    }
}

impl<'source> Node<'source> for Script<'source> {
    fn ts_type(&self) -> &TSType {
        match self.statements.last() {
            Some(statement) => statement.ts_type(),
            None => &TSType::Unit,
        }
    }

    fn location(&self) -> Location<'source> {
        todo!()
    }
}
