use crate::compiler::{
    ast::{import_path::ImportPath as ASTImportPath, ImportPathNode},
    parsing::cst::{import_path::ImportPath as CSTImportPath, Node},
    typesystem::context::Context,
};

use super::result::{TypecheckError, TypecheckErrorKind, TypecheckResult};

pub fn typecheck<'source>(
    path: &CSTImportPath<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, ImportPathNode<'source>> {
    match path {
        CSTImportPath::Simple(token) => {
            let name = token.value();
            let ts_type = ctx.get(name).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    path.range().clone(),
                    path.source(),
                )
            })?;

            Ok(ImportPathNode::new(
                ASTImportPath::Simple(name.to_string()),
                ts_type.clone(),
                path.source(),
                path.range().clone(),
            ))
        }
        CSTImportPath::Complex {
            head,
            dot: _,
            member,
        } => {
            let head = typecheck(head, ctx)?;
            let import_ctx = Context::from(head.ts_type(), None).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    head.range().clone(),
                    head.source(),
                )
            })?;

            let name = member.value();
            let ts_type = import_ctx.get(name).map_err(|error| {
                TypecheckError::new(
                    TypecheckErrorKind::Context(error),
                    member.range().clone(),
                    member.source(),
                )
            })?;

            Ok(ImportPathNode::new(
                ASTImportPath::Complex {
                    head: Box::new(head),
                    member: name.to_string(),
                },
                ts_type.clone(),
                path.source(),
                path.range().clone(),
            ))
        }
    }
}
