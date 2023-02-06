use crate::compiler::{
    ast::{
        statement::{
            import::Import as ASTImport, import_path::ImportPath as ASTImportPath,
            Statement as ASTStatement,
        },
        Node,
    },
    cst::statement::{
        import::Import as CSTImport,
        import_path::{ImportPath as CSTImportPath, ImportPathKind as CSTImportPathKind},
        Statement as CSTStatement, StatementKind as CSTStatementKind,
    },
    typesystem::{context::Context, ts_type::TSType},
};

use super::{
    expression::{self, typecheck_identifier},
    result::{TypecheckError, TypecheckErrorKind, TypecheckResult},
};

pub fn typecheck_import_path<'source>(
    node: &CSTImportPath<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, ASTImportPath<'source>> {
    match node.kind() {
        CSTImportPathKind::Simple(identifier) => Ok(ASTImportPath::Simple(typecheck_identifier(
            identifier, ctx,
        )?)),
        CSTImportPathKind::Complex {
            root,
            dot: _,
            member,
        } => {
            let root = typecheck_import_path(root.as_ref(), ctx)?;
            let root_ctx =
                Context::from(root.ts_type(), None, root.location()).map_err(|error| {
                    let location = error.location().clone();
                    TypecheckError::new(TypecheckErrorKind::Context(error), location)
                })?;
            let member = typecheck_identifier(member, &root_ctx)?;

            Ok(ASTImportPath::Complex {
                root: Box::new(root),
                member,
            })
        }
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
                let mut import_ctx = Context::from(from_path.ts_type(), None, from_path.location())
                    .map_err(|error| {
                        let location = error.location().clone();
                        TypecheckError::new(TypecheckErrorKind::Context(error), location)
                    })?;

                Ok((
                    typecheck_import_path(node.import_path(), &mut import_ctx)?,
                    Some(from_path),
                ))
            }
            ts_type => Err(TypecheckError::new(
                TypecheckErrorKind::ImportFromNonModule(ts_type.clone()),
                from_path.location().clone(),
            )),
        }
    } else {
        Ok((typecheck_import_path(node.import_path(), ctx)?, None))
    }?;

    let name = match &import_path {
        ASTImportPath::Simple(node) => node.value(),
        ASTImportPath::Complex { root: _, member } => member.value(),
    };

    ctx.set(name, import_path.ts_type().clone(), import_path.location())
        .map_err(|error| {
            let location = error.location().clone();
            TypecheckError::new(TypecheckErrorKind::Context(error), location)
        })?;

    Ok(ASTImport::new(import_path, from_path))
}

pub fn typecheck<'source>(
    statement: &CSTStatement<'source>,
    ctx: &mut Context,
) -> TypecheckResult<'source, ASTStatement<'source>> {
    match statement.kind() {
        CSTStatementKind::Import(node) => Ok(ASTStatement::Import(typecheck_import(node, ctx)?)),
        CSTStatementKind::Expression(node) => {
            Ok(ASTStatement::Expression(expression::typecheck(node, ctx)?))
        }
    }
}
