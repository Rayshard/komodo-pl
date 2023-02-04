use crate::compiler::{
    ast::{script::Script as ASTScript, ScriptNode},
    parsing::cst::{script::Script as CSTScript, Node as CSTNode},
    typesystem::{context::Context, ts_type::TSType},
};

use super::{result::TypecheckResult, typecheck_consecutive_mut, statement};

pub fn typecheck<'source>(
    script: CSTScript<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ScriptNode<'source>> {
    let mut ctx = Context::new(Some(ctx));
    let statements = typecheck_consecutive_mut(script.statements(), statement::typecheck, &mut ctx)?;

    let ts_type = if let Some(statement) = statements.last() {
        statement.ts_type().clone()
    } else {
        TSType::Unit
    };

    Ok(ScriptNode::new(
        ASTScript::new(statements),
        ts_type,
        script.source(),
        script.range(),
    ))
}
