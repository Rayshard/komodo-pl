use super::lexing::token::Token;

pub struct Script {
    statements: Vec<Statement>,
}

impl Script {
    pub fn new(statements: Vec<Statement>) -> Script {
        Script { statements }
    }

    pub fn statements(&self) -> &[Statement] {
        &self.statements
    }
}

#[derive(Debug)]
pub enum Statement {
    Import { keyword: Token, path: Token, semicolon: Token },
    Expression(Expression, Token),
}

#[derive(Debug)]
pub enum Expression {
    IntegerLiteral(Token),
    Identifier(Token),
    Call(Box<Expression>, Token, Box<Expression>, Token),
    Binary {
        left: Box<Expression>,
        op: BinaryOperator,
        right: Box<Expression>,
    },
    Parenthesized {
        open_parenthesis: Token,
        expression: Box<Expression>,
        close_parenthesis: Token,
    },
}

#[derive(Debug)]
pub enum BinaryOperatorKind {
    Add,
    Subtract,
    Multiply,
    Divide,
    MemberAccess,
}

#[derive(Debug)]
pub struct BinaryOperator {
    kind: BinaryOperatorKind,
    token: Token,
}

impl<'a> BinaryOperator {
    pub fn new(kind: BinaryOperatorKind, token: Token) -> BinaryOperator {
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
            BinaryOperatorKind::MemberAccess => 2,
        }
    }

    pub fn is_right_associative(&self) -> bool {
        match self {
            BinaryOperatorKind::Add => false,
            BinaryOperatorKind::Subtract => false,
            BinaryOperatorKind::Multiply => false,
            BinaryOperatorKind::Divide => false,
            BinaryOperatorKind::MemberAccess => false,
        }
    }

    pub fn is_left_associative(&self) -> bool {
        match self {
            BinaryOperatorKind::Add => true,
            BinaryOperatorKind::Subtract => true,
            BinaryOperatorKind::Multiply => true,
            BinaryOperatorKind::Divide => true,
            BinaryOperatorKind::MemberAccess => true,
        }
    }
}
