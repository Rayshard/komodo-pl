use serde::Serialize;

#[derive(Serialize)]
pub enum Literal {
    Int64(i64)
}