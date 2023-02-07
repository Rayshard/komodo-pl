use crate::compiler::{
    ast::script::Script as ASTScript, cst::script::Script as CSTScript,
    typesystem::context::Context,
};

use super::{result::TypecheckResult, statement, typecheck_consecutive_mut};

pub fn typecheck<'source>(
    script: CSTScript<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTScript<'source>> {
    let mut ctx = Context::new(Some(ctx));
    let statements =
        typecheck_consecutive_mut(script.statements(), statement::typecheck, &mut ctx)?;

    Ok(ASTScript::new(statements))
}
