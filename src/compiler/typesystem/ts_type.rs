use std::collections::HashMap;

use serde::Serialize;

#[derive(Debug, Clone, Serialize)]
pub enum TSType {
    Unit,
    Int64,
    String,
    Object {
        name: String,
        members: HashMap<String, Box<TSType>>
    },
    Function {
        name: String,
        parameters: Vec<(String, Box<TSType>)>,
        return_type: Box<TSType>
    },
    Module {
        name: String,
        members: HashMap<String, Box<TSType>>
    }
}
