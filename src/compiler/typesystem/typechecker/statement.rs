use crate::compiler::{
    ast::{
        import_path::ImportPath as ASTImportPath, statement::Statement as ASTStatement,
        StatementNode,
    },
    cst::{statement::Statement as CSTStatement, Node},
    typesystem::{context::Context, ts_type::TSType},
};

use super::{
    expression, import_path,
    result::{TypecheckError, TypecheckErrorKind, TypecheckResult},
};

pub fn typecheck<'source>(
    statement: &CSTStatement<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, StatementNode<'source>> {
    match statement {
        CSTStatement::Import {
            keyword_import: _,
            import_path,
            from_path,
            semicolon: _,
        } => {
            let (import_path, from_path) = if let Some((_, from_path)) = from_path {
                let from_path = import_path::typecheck(from_path, ctx)?;

                match from_path.ts_type() {
                    TSType::Module {
                        name: _,
                        members: _,
                    } => {
                        let mut import_ctx =
                            Context::from(from_path.ts_type(), None).map_err(|error| {
                                TypecheckError::new(
                                    TypecheckErrorKind::Context(error),
                                    from_path.range().clone(),
                                    from_path.source(),
                                )
                            })?;

                        Ok((
                            import_path::typecheck(import_path, &mut import_ctx)?,
                            Some(from_path),
                        ))
                    }
                    ts_type => Err(TypecheckError::new(
                        TypecheckErrorKind::ImportFromNonModule(ts_type.clone()),
                        from_path.range().clone(),
                        from_path.source(),
                    )),
                }
            } else {
                Ok((import_path::typecheck(import_path, ctx)?, None))
            }?;

            let name = match import_path.instance() {
                ASTImportPath::Simple(name) => name,
                ASTImportPath::Complex { head: _, member } => member,
            };

            ctx.set(name, import_path.ts_type().clone())
                .map_err(|error| {
                    TypecheckError::new(
                        TypecheckErrorKind::Context(error),
                        import_path.range().clone(),
                        import_path.source(),
                    )
                })?;

            Ok(StatementNode::new(
                ASTStatement::Import {
                    import_path,
                    from_path,
                },
                TSType::Unit,
                statement.source(),
                statement.range().clone(),
            ))
        }
        CSTStatement::Expression {
            expression,
            semicolon: _,
        } => {
            let expression = expression::typecheck(expression, ctx)?;
            Ok(StatementNode::new(
                ASTStatement::Expression(expression),
                TSType::Unit,
                statement.source(),
                statement.range().clone(),
            ))
        }
    }
}
