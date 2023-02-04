use crate::compiler::{
    ast::{
        statement::{
            import::Import as ASTImport, import_path::ImportPath as ASTImportPath,
            Statement as ASTStatement,
        },
        Node,
    },
    cst::statement::{
        import::Import as CSTImport, import_path::ImportPath as CSTImportPath,
        Statement as CSTStatement, StatementKind as CSTStatementKind,
    },
    typesystem::{context::Context, ts_type::TSType},
};

use super::{
    expression::{self, typecheck_identifier, typecheck_member_access},
    result::{TypecheckError, TypecheckErrorKind, TypecheckResult},
};

pub fn typecheck_import_path<'source>(
    node: &CSTImportPath<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, ASTImportPath<'source>> {
    match node {
        CSTImportPath::Simple(identifier) => Ok(ASTImportPath::Simple(typecheck_identifier(
            identifier, ctx,
        )?)),
        CSTImportPath::Complex(member_access) => Ok(ASTImportPath::Complex(
            typecheck_member_access(member_access, ctx)?,
        )),
    }
}

pub fn typecheck_import<'source>(
    node: &CSTImport<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, ASTImport<'source>> {
    let (import_path, from_path) = if let Some((_, from_path)) = node.from_path() {
        let from_path = typecheck_import_path(from_path, ctx)?;

        match from_path.ts_type() {
            TSType::Module {
                name: _,
                members: _,
            } => {
                let mut import_ctx = Context::from(from_path.ts_type(), None).map_err(|error| {
                    TypecheckError::new(
                        TypecheckErrorKind::Context(error),
                        from_path.range().clone(),
                        from_path.source(),
                    )
                })?;

                Ok((
                    typecheck_import_path(node.import_path(), &mut import_ctx)?,
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
        Ok((typecheck_import_path(node.import_path(), ctx)?, None))
    }?;

    let name = match import_path {
        ASTImportPath::Simple(identifier) => identifier.value(),
        ASTImportPath::Complex(member_access) => member_access.member().value(),
    };

    ctx.set(name, import_path.ts_type().clone())
        .map_err(|error| {
            TypecheckError::new(
                TypecheckErrorKind::Context(error),
                import_path.range().clone(),
                import_path.source(),
            )
        })?;

    Ok(ASTImport {
        path: import_path,
        from: from_path,
    })
}

pub fn typecheck<'source>(
    statement: &CSTStatement<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, ASTStatement<'source>> {
    match statement.kind() {
        CSTStatementKind::Import(node) => {
            Ok(ASTStatement::Import(typecheck_import(node, ctx)?))
        }
        CSTStatementKind::Expression(node) => {
            Ok(ASTStatement::Expression(expression::typecheck(node, ctx)?))
        }
    }
}
