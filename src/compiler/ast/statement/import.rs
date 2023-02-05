use serde::Serialize;

use crate::compiler::{
    ast::{statement::import_path::ImportPath, Node},
    typesystem::ts_type::TSType,
    utilities::location::Location,
};

#[derive(Serialize)]
pub struct Import<'source> {
    path: ImportPath<'source>,
    from: Option<ImportPath<'source>>,
}

impl<'source> Import<'source> {
    pub fn new(path: ImportPath<'source>, from: Option<ImportPath<'source>>) -> Self {
        Self { path, from }
    }

    pub fn path(&self) -> &ImportPath<'source> {
        &self.path
    }

    pub fn from(&self) -> &Option<ImportPath<'source>> {
        &self.from
    }
}

impl<'source> Node<'source> for Import<'source> {
    fn ts_type(&self) -> &TSType {
        &TSType::Unit
    }

    fn location(&self) -> Location<'source> {
        todo!()
    }
}
