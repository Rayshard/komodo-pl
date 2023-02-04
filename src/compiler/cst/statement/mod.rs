pub mod import;
pub mod import_path;

use serde::Serialize;

use crate::compiler::{
    lexer::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use self::import::Import;

use super::{expression::Expression, Node};

#[derive(Serialize)]
pub enum StatementKind<'source> {
    Import(Import<'source>),
    Expression(Expression<'source>),
}

#[derive(Serialize)]
pub struct Statement<'source> {
    kind: StatementKind<'source>,
    semicolon: Token<'source>,
}

impl<'source> Statement<'source> {
    pub fn kind(&self) -> &StatementKind {
        &self.kind
    }

    pub fn semicolon(&self) -> &Token<'source> {
        &self.semicolon
    }
}

impl<'source> Node<'source> for Statement<'source> {
    fn range(&self) -> Range {
        Range::new(
            match &self.kind {
                StatementKind::Import(import) => import.range(),
                StatementKind::Expression(expression) => expression.range(),
            }
            .start(),
            self.semicolon.range().end(),
        )
    }

    fn source(&self) -> &'source TextSource {
        self.semicolon.source()
    }
}
