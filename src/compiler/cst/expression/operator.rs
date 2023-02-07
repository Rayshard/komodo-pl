use crate::compiler::cst::Node;

pub trait Operator<'source>: Node<'source> {
    fn context_id(&self) -> &str;
}
