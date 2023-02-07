use std::{collections::HashMap, fs, io};

use colored::Colorize;
use komodo::{
    compiler::{self, utilities::text_source::TextSource},
    runtime::interpreter::{self, Value},
};

pub fn text_source_from_file(path: &str) -> io::Result<TextSource> {
    let text = fs::read_to_string(path)?;
    Ok(TextSource::new(path.to_string(), text))
}

fn main() {
    let source = match text_source_from_file("tests/e2e/hello-world.kmd") {
        Ok(source) => source,
        Err(error) => {
            println!("{}", format!("ERROR | Unable to load file: {error}").red());
            std::process::exit(1);
        }
    };

    match compiler::compile(&source) {
        Ok(script) => {
            //println!("{}", serde_yaml::to_string(&script).unwrap());

            let ctx = interpreter::Context::from([(
                "std".to_string(),
                Value::Module {
                    name: "std".to_string(),
                    members: HashMap::from([(
                        "io".to_string(),
                        Value::Module {
                            name: "std.io".to_string(),
                            members: HashMap::from([(
                                "stdout".to_string(),
                                Value::Object {
                                    name: "std.io.stdout".to_string(),
                                    members: HashMap::from([(
                                        "print_line".to_string(),
                                        Value::Function("std.io.stdout.print_line".to_string()),
                                    )]),
                                },
                            )]),
                        },
                    )]),
                },
            )]);

            let result = interpreter::interpret_script(&script, &ctx);
            println!("{result:?}");
        }
        Err(error) => {
            println!("{}", error.to_string().red());
            std::process::exit(1);
        }
    }
}
