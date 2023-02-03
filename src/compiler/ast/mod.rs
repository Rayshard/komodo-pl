use self::{script::Script, statement::{Statement}, expression::Expression, literal::Literal, import_path::ImportPath, node::Node};

pub mod node;
pub mod expression;
pub mod literal;
pub mod script;
pub mod statement;
pub mod import_path;

pub type ScriptNode<'source> = Node<'source, Script<'source>>;
pub type StatementNode<'source> = Node<'source, Statement<'source>>;
pub type ImportPathNode<'source> = Node<'source, ImportPath<'source>>;
pub type ExpressionNode<'source> = Node<'source, Expression<'source>>;
pub type LiteralNode<'source> = Node<'source, Literal>;