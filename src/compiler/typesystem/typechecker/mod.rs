pub mod expression;
pub mod import_path;
pub mod result;
pub mod script;
pub mod statement;

use self::result::TypecheckResult;
use super::context::Context;

type Typechecker<'source, TIn, TOut> =
    fn(item: &TIn, ctx: &Context) -> TypecheckResult<'source, TOut>;

type TypecheckerMut<'source, TIn, TOut> =
    fn(item: &TIn, ctx: &mut Context) -> TypecheckResult<'source, TOut>;

pub fn typecheck_consecutive<'source, TIn, TOut>(
    items: &[TIn],
    typechecker: Typechecker<'source, TIn, TOut>,
    ctx: &Context,
) -> TypecheckResult<'source, Vec<TOut>> {
    let mut results = vec![];

    for item in items {
        results.push(typechecker(item, ctx)?);
    }

    Ok(results)
}

pub fn typecheck_consecutive_mut<'source, TIn, TOut>(
    items: &[TIn],
    typechecker: TypecheckerMut<'source, TIn, TOut>,
    ctx: &mut Context,
) -> TypecheckResult<'source, Vec<TOut>> {
    let mut results = vec![];

    for item in items {
        results.push(typechecker(item, ctx)?);
    }

    Ok(results)
}
