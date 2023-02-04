use serde::Serialize;

use crate::compiler::{
    cst::Node,
    lexer::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use super::import_path::ImportPath;

#[derive(Serialize)]
pub struct Import<'source> {
    keyword_import: Token<'source>,
    import_path: ImportPath<'source>,
    from_path: Option<(Token<'source>, ImportPath<'source>)>,
}

impl<'source> Import<'source> {
    pub fn keyword_import(&self) -> &Token<'source> {
        &self.keyword_import
    }

    pub fn import_path(&self) -> &ImportPath<'source> {
        &self.import_path
    }

    pub fn from_path(&self) -> &Option<(Token<'source>, ImportPath<'source>)> {
        &self.from_path
    }
}

impl<'source> Node<'source> for Import<'source> {
    fn range(&self) -> Range {
        Range::new(
            self.keyword_import.range().start(),
            match &self.from_path {
                Some((_, path)) => path.range().end(),
                None => self.import_path.range().end(),
            },
        )
    }

    fn source(&self) -> &'source TextSource {
        self.import_path.source()
    }
}
