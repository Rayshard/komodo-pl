use serde::{Serialize, ser::SerializeMap};

use self::{script::Script, statement::Statement, expression::Expression, literal::Literal};

use super::{typesystem::ts_type::TSType, utilities::text_source::TextSource};

pub mod expression;
pub mod literal;
pub mod script;
pub mod statement;

pub struct Node<'a, T>
where
    T: Serialize,
{
    instance: T,
    ts_type: TSType,
    source: &'a TextSource,
}

impl<'a, T> Node<'a, T>
where
    T: Serialize,
{
    pub fn new(instance: T, ts_type: TSType, source: &'a TextSource) -> Node<'a, T> {
        Node {
            instance,
            ts_type,
            source,
        }
    }

    pub fn instance(&self) -> &T {
        &self.instance
    }

    pub fn ts_type(&self) -> &TSType {
        &self.ts_type
    }
}

impl<'a, T> Serialize for Node<'a, T>
where
    T: Serialize,
{
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        let mut map = serializer.serialize_map(None)?;

        map.serialize_entry("source", self.source.name())?;
        map.serialize_entry("instance", &self.instance)?;
        map.serialize_entry("ts_type", &self.ts_type)?;

        map.end()
    }
}

pub type ScriptNode<'a> = Node<'a, Script<'a>>;
pub type StatementNode<'a> = Node<'a, Statement<'a>>;
pub type ExpressionNode<'a> = Node<'a, Expression<'a>>;
pub type LiteralNode<'a> = Node<'a, Literal>;