use serde::Serialize;

use crate::compiler::{
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};

use super::{statement::Statement, Node};

#[derive(Serialize)]
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

impl<'source> Node for Script<'source> {
    fn ts_type(&self) -> &TSType {
        match self.statements.last() {
            Some(statement) => statement.ts_type(),
            None => &TSType::Unit,
        }
    }
    
    fn range(&self) -> &Range {
        todo!()
    }

    fn source(&self) -> &TextSource {
        todo!()
    }
}
