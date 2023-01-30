use serde::Serialize;

use super::{expression::Expression, Node};

#[derive(Serialize)]
pub enum Statement<'a> {
    Expression(Node<'a, Expression<'a>>),
}
