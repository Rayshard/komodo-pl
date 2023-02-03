use std::collections::HashMap;

use super::ts_type::TSType;

pub type ContextError = String;
pub type ContextResult<T> = Result<T, ContextError>;

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

    pub fn get(&self, name: &str) -> ContextResult<&TSType> {
        self.entries.get(name).map_or_else(
            || {
                if let Some(parent) = self.parent {
                    parent.get(name)
                } else {
                    Err(format!(
                        "Name '{name}' does not exist in the current context."
                    ))
                }
            },
            |ts_type| Ok(ts_type),
        )
    }

    pub fn set(&mut self, name: &str, ts_type: TSType) -> ContextResult<()> {
        if self.entries.contains_key(name) {
            Err(format!(
                "Name '{name}' already exists in the current context."
            ))
        } else {
            self.entries.insert(name.to_string(), ts_type);
            Ok(())
        }
    }

    pub fn from(
        ts_type: &TSType,
        parent: Option<&'parent Context<'parent>>,
    ) -> ContextResult<Context<'parent>> {
        let mut ctx = Context::new(parent);

        match ts_type {
            TSType::Object { name: _, members } => {
                for (member_name, member_ts_type) in members {
                    ctx.set(&member_name, member_ts_type.clone())?;
                }
            }
            TSType::Function {
                name: _,
                parameters: _,
                return_type: _,
            } => todo!(),
            TSType::Module { name: _, members } => {
                for (member_name, member_ts_type) in members {
                    ctx.set(&member_name, member_ts_type.clone())?;
                }
            }
            ts_type => panic!("Cannot create a context from {ts_type:?}"),
        }

        Ok(ctx)
    }
}
