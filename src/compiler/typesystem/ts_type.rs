use std::collections::HashMap;

use serde::Serialize;

#[derive(Debug, Clone, Serialize, PartialEq)]
pub struct FunctionOverload {
    parameters: Vec<(String, TSType)>,
    return_type: TSType,
}

impl FunctionOverload {
    pub fn new(parameters: Vec<(String, TSType)>, return_type: TSType) -> Self {
        Self {
            parameters,
            return_type,
        }
    }

    pub fn parameters(&self) -> &[(String, TSType)] {
        &self.parameters
    }

    pub fn return_type(&self) -> &TSType {
        &self.return_type
    }
}

#[derive(Debug, Clone, Serialize, PartialEq)]
pub struct Function {
    name: String,
    overloads: Vec<FunctionOverload>,
}

impl Function {
    pub fn new(name: String, overloads: Vec<FunctionOverload>) -> Self {
        Self { name, overloads }
    }

    pub fn name(&self) -> &str {
        &self.name
    }

    pub fn overloads(&self) -> &[FunctionOverload] {
        &self.overloads
    }

    pub fn add_overload(&mut self, overload: FunctionOverload) -> Result<(), String> {
        for existing_overload in self.overloads.iter_mut() {
            if &overload == existing_overload {
                return Err(format!(
                    "Overload {:?} already exists for function {}",
                    existing_overload, self.name
                ));
            }
        }

        Ok(self.overloads.push(overload))
    }
}

#[derive(Debug, Clone, Serialize, PartialEq)]
pub enum TSType {
    Unit,
    Int64,
    Float64,
    String,
    Object {
        name: String,
        members: HashMap<String, TSType>,
    },
    Function(Function),
    Module {
        name: String,
        members: HashMap<String, TSType>,
    },
}
