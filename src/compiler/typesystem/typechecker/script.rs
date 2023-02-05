use crate::compiler::{
    ast::{script::Script as ASTScript, statement::Statement as ASTStatement},
    cst::{script::Script as CSTScript, statement::Statement as CSTStatement},
    typesystem::context::Context,
};

use super::{result::TypecheckResult, statement, typecheck_consecutive_mut, TypecheckerMut};

pub fn typecheck<'source>(
    script: CSTScript<'source>,
    ctx: &Context,
) -> TypecheckResult<'source, ASTScript<'source>> {
    let mut ctx = Context::new(Some(ctx));
    let statements =vec![];
        //typecheck_consecutive_mut(script.statements(), statement::typecheck, &mut ctx)?;

    Ok(ASTScript::new(statements))
}
