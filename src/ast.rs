use crate::utils::{Position, Span};
use core::fmt::Debug;

#[derive(Debug)]
pub struct NodeMeta {
    span: Span,
    ts_type: Option<String>,
}

impl NodeMeta {
    pub fn new(start: Position, end: Position) -> NodeMeta {
        NodeMeta {
            span: Span::new(start, end),
            ts_type: None,
        }
    }
}

pub trait Node {
    fn get_node_meta(&self) -> &NodeMeta;
    fn get_node_type(&self) -> NodeType;

    fn get_span(&self) -> &Span {
        &self.get_node_meta().span
    }

    fn get_ts_type(&self) -> &Option<String> {
        &self.get_node_meta().ts_type
    }
}


#[derive(Debug)]
pub enum NodeType<'a> {
    Expr(&'a Expression),
}

impl Debug for dyn Node {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        write!(
            f,
            "{:?}",
            match self.get_node_type() {
                NodeType::Expr(expr) => expr,
            }
        )
    }
}

#[derive(Debug)]
pub enum Expression {
    Binop {
        meta: NodeMeta,
        left: Box<Expression>,
        op: String,
        right: Box<Expression>,
    },
    IntLit {
        meta: NodeMeta,
        value: String,
    },
}

impl Node for Expression {
    fn get_node_meta(&self) -> &NodeMeta {
        match self {
            Expression::Binop { meta, .. } => meta,
            Expression::IntLit { meta, .. } => meta,
        }
    }

    fn get_node_type(&self) -> NodeType {
        NodeType::Expr(self)
    }
}
