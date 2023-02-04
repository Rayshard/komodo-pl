use std::{collections::HashMap, fmt::Display, io};

use colored::Colorize;
use komodo::{
    compiler::{
        ast::ScriptNode as ASTScriptNode,
        cst::script::Script as CSTScript,
        lexing::{
            lexer::{self, LexError},
            token::Token,
        },
        parsing::parser::{self, ParseError},
        typesystem::{
            context::Context,
            ts_type::TSType,
            typechecker::{self, result::TypecheckError},
        },
        utilities::text_source::TextSource,
    },
    runtime::interpreter::{self, Value},
};

enum CompilationError<'source> {
    File(io::Error),
    Lexer(Vec<LexError<'source>>),
    Parser(ParseError<'source>),
    Typechecker(TypecheckError<'source>),
}

impl<'source> Display for CompilationError<'source> {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            CompilationError::File(error) => write!(
                f,
                "{}",
                format!("ERROR | Unable to load file: {error}").red()
            ),
            CompilationError::Lexer(errors) => {
                let concated_errors = errors
                    .iter()
                    .map(|e| e.to_string())
                    .collect::<Vec<String>>()
                    .join("\n");

                write!(f, "{}", concated_errors.red())
            }
            CompilationError::Parser(error) => write!(f, "{}", error.to_string().red()),
            CompilationError::Typechecker(error) => write!(f, "{}", error.to_string().red()),
        }
    }
}

type CompilationResult<'source, T> = Result<T, CompilationError<'source>>;

fn lex<'source>(source: &'source TextSource) -> CompilationResult<Vec<Token<'source>>> {
    let (tokens, errors) = lexer::lex(source);

    // for token in tokens.iter() {
    //     println!("{token:?} = {}", escape(token.value(&input)))
    // }

    if errors.is_empty() {
        Ok(tokens)
    } else {
        Err(CompilationError::Lexer(errors))
    }
}

fn parse<'source>(
    source: &'source TextSource,
    tokens: Vec<Token<'source>>,
) -> CompilationResult<'source, CSTScript<'source>> {
    parser::parse_script(source, &tokens).map_or_else(
        |error| Err(CompilationError::Parser(error)),
        |script| Ok(script),
    )
}

fn typecheck<'source>(
    script: CSTScript<'source>,
    ctx: &Context,
) -> CompilationResult<'source, ASTScriptNode<'source>> {
    typechecker::script::typecheck(script, ctx)
        .map_err(|error| CompilationError::Typechecker(error))
}

fn compile<'source>(
    source: &'source TextSource,
) -> CompilationResult<'source, ASTScriptNode<'source>> {
    let tokens = lex(source)?;
    let script = parse(source, tokens)?;

    let mut ctx = Context::new(None);
    ctx.set(
        "std",
        TSType::Module {
            name: "std".to_string(),
            members: HashMap::from([(
                "io".to_string(),
                TSType::Module {
                    name: "io".to_string(),
                    members: HashMap::from([(
                        "stdout".to_string(),
                        TSType::Object {
                            name: "stdout".to_string(),
                            members: HashMap::from([(
                                "print_line".to_string(),
                                TSType::Function {
                                    name: "print_line".to_string(),
                                    parameters: vec![("value".to_string(), TSType::String)],
                                    return_type: Box::new(TSType::Unit),
                                },
                            )]),
                        },
                    )]),
                },
            )]),
        },
    )
    .unwrap();

    typecheck(script, &ctx)
}

fn main() {
    let source = match TextSource::from_file("tests/e2e/hello-world.kmd")
        .map_err(|error| CompilationError::File(error))
    {
        Ok(source) => source,
        Err(error) => {
            println!("{error}");
            std::process::exit(1);
        }
    };

    match compile(&source) {
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
            println!("{error}");
            std::process::exit(1);
        }
    }
}
