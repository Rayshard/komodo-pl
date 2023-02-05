pub mod error;
pub mod state;

use crate::compiler::{
    cst::{expression::Expression, script::Script, statement::Statement},
    lexer::token::{Token, TokenKind},
    utilities::text_source::TextSource,
};

use error::ParseError;
use state::ParseState;

use super::cst::{
    expression::{
        binary::Binary,
        binary_operator::{BinaryOperator, BinaryOperatorKind},
        call::Call,
        identifier::Identifier,
        literal::{Literal, LiteralKind},
        member_access::MemberAccess,
        parenthesized::Parenthesized,
        unary_operator::UnaryOperator,
    },
    statement::{import::Import, import_path::ImportPath, StatementKind},
};

pub type ParseResult<'tokens, 'source, T> =
    Result<(T, ParseState<'tokens, 'source>), (ParseError<'source>, ParseState<'tokens, 'source>)>;

type Parser<'tokens, 'source, T> =
    fn(ParseState<'tokens, 'source>) -> ParseResult<'tokens, 'source, T>;

fn longest<'tokens, 'source, T>(
    state: ParseState<'tokens, 'source>,
    parsers: &[fn(ParseState<'tokens, 'source>) -> ParseResult<'tokens, 'source, T>],
    multi_error_message: String,
) -> ParseResult<'tokens, 'source, T> {
    let mut longest_success: Option<(T, ParseState)> = None;
    let mut longest_error: (ParseError, ParseState) = (
        ParseError::new(
            multi_error_message.clone(),
            state.current_token().location().clone(),
        ),
        state.clone(),
    );

    for parser in parsers {
        match parser(state.clone()) {
            Ok(result)
                if longest_success.is_none()
                    || result.1.offset() > longest_success.as_ref().unwrap().1.offset() =>
            {
                longest_success = Some(result);
            }
            Err(result) if result.1.offset() > longest_error.1.offset() => longest_error = result,
            Err(result) if result.1.offset() == longest_error.1.offset() => {
                // longest_error = (
                //     ParseError::new(
                //         multi_error_message.clone(),
                //         longest_error.0.location().clone(),
                //     ),
                //     longest_error.1,
                // )
                todo!()
            }
            _ => continue,
        }
    }

    match longest_success {
        Some(success) if success.1.offset() >= longest_error.1.offset() => Ok(success),
        _ => Err(longest_error),
    }
}

fn separated<'tokens, 'source, V, D>(
    state: ParseState<'tokens, 'source>,
    value_parser: Parser<'tokens, 'source, V>,
    delimiter_parser: Parser<'tokens, 'source, D>,
) -> ParseResult<'tokens, 'source, Vec<V>> {
    if let Ok((value, mut state)) = value_parser(state.clone()) {
        let mut values = vec![value];

        while let Ok((_, next_state)) = delimiter_parser(state.clone()) {
            let (value, next_state) = value_parser(next_state)?;

            values.push(value);
            state = next_state;
        }

        Ok((values, state))
    } else {
        Ok((vec![], state))
    }
}

fn expect_token<'tokens, 'source>(
    kind: TokenKind,
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Token<'source>> {
    let token = state.current_token();

    if token.kind() == &kind {
        Ok((token.clone(), state.next()))
    } else {
        Err((
            ParseError::new(
                format!("Expected {kind:?}, but found {:?}", token.kind()),
                token.location().clone(),
            ),
            state,
        ))
    }
}

pub fn skip_whitespace<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseState<'tokens, 'source> {
    let mut state = state;

    while state.current_token().is_whitespace() {
        state = state.next();
    }

    state
}

pub fn parse_binary_operator<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, BinaryOperator<'source>> {
    longest(
        state,
        &[
            |state| {
                expect_token(TokenKind::SymbolPlus, state).map(|(token, state)| {
                    (
                        BinaryOperator::new(BinaryOperatorKind::Add, token.clone()),
                        state,
                    )
                })
            },
            |state| {
                expect_token(TokenKind::SymbolHyphen, state).map(|(token, state)| {
                    (
                        BinaryOperator::new(BinaryOperatorKind::Subtract, token.clone()),
                        state,
                    )
                })
            },
            |state| {
                expect_token(TokenKind::SymbolAsterisk, state).map(|(token, state)| {
                    (
                        BinaryOperator::new(BinaryOperatorKind::Multiply, token.clone()),
                        state,
                    )
                })
            },
            |state| {
                expect_token(TokenKind::SymbolForwardSlash, state).map(|(token, state)| {
                    (
                        BinaryOperator::new(BinaryOperatorKind::Divide, token.clone()),
                        state,
                    )
                })
            },
        ],
        "Expected a binary operator".to_string(),
    )
}

pub fn parse_postfix_unary_operator<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, UnaryOperator> {
    longest(state, &[], "Expected a post-fix unary operator".to_string())
}

pub fn parse_primary_expression<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    // TODO: make this static
    let atoms = &[
        parse_integer_literal_expression as Parser<'tokens, 'source, Expression<'source>>,
        parse_string_literal_expression as Parser<'tokens, 'source, Expression<'source>>,
        parse_parenthesized_expression as Parser<'tokens, 'source, Expression<'source>>,
        |state| {
            parse_identifier(state)
                .map(|(identifier, state)| (Expression::Identifier(identifier), state))
        },
    ];

    let (mut expression, mut state) = longest(state, atoms, "Expected an expression".to_string())?;

    loop {
        let token = state.current_token();

        match token.kind() {
            TokenKind::SymbolPeriod => {
                let (member, next_state) = expect_token(TokenKind::Identifier, state.next())?;
                expression =
                    Expression::MemberAccess(MemberAccess::new(expression, token.clone(), member));
                state = next_state;
            }
            TokenKind::SymbolOpenParenthesis => {
                let (args, next_state) = separated(state.next(), parse_expression, |state| {
                    expect_token(TokenKind::SymbolComma, state)
                })?;
                let (close_parenthesis, next_state) =
                    expect_token(TokenKind::SymbolCloseParenthesis, next_state)?;

                expression = Expression::Call(Call::new(
                    expression,
                    token.clone(),
                    args,
                    close_parenthesis,
                ));

                state = next_state;
            }
            _ => break,
        }
    }

    Ok((expression, state))
}

pub fn parse_expression_at_precedence<'tokens, 'source>(
    minimum_precedence: u32,
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    let (mut expression, mut state) = parse_primary_expression(state)?;

    while let Ok((binop, next_state)) = parse_binary_operator(skip_whitespace(state.clone())) {
        if binop.precedence() < minimum_precedence {
            break;
        }

        let next_minimum_precedence = if binop.is_right_associative() {
            binop.precedence()
        } else {
            binop.precedence() + 1
        };

        let (rhs, next_state) =
            parse_expression_at_precedence(next_minimum_precedence, skip_whitespace(next_state))?;

        expression = Expression::Binary(Binary::new(expression, binop, rhs));
        state = next_state;
    }

    Ok((expression, state))
}

pub fn parse_expression<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    parse_expression_at_precedence(0, state)
}

pub fn parse_integer_literal_expression<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    let (token, state) = expect_token(TokenKind::IntegerLiteral, state)?;
    Ok((
        Expression::Literal(Literal::new(LiteralKind::Integer, token)),
        state,
    ))
}

pub fn parse_string_literal_expression<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    let (token, state) = expect_token(TokenKind::StringLiteral, state)?;
    Ok((
        Expression::Literal(Literal::new(LiteralKind::String, token)),
        state,
    ))
}

pub fn parse_identifier<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Identifier<'source>> {
    let (token, state) = expect_token(TokenKind::Identifier, state)?;
    Ok((token, state))
}

pub fn parse_parenthesized_expression<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    let (open_parenthesis, state) = expect_token(TokenKind::SymbolOpenParenthesis, state)?;
    let (expression, state) = parse_expression(skip_whitespace(state))?;
    let (close_parenthesis, state) =
        expect_token(TokenKind::SymbolCloseParenthesis, skip_whitespace(state))?;

    Ok((
        Expression::Parenthesized(Parenthesized::new(
            open_parenthesis,
            expression,
            close_parenthesis,
        )),
        state,
    ))
}

pub fn parse_member_access<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, MemberAccess<'source>> {
    let (root, state) = parse_primary_expression(state)?;
    let (dot, state) = expect_token(TokenKind::SymbolPeriod, skip_whitespace(state))?;
    let (member, state) = parse_identifier(state)?;
    let (mut member_access, mut state) = (MemberAccess::new(root, dot, member), state);

    while let Ok((dot, next_state)) =
        expect_token(TokenKind::SymbolPeriod, skip_whitespace(state.clone()))
    {
        let (member, next_state) =
            expect_token(TokenKind::Identifier, skip_whitespace(next_state))?;

        member_access = MemberAccess::new(Expression::MemberAccess(member_access), dot, member);
        state = next_state;
    }

    Ok((member_access, state))
}

pub fn parse_import_path<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, ImportPath<'source>> {
    // TODO: make static
    let kinds = &[
        |state| {
            parse_identifier(state)
                .map(|(identifier, state)| (ImportPath::Simple(identifier), state))
        },
        |state| {
            parse_member_access(state)
                .map(|(member_access, state)| (ImportPath::Complex(member_access), state))
        },
    ];

    longest(state, kinds, "Expected an import path".to_string())
}

pub fn parse_import<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Import<'source>> {
    let (keyword_import, state) = expect_token(TokenKind::KeywordImport, state)?;
    let (import_path, state) = parse_import_path(skip_whitespace(state))?;
    let (from_path, state) = if let Ok((keyword_from, state)) =
        expect_token(TokenKind::KeywordFrom, skip_whitespace(state.clone()))
    {
        let (path, state) = parse_import_path(skip_whitespace(state))?;
        (Some((keyword_from, path)), state)
    } else {
        (None, state)
    };

    Ok((Import::new(keyword_import, import_path, from_path), state))
}

pub fn parse_import_statement<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Statement<'source>> {
    let (import, state) = parse_import(state)?;
    let (semicolon, state) = expect_token(TokenKind::SymbolSemicolon, skip_whitespace(state))?;

    ParseResult::Ok((
        Statement::new(StatementKind::Import(import), semicolon),
        state,
    ))
}

pub fn parse_expression_statement<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Statement<'source>> {
    let (expression, state) = parse_expression(state)?;
    let (semicolon, state) = expect_token(TokenKind::SymbolSemicolon, skip_whitespace(state))?;

    ParseResult::Ok((
        Statement::new(StatementKind::Expression(expression), semicolon),
        state,
    ))
}

pub fn parse_statement<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Statement<'source>> {
    // TODO: make static
    let parsers: &[fn(
        ParseState<'tokens, 'source>,
    ) -> ParseResult<'tokens, 'source, Statement<'source>>] = &[
        parse_expression_statement as Parser<'tokens, 'source, Statement<'source>>,
        parse_import_statement as Parser<'tokens, 'source, Statement<'source>>,
    ];

    longest(state, parsers, "Expected a statement".to_string())
}

pub fn parse_script<'source, 'tokens>(
    source: &'source TextSource,
    tokens: &'tokens [Token<'source>],
) -> Result<Script<'source>, ParseError<'source>> {
    let mut state = skip_whitespace(ParseState::new(&tokens));
    let mut statements = Vec::<Statement>::new();

    while !state.current_token().is_eof() {
        let (statement, next_state) = parse_statement(state).map_err(|(error, _)| error)?;

        statements.push(statement);
        state = skip_whitespace(next_state);
    }

    Ok(Script::new(source, statements))
}
