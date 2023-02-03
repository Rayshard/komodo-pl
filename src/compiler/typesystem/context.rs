use std::collections::HashMap;

use super::ts_type::TSType;

pub type ContextError = String;

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

    pub fn get(&self, name: &str) -> Result<&TSType, ContextError> {
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

    pub fn set(&mut self, name: &str, ts_type: TSType) -> Result<(), ContextError> {
        if self.entries.contains_key(name) {
            Err(format!("Name '{name}' already exists in the current context."))
        }
        else {
            self.entries.insert(name.to_string(), ts_type);
            Ok(())
        }
    }
}
