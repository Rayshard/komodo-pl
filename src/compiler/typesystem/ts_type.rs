use std::collections::HashMap;

use serde::Serialize;

#[derive(Debug, Clone, Serialize, PartialEq)]
pub enum TSType {
    Unit,
    Int64,
    String,
    Object {
        name: String,
        members: HashMap<String, TSType>
    },
    Function {
        name: String,
        parameters: Vec<(String, TSType)>,
        return_type: Box<TSType>
    },
    Module {
        name: String,
        members: HashMap<String, TSType>
    }
}
