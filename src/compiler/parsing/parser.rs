use crate::compiler::{
    lexing::token::{Token, TokenKind},
    utilities::{range::Range, text_source::TextSource},
};

use super::cst::{
    binary_operator::{BinaryOperator, BinaryOperatorKind},
    expression::Expression,
    script::Script,
    statement::{ImportPath, Statement},
    unary_operator::UnaryOperator,
};

#[derive(Clone)]
pub struct ParseState<'a, 'b> {
    tokens: &'a [Token<'b>],
    offset: usize,
}

impl<'a, 'b> ParseState<'a, 'b> {
    fn new(tokens: &'a [Token<'b>]) -> ParseState<'a, 'b> {
        if let Some(token) = tokens.last() {
            if !token.is_eof() {
                panic!("Invalid tokens. The last token of the input tokens should be an EOF token.")
            }

            return ParseState { tokens, offset: 0 };
        } else {
            panic!(
                "Invalid tokens. Input tokens must have at least one token (which should be EOF)."
            )
        }
    }

    fn current_token(&self) -> &'a Token<'b> {
        &self.tokens[self.offset]
    }

    fn next(&self) -> ParseState<'a, 'b> {
        if self.offset == self.tokens.len() {
            panic!("Unable to create new parser state because there are no more tokens to read.")
        }

        ParseState {
            tokens: self.tokens,
            offset: self.offset + 1,
        }
    }
}

pub struct ParseError<'a> {
    range: Range,
    message: String,
    source: &'a TextSource,
}

impl<'a> ToString for ParseError<'a> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.source.get_terminal_link(self.range.start()).unwrap(),
            self.message
        )
    }
}

pub type ParseResult<'a, 'b, T> =
    Result<(T, ParseState<'a, 'b>), (ParseError<'b>, ParseState<'a, 'b>)>;

type Parser<'a, 'b, T> = fn(ParseState<'a, 'b>) -> ParseResult<'a, 'b, T>;

fn longest<'a, 'b, T>(
    state: ParseState<'a, 'b>,
    parsers: &[fn(ParseState<'a, 'b>) -> ParseResult<'a, 'b, T>],
    multi_error_message: String,
) -> ParseResult<'a, 'b, T> {
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
                    || result.1.offset > longest_success.as_ref().unwrap().1.offset =>
            {
                longest_success = Some(result);
            }
            Err(result) if result.1.offset > longest_error.1.offset => longest_error = result,
            Err(result) if result.1.offset == longest_error.1.offset => {
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
        Some(success) if success.1.offset >= longest_error.1.offset => Ok(success),
        _ => Err(longest_error),
    }
}

fn expect_token<'a, 'b>(
    kind: TokenKind,
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Token<'b>> {
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

pub fn skip_whitespace<'a, 'b>(state: ParseState<'a, 'b>) -> ParseState<'a, 'b> {
    let mut state = state;

    while state.current_token().is_whitespace() {
        state = state.next();
    }

    state
}

pub fn parse_binary_operator<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, BinaryOperator<'b>> {
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

pub fn parse_postfix_unary_operator<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, UnaryOperator> {
    longest(state, &[], "Expected a post-fix unary operator".to_string())
}

pub fn parse_primary_expression<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Expression<'b>> {
    let atoms = &[
        parse_integer_literal_expression as Parser<'a, 'b, Expression<'b>>,
        parse_string_literal_expression as Parser<'a, 'b, Expression<'b>>,
        parse_parenthesized_expression as Parser<'a, 'b, Expression<'b>>,
        parse_identifier_expression as Parser<'a, 'b, Expression<'b>>,
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
                let (arg, next_state) = parse_expression(state.next())?;
                let (close_parenthesis, next_state) =
                    expect_token(TokenKind::SymbolCloseParenthesis, next_state)?;

                expression = Expression::Call {
                    head: Box::new(expression),
                    open_parenthesis: token.clone(),
                    arg: Box::new(arg),
                    close_parenthesis,
                };

                state = next_state;
            }
            _ => break,
        }
    }

    Ok((expression, state))
}

pub fn parse_expression_at_precedence<'a, 'b>(
    minimum_precedence: u32,
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Expression<'b>> {
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

pub fn parse_expression<'a, 'b>(state: ParseState<'a, 'b>) -> ParseResult<'a, 'b, Expression<'b>> {
    parse_expression_at_precedence(0, state)
}

pub fn parse_integer_literal_expression<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Expression<'b>> {
    let (token, state) = expect_token(TokenKind::IntegerLiteral, state)?;
    Ok((Expression::IntegerLiteral(token), state))
}

pub fn parse_string_literal_expression<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Expression<'b>> {
    let (token, state) = expect_token(TokenKind::StringLiteral, state)?;
    Ok((Expression::StringLiteral(token), state))
}

pub fn parse_identifier_expression<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Expression<'b>> {
    let (token, state) = expect_token(TokenKind::Identifier, state)?;
    Ok((Expression::Identifier(token), state))
}

pub fn parse_parenthesized_expression<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Expression<'b>> {
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

pub fn parse_import_path<'a, 'b>(state: ParseState<'a, 'b>) -> ParseResult<'a, 'b, ImportPath<'b>> {
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

pub fn parse_import_statement<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Statement<'b>> {
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

pub fn parse_expression_statement<'a, 'b>(
    state: ParseState<'a, 'b>,
) -> ParseResult<'a, 'b, Statement<'b>> {
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

pub fn parse_statement<'a, 'b>(state: ParseState<'a, 'b>) -> ParseResult<'a, 'b, Statement<'b>> {
    let parsers: &[fn(ParseState<'a, 'b>) -> ParseResult<'a, 'b, Statement<'b>>] = &[
        parse_expression_statement as Parser<'a, 'b, Statement<'b>>,
        parse_import_statement as Parser<'a, 'b, Statement<'b>>,
    ];

    longest(state, parsers, "Expected a statement".to_string())
}

pub fn parse_script<'a, 'b>(
    source: &'a TextSource,
    tokens: &'b [Token<'a>],
) -> Result<Script<'a>, ParseError<'a>> {
    let mut state = skip_whitespace(ParseState::new(&tokens));
    let mut statements = Vec::<Statement>::new();

    while !state.current_token().is_eof() {
        let (statement, next_state) = parse_statement(state).map_err(|(error, _)| error)?;

        statements.push(statement);
        state = skip_whitespace(next_state);
    }

    Ok(Script::new(source, statements))
}
