use crate::utils::{Position, Span};
use core::fmt::Debug;

#[derive(Debug)]
pub struct NodeMeta<'a> {
    span: Span<'a>,
    ts_type: Option<String>,
}

impl<'a> NodeMeta<'a> {
    pub fn new(span: Span) -> NodeMeta {
        NodeMeta {
            span: span,
            ts_type: None,
        }
    }
}

pub trait Node {
    fn get_node_meta(&self) -> &NodeMeta;
    fn get_node_kind(&self) -> NodeKind;

    fn get_span(&self) -> &Span {
        &self.get_node_meta().span
    }

    fn get_ts_type(&self) -> &Option<String> {
        &self.get_node_meta().ts_type
    }
}


#[derive(Debug)]
pub enum NodeKind<'a> {
    Expr(&'a Expression<'a>),
}

impl Debug for dyn Node {
    fn fmt(&self, f: &mut core::fmt::Formatter<'_>) -> core::fmt::Result {
        write!(
            f,
            "{:?}",
            match self.get_node_kind() {
                NodeKind::Expr(expr) => expr,
            }
        )
    }
}

#[derive(Debug)]
pub enum Expression<'a> {
    Binop {
        meta: NodeMeta<'a>,
        left: Box<Expression<'a>>,
        op: String,
        right: Box<Expression<'a>>,
    },
    IntLit {
        meta: NodeMeta<'a>,
        value: String,
    },
}

impl Node for Expression<'_> {
    fn get_node_meta(&self) -> &NodeMeta {
        match self {
            Expression::Binop { meta, .. } => meta,
            Expression::IntLit { meta, .. } => meta,
        }
    }

    fn get_node_kind(&self) -> NodeKind {
        NodeKind::Expr(self)
    }
}
