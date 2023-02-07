use super::utilities::location::Location;

pub mod expression;
pub mod script;
pub mod statement;

pub trait Node<'source> {
    fn location(&self) -> Location<'source>;
}
