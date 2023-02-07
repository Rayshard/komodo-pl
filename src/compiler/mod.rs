use std::collections::HashMap;

use self::{
    ast::script::Script as ASTScript,
    cst::script::Script as CSTScript,
    lexer::token::Token,
    result::{CompilationError, CompilationResult},
    typesystem::{
        context::Context,
        ts_type::{Function as TSFunction, FunctionOverload as TSFunctionOverload, TSType},
        typechecker,
    },
    utilities::{range::Range, text_source::TextSource},
};

pub mod ast;
pub mod cst;
pub mod lexer;
pub mod parser;
pub mod result;
pub mod typesystem;
pub mod utilities;

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
) -> CompilationResult<'source, ASTScript<'source>> {
    typechecker::script::typecheck(script, ctx)
        .map_err(|error| CompilationError::Typechecker(error))
}

pub fn compile<'source>(source: &'source TextSource) -> CompilationResult<'source, ASTScript<'source>> {
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
                                TSType::Function(TSFunction::new(
                                    "print_line".to_string(),
                                    vec![
                                        TSFunctionOverload::new(
                                            vec![("value".to_string(), TSType::String)],
                                            TSType::Unit,
                                        ),
                                        TSFunctionOverload::new(
                                            vec![("value".to_string(), TSType::Int64)],
                                            TSType::Unit,
                                        ),
                                    ],
                                )),
                            )]),
                        },
                    )]),
                },
            )]),
        },
        source.get_location(Range::new(0, 0)).unwrap(),
    )
    .map_err(|error| CompilationError::Context(error))?;

    ctx.add_binary_operator_overload(
        "binop::+",
        (TSType::Int64, TSType::Int64),
        TSType::Int64,
        source.get_location(Range::new(0, 0)).unwrap(),
    )
    .map_err(|error| CompilationError::Context(error))?;

    ctx.add_binary_operator_overload(
        "binop::+",
        (TSType::String, TSType::String),
        TSType::String,
        source.get_location(Range::new(0, 0)).unwrap(),
    )
    .map_err(|error| CompilationError::Context(error))?;

    typecheck(script, &ctx)
}
