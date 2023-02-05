use std::collections::HashMap;

use crate::compiler::utilities::location::Location;

use super::ts_type::TSType;

pub struct ContextError<'source> {
    message: String,
    location: Location<'source>,
}

impl<'source> ContextError<'source> {
    pub fn new(message: String, location: Location) -> Self {
        Self { message, location }
    }

    pub fn message(&self) -> &str {
        &self.message
    }

    pub fn location(&self) -> &Location {
        &self.location
    }
}

pub type ContextResult<'source, T> = Result<T, ContextError<'source>>;

pub struct Context<'parent> {
    parent: Option<&'parent Context<'parent>>,
    entries: HashMap<String, TSType>,
}

impl<'parent> Context<'parent> {
    pub fn new(parent: Option<&'parent Context<'parent>>) -> Context<'parent> {
        Context {
            parent,
            entries: HashMap::new(),
        }
    }

    pub fn get(&self, name: &str, location: Location) -> ContextResult<&TSType> {
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

    pub fn set(&mut self, name: &str, ts_type: TSType, location: Location) -> ContextResult<()> {
        if self.entries.contains_key(name) {
            Err(ContextError::new(
                format!(
                    "Name '{name}' already exists in the current context."
                ),
                location,
            ))
        } else {
            self.entries.insert(name.to_string(), ts_type);
            Ok(())
        }
    }

    pub fn from<'source>(
        ts_type: &TSType,
        parent: Option<&'parent Context<'parent>>,
        location: Location<'source>
    ) -> ContextResult<'source, Context<'parent>> {
        let mut ctx = Context::new(parent);

        match ts_type {
            TSType::Object { name: _, members } => {
                for (member_name, member_ts_type) in members {
                    ctx.set(&member_name, member_ts_type.clone(), location)?;
                }
            }
            TSType::Function {
                name: _,
                parameters: _,
                return_type: _,
            } => todo!(),
            TSType::Module { name: _, members } => {
                for (member_name, member_ts_type) in members {
                    ctx.set(&member_name, member_ts_type.clone(), location)?;
                }
            }
            ts_type => panic!("Cannot create a context from {ts_type:?}"),
        }

        Ok(ctx)
    }
}
