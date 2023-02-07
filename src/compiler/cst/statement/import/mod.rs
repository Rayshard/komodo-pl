pub mod from_qualifier;
pub mod import_path;

use serde::Serialize;

use crate::compiler::{
    cst::Node,
    lexer::token::Token,
    utilities::{location::Location, range::Range},
};

use self::{from_qualifier::FromQualifier, import_path::ImportPath};

#[derive(Serialize, Clone)]
pub struct Import<'source> {
    keyword_import: Token<'source>,
    path: ImportPath<'source>,
    from_qualifier: Option<FromQualifier<'source>>,
}

impl<'source> Import<'source> {
    pub fn new(
        keyword_import: Token<'source>,
        path: ImportPath<'source>,
        from_qualifier: Option<FromQualifier<'source>>,
    ) -> Self {
        Self {
            keyword_import,
            path,
            from_qualifier,
        }
    }

    pub fn keyword_import(&self) -> &Token<'source> {
        &self.keyword_import
    }

    pub fn path(&self) -> &ImportPath<'source> {
        &self.path
    }

    pub fn from_qualifier(&self) -> &Option<FromQualifier<'source>> {
        &self.from_qualifier
    }
}

impl<'source> Node<'source> for Import<'source> {
    fn location(&self) -> Location<'source> {
        Location::new(
            self.keyword_import.location().source(),
            Range::new(
                self.keyword_import.location().range().start(),
                match &self.from_qualifier {
                    Some(from_qualifier) => from_qualifier.location().range().end(),
                    None => self.path.location().range().end(),
                },
            ),
        )
    }
}
