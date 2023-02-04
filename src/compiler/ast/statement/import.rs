use serde::Serialize;

use crate::compiler::{
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource}, ast::{statement::import_path::ImportPath, Node},
};

#[derive(Serialize)]
pub struct Import<'source> {
    path: ImportPath<'source>,
    from: Option<ImportPath<'source>>,
}

impl<'source> Import<'source> {
    pub fn path(&self) -> &ImportPath<'source> {
        &self.path
    }

    pub fn from(&self) -> &Option<ImportPath<'source>> {
        &self.from
    }
}

impl<'source> Node for Import<'source> {
    fn ts_type(&self) -> &TSType {
        &TSType::Unit
    }

    fn range(&self) -> &Range {
        todo!()
    }

    fn source(&self) -> &TextSource {
        todo!()
    }
}
