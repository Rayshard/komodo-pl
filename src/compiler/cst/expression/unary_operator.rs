use serde::Serialize;

use crate::compiler::utilities::location::Location;

use super::Node;

#[derive(Serialize, Clone)]
pub enum UnaryOperator {}

impl<'source> Node<'source> for UnaryOperator {
    fn location(&self) -> &Location<'source> {
        todo!()
    }
}
