use super::{lexing::token::Token, utilities::text_source::TextSource};

#[derive(Debug)]
pub struct Script {
    source: TextSource,
    statements: Vec<Statement>,
}

impl Script {
    pub fn new(source: TextSource, statements: Vec<Statement>) -> Script {
        Script { source, statements }
    }

    pub fn statements(&self) -> &[Statement] {
        &self.statements
    }

    pub fn source(&self) -> &TextSource {
        &self.source
    }
}

#[derive(Debug)]
pub enum ImportPath {
    Simple(Token),
    Complex {
        head: Box<ImportPath>,
        dot: Token,
        member: Token
    }
}

#[derive(Debug)]
pub enum Statement {
    Import {
        keyword_import: Token,
        import_path: ImportPath,
        from_path: Option<(Token, ImportPath)>,
        semicolon: Token,
    },
    Expression(Expression, Token),
}

#[derive(Debug)]
pub enum Expression {
    IntegerLiteral(Token),
    StringLiteral(Token),
    Identifier(Token),
    MemberAccess {
        head: Box<Expression>,
        dot: Token,
        member: Token,
    },
    Call {
        head: Box<Expression>,
        open_parenthesis: Token,
        arg: Box<Expression>,
        close_parenthesis: Token,
    },
    Unary {
        operand: Box<Expression>,
        op: UnaryOperator,
    },
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
}

#[derive(Debug)]
pub struct BinaryOperator {
    kind: BinaryOperatorKind,
    token: Token,
}

impl BinaryOperator {
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

#[derive(Debug)]
pub enum UnaryOperator {}
