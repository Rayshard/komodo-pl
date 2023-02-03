use crate::compiler::{
    lexing::token::{Token, TokenKind},
    utilities::{range::Range, text_source::TextSource},
};

use super::{
    cst::{
        binary_operator::{BinaryOperator, BinaryOperatorKind},
        expression::Expression,
        script::Script,
        statement::{ImportPath, Statement},
        unary_operator::UnaryOperator,
    },
    parse_state::ParseState,
};

pub struct ParseError<'source> {
    range: Range,
    message: String,
    source: &'source TextSource,
}

impl<'source> ToString for ParseError<'source> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.source.get_terminal_link(self.range.start()).unwrap(),
            self.message
        )
    }
}

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
        ParseError {
            range: state.current_token().range().clone(),
            message: multi_error_message.clone(),
            source: state.current_token().source(),
        },
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
                longest_error = (
                    ParseError {
                        range: longest_error.0.range,
                        message: multi_error_message.clone(),
                        source: longest_error.0.source,
                    },
                    longest_error.1,
                )
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
    value_parser: fn(ParseState<'tokens, 'source>) -> ParseResult<'tokens, 'source, V>,
    delimiter_parser: fn(ParseState<'tokens, 'source>) -> ParseResult<'tokens, 'source, D>,
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
            ParseError {
                range: token.range().clone(),
                message: format!("Expected {kind:?}, but found {:?}", token.kind()),
                source: token.source(),
            },
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
    let atoms = &[
        parse_integer_literal_expression as Parser<'tokens, 'source, Expression<'source>>,
        parse_string_literal_expression as Parser<'tokens, 'source, Expression<'source>>,
        parse_parenthesized_expression as Parser<'tokens, 'source, Expression<'source>>,
        parse_identifier_expression as Parser<'tokens, 'source, Expression<'source>>,
    ];

    let (mut expression, mut state) = longest(state, atoms, "Expected an expression".to_string())?;

    loop {
        let token = state.current_token();

        match token.kind() {
            TokenKind::SymbolPeriod => {
                let (member, next_state) = expect_token(TokenKind::Identifier, state.next())?;
                expression = Expression::MemberAccess {
                    head: Box::new(expression),
                    dot: token.clone(),
                    member,
                };

                state = next_state;
            }
            TokenKind::SymbolOpenParenthesis => {
                let (args, next_state) = separated(state.next(), parse_expression, |state| {
                    expect_token(TokenKind::SymbolComma, state)
                })?;
                let (close_parenthesis, next_state) =
                    expect_token(TokenKind::SymbolCloseParenthesis, next_state)?;

                expression = Expression::Call {
                    head: Box::new(expression),
                    open_parenthesis: token.clone(),
                    args,
                    close_parenthesis,
                };

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

        expression = Expression::Binary {
            left: Box::new(expression),
            op: binop,
            right: Box::new(rhs),
        };

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
    Ok((Expression::IntegerLiteral(token), state))
}

pub fn parse_string_literal_expression<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    let (token, state) = expect_token(TokenKind::StringLiteral, state)?;
    Ok((Expression::StringLiteral(token), state))
}

pub fn parse_identifier_expression<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    let (token, state) = expect_token(TokenKind::Identifier, state)?;
    Ok((Expression::Identifier(token), state))
}

pub fn parse_parenthesized_expression<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Expression<'source>> {
    let (open_parenthesis, state) = expect_token(TokenKind::SymbolOpenParenthesis, state)?;
    let (expression, state) = parse_expression(skip_whitespace(state))?;
    let (close_parenthesis, state) =
        expect_token(TokenKind::SymbolCloseParenthesis, skip_whitespace(state))?;

    Ok((
        Expression::Parenthesized {
            open_parenthesis,
            expression: Box::new(expression),
            close_parenthesis,
        },
        state,
    ))
}

pub fn parse_import_path<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, ImportPath<'source>> {
    let (mut path, mut state) = expect_token(TokenKind::Identifier, state)
        .map(|(token, state)| (ImportPath::Simple(token), state))?;

    while let Ok((dot, next_state)) =
        expect_token(TokenKind::SymbolPeriod, skip_whitespace(state.clone()))
    {
        let (member, next_state) =
            expect_token(TokenKind::Identifier, skip_whitespace(next_state))?;

        path = ImportPath::Complex {
            head: Box::new(path),
            dot,
            member,
        };
        state = next_state;
    }

    Ok((path, state))
}

pub fn parse_import_statement<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Statement<'source>> {
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
    let (semicolon, state) = expect_token(TokenKind::SymbolSemicolon, skip_whitespace(state))?;

    Ok((
        Statement::Import {
            keyword_import,
            import_path,
            from_path,
            semicolon,
        },
        state,
    ))
}

pub fn parse_expression_statement<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Statement<'source>> {
    let (expression, state) = parse_expression(state)?;
    let (semicolon, state) = expect_token(TokenKind::SymbolSemicolon, skip_whitespace(state))?;

    ParseResult::Ok((
        Statement::Expression {
            expression,
            semicolon,
        },
        state,
    ))
}

pub fn parse_statement<'tokens, 'source>(
    state: ParseState<'tokens, 'source>,
) -> ParseResult<'tokens, 'source, Statement<'source>> {
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
