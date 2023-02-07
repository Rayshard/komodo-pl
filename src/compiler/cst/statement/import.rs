use serde::Serialize;

use crate::compiler::{
    cst::Node,
    lexer::token::Token,
    utilities::{location::Location, range::Range},
};

use super::import_path::ImportPath;

#[derive(Serialize, Clone)]
pub struct Import<'source> {
    keyword_import: Token<'source>,
    import_path: ImportPath<'source>,
    from_path: Option<(Token<'source>, ImportPath<'source>)>,
}

impl<'source> Import<'source> {
    pub fn new(
        keyword_import: Token<'source>,
        import_path: ImportPath<'source>,
        from_path: Option<(Token<'source>, ImportPath<'source>)>,
    ) -> Self {
        Self {
            keyword_import,
            import_path,
            from_path,
        }
    }

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
    fn location(&self) -> Location<'source> {
        Location::new(
            self.keyword_import.location().source(),
            Range::new(
                self.keyword_import.location().range().start(),
                match &self.from_path {
                    Some((_, path)) => path.location().range().end(),
                    None => self.import_path.location().range().end(),
                },
            ),
        )
    }
}
