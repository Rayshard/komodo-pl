use std::collections::HashMap;

use crate::compiler::{
    cst::expression::operator::Operator as CSTOperator, utilities::location::Location,
};

use super::ts_type::{Function, FunctionOverload, TSType};

#[derive(Debug, Clone)]
pub struct ContextError<'source> {
    message: String,
    location: Location<'source>,
}

impl<'source> ContextError<'source> {
    pub fn new(message: String, location: Location<'source>) -> Self {
        Self { message, location }
    }

    pub fn message(&self) -> &str {
        &self.message
    }

    pub fn location(&self) -> &Location<'source> {
        &self.location
    }
}

impl<'source> ToString for ContextError<'source> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.location.start_terminal_link(),
            self.message
        )
    }
}

pub type ContextResult<'source, T> = Result<T, ContextError<'source>>;

pub struct Context<'parent> {
    parent: Option<&'parent Context<'parent>>,
    entries: HashMap<String, TSType>,
    operators: HashMap<String, Function>,
}

impl<'parent> Context<'parent> {
    pub fn new(parent: Option<&'parent Context<'parent>>) -> Context<'parent> {
        Context {
            parent,
            entries: HashMap::new(),
            operators: HashMap::new(),
        }
    }

    pub fn get<'source>(
        &self,
        name: &str,
        location: Location<'source>,
    ) -> ContextResult<'source, &TSType> {
        self.entries.get(name).map_or_else(
            || {
                if let Some(parent) = self.parent {
                    parent.get(name, location)
                } else {
                    Err(ContextError::new(
                        format!("Name '{name}' does not exist in the current context."),
                        location,
                    ))
                }
            },
            |ts_type| Ok(ts_type),
        )
    }

    pub fn set<'source>(
        &mut self,
        name: &str,
        ts_type: TSType,
        location: Location<'source>,
    ) -> ContextResult<'source, ()> {
        if self.entries.contains_key(name) {
            Err(ContextError::new(
                format!("Name '{name}' already exists in the current context."),
                location,
            ))
        } else {
            self.entries.insert(name.to_string(), ts_type);
            Ok(())
        }
    }

    pub fn get_operator_function<'source, T>(
        &self,
        operator: &T,
    ) -> ContextResult<'source, &Function>
    where
        T: CSTOperator<'source>,
    {
        self.operators.get(operator.context_id()).map_or_else(
            || {
                if let Some(parent) = self.parent {
                    parent.get_operator_function(operator)
                } else {
                    Err(ContextError::new(
                        format!(
                            "Operator '{}' does not exist in the current context.",
                            operator.context_id()
                        ),
                        operator.location().clone(),
                    ))
                }
            },
            |function| Ok(function),
        )
    }

    pub fn add_binary_operator_overload<'source>(
        &mut self,
        id: &str,
        (operand1, operand2): (TSType, TSType),
        return_type: TSType,
        location: Location<'source>,
    ) -> ContextResult<'source, ()> {
        let overload = FunctionOverload::new(
            vec![
                ("left".to_string(), operand1),
                ("right".to_string(), operand2),
            ],
            return_type,
        );

        if let Some(function) = self.operators.get_mut(id) {
            function
                .add_overload(overload)
                .map_err(|error| ContextError::new(error, location))
        } else {
            self.operators.insert(
                id.to_string(),
                Function::new(id.to_string(), vec![overload]),
            );

            Ok(())
        }
    }

    pub fn from<'source>(
        ts_type: &TSType,
        parent: Option<&'parent Context<'parent>>,
        location: Location<'source>,
    ) -> ContextResult<'source, Context<'parent>> {
        let mut ctx = Context::new(parent);

        match ts_type {
            TSType::Object { name: _, members } => {
                for (member_name, member_ts_type) in members {
                    ctx.set(&member_name, member_ts_type.clone(), location.clone())?;
                }
            }
            TSType::Module { name: _, members } => {
                for (member_name, member_ts_type) in members {
                    ctx.set(&member_name, member_ts_type.clone(), location.clone())?;
                }
            }
            ts_type => panic!("Cannot create a context from {ts_type:?}"),
        }

        Ok(ctx)
    }
}
