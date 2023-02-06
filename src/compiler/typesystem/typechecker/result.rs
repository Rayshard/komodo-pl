use crate::compiler::{
    cst::expression::binary_operator::BinaryOperatorKind,
    typesystem::{
        context::ContextError,
        ts_type::{Function as TSFunction, TSType},
    },
    utilities::location::Location,
};

pub enum TypecheckErrorKind<'source> {
    Context(ContextError<'source>),
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
    NoOverloadMatchesArguments {
        function: TSFunction,
        args: Vec<TSType>,
    },
}

pub struct TypecheckError<'source> {
    kind: TypecheckErrorKind<'source>,
    location: Location<'source>,
}

impl<'source> TypecheckError<'source> {
    pub fn new(kind: TypecheckErrorKind<'source>, location: Location<'source>) -> Self {
        Self { kind, location }
    }
}

impl<'source> ToString for TypecheckError<'source> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.location.start_terminal_link(),
            match &self.kind {
                TypecheckErrorKind::Context(error) => error.message().to_string(),
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
                TypecheckErrorKind::NoOverloadMatchesArguments { function, args } =>
                //TODO make prettier
                    format!("No overload for {} accepts the supplied argument list ({:?}). Available overloads are: {:?}", function.name(), args, function.overloads()),
            }
        )
    }
}

pub type TypecheckResult<'source, T> = Result<T, TypecheckError<'source>>;
