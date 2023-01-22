use crate::lexer::Token;

pub struct Module<'a> {
    elements: Vec<Expression<'a>>,
}

impl<'a> Module<'a> {
    pub fn new(elements: Vec<Expression>) -> Module {
        Module { elements }
    }

    pub fn elements(&self) -> &[Expression<'a>] {
        &self.elements
    }
}

#[derive(Debug)]
pub enum Expression<'a> {
    IntegerLiteral(&'a Token),
    Binary {
        left: Box<Expression<'a>>,
        op: &'a Token,
        right: Box<Expression<'a>>,
    },
    Parenthesized {
        open_parenthesis: &'a Token,
        expression: Box<Expression<'a>>,
        close_parenthesis: &'a Token,
    }
}
