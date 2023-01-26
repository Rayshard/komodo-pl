use super::lexing::token::Token;

pub struct Module<'a> {
    statements: Vec<Statement<'a>>,
}

impl<'a> Module<'a> {
    pub fn new(statements: Vec<Statement>) -> Module {
        Module { statements }
    }

    pub fn statements(&self) -> &[Statement<'a>] {
        &self.statements
    }
}

#[derive(Debug)]
pub enum Statement<'a> {
    Expression(Expression<'a>, &'a Token)
}

#[derive(Debug)]
pub enum Expression<'a> {
    IntegerLiteral(&'a Token),
    Binary {
        left: Box<Expression<'a>>,
        op: BinaryOperator<'a>,
        right: Box<Expression<'a>>,
    },
    Parenthesized {
        open_parenthesis: &'a Token,
        expression: Box<Expression<'a>>,
        close_parenthesis: &'a Token,
    },
}

#[derive(Debug)]
pub enum BinaryOperatorKind {
    Add,
    Subtract,
    Multiply,
    Divide,
}

#[derive(Debug)]
pub struct BinaryOperator<'a> {
    kind: BinaryOperatorKind,
    token: &'a Token,
}

impl<'a> BinaryOperator<'a> {
    pub fn new(kind: BinaryOperatorKind, token: &'a Token) -> BinaryOperator {
        BinaryOperator { kind, token }
    }

    pub fn kind(&self) -> &BinaryOperatorKind {
        &self.kind
    }

    pub fn precedence(&self) -> u32 {
        self.kind.precedence()
    }

    pub fn is_right_associative(&self) -> bool {
        self.kind.is_right_associative()
    }

    pub fn is_left_associative(&self) -> bool {
        self.kind.is_left_associative()
    }
}

impl BinaryOperatorKind {
    pub fn precedence(&self) -> u32 {
        match self {
            BinaryOperatorKind::Add => 0,
            BinaryOperatorKind::Subtract => 0,
            BinaryOperatorKind::Multiply => 1,
            BinaryOperatorKind::Divide => 1,
        }
    }

    pub fn is_right_associative(&self) -> bool {
        match self {
            BinaryOperatorKind::Add => false,
            BinaryOperatorKind::Subtract => false,
            BinaryOperatorKind::Multiply => false,
            BinaryOperatorKind::Divide => false,
        }
    }

    pub fn is_left_associative(&self) -> bool {
        match self {
            BinaryOperatorKind::Add => true,
            BinaryOperatorKind::Subtract => true,
            BinaryOperatorKind::Multiply => true,
            BinaryOperatorKind::Divide => true,
        }
    }
}
