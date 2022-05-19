use crate::utils::Position;
use core::fmt::Debug;

pub trait Node {
    fn get_position(&self) -> &Position;
    fn get_node_type(&self) -> NodeType;
    fn get_ts_type(&self) -> &Option<String>;
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
                _ => unimplemented!(),
            }
        )
    }
}

#[derive(Debug)]
pub enum Expression {
    Binop {
        left: Box<Expression>,
        op: String,
        right: Box<Expression>,
        ts_type: Option<String>,
    },
    IntLit {
        value: String,
        pos: Position,
        ts_type: Option<String>,
    },
}

impl Node for Expression {
    fn get_position(&self) -> &Position {
        match self {
            Expression::Binop { left, .. } => left.get_position(),
            Expression::IntLit { pos, .. } => pos,
        }
    }

    fn get_node_type(&self) -> NodeType {
        NodeType::Expr(self)
    }

    fn get_ts_type(&self) -> &Option<String> {
        match self {
            Expression::Binop { ts_type, .. } => ts_type,
            Expression::IntLit { ts_type, .. } => ts_type,
        }
    }
}
