use serde::Serialize;

#[derive(Debug, Clone, Serialize)]
pub enum TSType {
    Unit,
    Int64,
}
