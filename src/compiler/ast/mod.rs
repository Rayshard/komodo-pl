use self::{script::Script, statement::{Statement}, expression::Expression, literal::Literal, import_path::ImportPath, node::Node};

pub mod node;
pub mod expression;
pub mod literal;
pub mod script;
pub mod statement;
pub mod import_path;

pub type ScriptNode<'a> = Node<'a, Script<'a>>;
pub type StatementNode<'a> = Node<'a, Statement<'a>>;
pub type ImportPathNode<'a> = Node<'a, ImportPath<'a>>;
pub type ExpressionNode<'a> = Node<'a, Expression<'a>>;
pub type LiteralNode<'a> = Node<'a, Literal>;