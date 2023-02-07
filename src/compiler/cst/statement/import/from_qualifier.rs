use serde::Serialize;

use crate::compiler::{lexer::token::Token, cst::Node, utilities::{location::Location, range::Range}};

use super::import_path::ImportPath;

#[derive(Serialize, Clone)]
pub struct FromQualifier<'source> {
    keyword_from: Token<'source>,
    path: ImportPath<'source>,
}

impl<'source> FromQualifier<'source> {
    pub fn new(keyword_from: Token<'source>, path: ImportPath<'source>) -> Self {
        Self { keyword_from, path }
    }

    pub fn keyword_from(&self) -> &Token<'source> {
        &self.keyword_from
    }

    pub fn path(&self) -> &ImportPath<'source> {
        &self.path
    }
}

impl<'source> Node<'source> for FromQualifier<'source> {
    fn location(&self) -> Location<'source> {
        Location::new(
            self.keyword_from.location().source(),
            Range::new(
                self.keyword_from.location().range().start(),
                self.path.location().range().end(),
            ),
        )
    }
}
