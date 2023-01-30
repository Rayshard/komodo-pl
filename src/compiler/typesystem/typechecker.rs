use crate::compiler::{
    ast::{
        expression::Expression as ASTExpression, literal::Literal as ASTLiteral,
        script::Script as ASTScript, statement::Statement as ASTStatement, Node as ASTNode,
    },
    parsing::cst::{
        expression::Expression as CSTExpression, script::Script as CSTScript,
        statement::Statement as CSTStatement, Node as CSTNode,
    },
    typesystem::ts_type::TSType,
    utilities::{range::Range, text_source::TextSource},
};

#[derive(Debug)]
pub struct TypecheckError<'a> {
    range: Range,
    message: String,
    source: &'a TextSource,
}

impl<'a> ToString for TypecheckError<'a> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.source.get_terminal_link(self.range.start()).unwrap(),
            self.message
        )
    }
}

pub type TypecheckResult<'a, T> = Result<ASTNode<'a, T>, TypecheckError<'a>>;

pub fn typecheck_expression<'a>(
    expression: &CSTExpression<'a>,
) -> TypecheckResult<'a, ASTExpression<'a>> {
    match expression {
        CSTExpression::IntegerLiteral(token) => match token.value().parse::<i64>() {
            Ok(value) => Ok(ASTNode::new(
                ASTExpression::Literal(ASTNode::new(
                    ASTLiteral::Int64(value),
                    TSType::Int64,
                    expression.source(),
                )),
                TSType::Int64,
                expression.source(),
            )),
            Err(error) => Err(TypecheckError {
                range: expression.range(),
                message: match error.kind() {
                    std::num::IntErrorKind::PosOverflow | std::num::IntErrorKind::NegOverflow => {
                        "Value outside of range for Int64".to_string()
                    }
                    error => panic!("Unexcpected error on {}: {error:?}", token.value()),
                },
                source: expression.source(),
            }),
        },
        CSTExpression::StringLiteral(_) => todo!(),
        CSTExpression::Identifier(_) => todo!(),
        CSTExpression::MemberAccess {
            head: _,
            dot: _,
            member: _,
        } => todo!(),
        CSTExpression::Call {
            head: _,
            open_parenthesis: _,
            arg: _,
            close_parenthesis: _,
        } => todo!(),
        CSTExpression::Unary { operand: _, op: _ } => todo!(),
        CSTExpression::Binary { left, op, right } => {
            let left = typecheck_expression(left.as_ref())?;
            let right = typecheck_expression(right.as_ref())?;

            match (left.ts_type(), op.kind(), right.ts_type()) {
                (TSType::Int64, op, TSType::Int64) => Ok(ASTNode::new(
                    ASTExpression::Binary {
                        left: Box::new(left),
                        op: op.clone(),
                        right: Box::new(right),
                    },
                    TSType::Int64,
                    expression.source()
                )),
                (left, op, right) => Err(TypecheckError {
                    range: expression.range(),
                    message: format!("Unable to apply operator {op:?} to {left:?} and {right:?}"),
                    source: expression.source(),
                }),
            }
        }
        CSTExpression::Parenthesized {
            open_parenthesis: _,
            expression,
            close_parenthesis: _,
        } => typecheck_expression(expression),
    }
}

pub fn typecheck_statement<'a>(
    statement: &CSTStatement<'a>,
) -> TypecheckResult<'a, ASTStatement<'a>> {
    match statement {
        CSTStatement::Import {
            keyword_import: _,
            import_path: _,
            from_path: _,
            semicolon: _,
        } => todo!(),
        CSTStatement::Expression {
            expression,
            semicolon: _,
        } => {
            let expression = typecheck_expression(expression)?;
            Ok(ASTNode::new(
                ASTStatement::Expression(expression),
                TSType::Unit,
                statement.source()
            ))
        }
    }
}

pub fn typecheck_script<'a>(script: CSTScript<'a>) -> TypecheckResult<'a, ASTScript<'a>> {
    let mut statements = vec![];

    for statement in script.statements() {
        let statement = typecheck_statement(statement)?;
        statements.push(statement);
    }

    let ts_type = if let Some(statement) = statements.last() {
        statement.ts_type().clone()
    } else {
        TSType::Unit
    };

    Ok(ASTNode::new(ASTScript::new(statements), ts_type, script.source()))
}
