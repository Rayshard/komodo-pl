pub enum Node {
    Expr(Expression)
}

pub enum Expression {
    Literal(Literal)
}

pub enum Literal {
    Integer(u64)
}