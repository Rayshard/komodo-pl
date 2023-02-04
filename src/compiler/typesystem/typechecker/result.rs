use crate::compiler::{typesystem::{context::ContextError, ts_type::TSType}, cst::binary_operator::BinaryOperatorKind, utilities::{range::Range, text_source::TextSource}};

pub enum TypecheckErrorKind {
    Context(ContextError),
    IntegerOverflow,
    IncompatibleOperandsForBinaryOperator {
        left: TSType,
        op: BinaryOperatorKind,
        right: TSType,
    },
    ImportFromNonModule(TSType),
    TypeIsNotCallable(TSType),
    Unexpected {
        expected: TSType,
        found: TSType,
    },
    NotEnoughArguments {
        expected: usize,
        found: usize,
    },
}

pub struct TypecheckError<'source> {
    kind: TypecheckErrorKind,
    range: Range,
    source: &'source TextSource,
}

impl<'source> TypecheckError<'source> {
    pub fn new(
        kind: TypecheckErrorKind,
        range: Range,
        source: &'source TextSource,
    ) -> TypecheckError<'source> {
        TypecheckError {
            kind,
            range,
            source,
        }
    }
}

impl<'source> ToString for TypecheckError<'source> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.source.get_terminal_link(self.range.start()).unwrap(),
            match &self.kind {
                TypecheckErrorKind::Context(error) => error.clone(),
                TypecheckErrorKind::IntegerOverflow =>
                    "Value outside of range for Int64".to_string(),
                TypecheckErrorKind::IncompatibleOperandsForBinaryOperator { left, op, right } =>
                    format!("Unable to apply operator {op:?} to {left:?} and {right:?}"),
                TypecheckErrorKind::ImportFromNonModule(ts_type) =>
                    format!("Expected module but found {ts_type:?}"),
                TypecheckErrorKind::TypeIsNotCallable(ts_type) =>
                    format!("Type {ts_type:?} is not callable"),
                TypecheckErrorKind::Unexpected { expected, found } =>
                    format!("Expected {expected:?} but found {found:?}"),
                TypecheckErrorKind::NotEnoughArguments { expected, found } =>
                    format!("Expected {expected} arguments but found {found}."),
            }
        )
    }
}

pub type TypecheckResult<'source, T> = Result<T, TypecheckError<'source>>;
