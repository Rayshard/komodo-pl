use super::{
    cst::{
        BinaryOperator, BinaryOperatorKind, Expression, ImportPath, Script, Statement,
        UnaryOperator,
    },
    lexing::token::{Token, TokenKind},
    utilities::{range::Range, text_source::TextSource},
};

#[derive(Clone)]
pub struct ParseState<'a> {
    tokens: &'a [Token],
    offset: usize,
}

impl<'a> ParseState<'a> {
    fn new(tokens: &'a [Token]) -> ParseState<'a> {
        if let Some(token) = tokens.last() {
            if token.kind != TokenKind::EOF {
                panic!("Invalid tokens. The last token of the input tokens should be an EOF token.")
            }

            return ParseState { tokens, offset: 0 };
        } else {
            panic!(
                "Invalid tokens. Input tokens must have at least one token (which should be EOF)."
            )
        }
    }

    fn current_token(&self) -> &'a Token {
        &self.tokens[self.offset]
    }

    fn next(&self) -> ParseState<'a> {
        if self.offset == self.tokens.len() {
            panic!("Unable to create new parser state because there are no more tokens to read.")
        }

        ParseState {
            tokens: self.tokens,
            offset: self.offset + 1,
        }
    }
}

pub struct ParseError {
    pub range: Range,
    pub message: String,
}

impl ToString for ParseError {
    fn to_string(&self) -> String {
        self.message.clone()
    }
}

pub type ParseResult<'a, T> = Result<(T, ParseState<'a>), (ParseError, ParseState<'a>)>;

fn longest<'a, T>(
    state: ParseState<'a>,
    parsers: &[fn(ParseState<'a>) -> ParseResult<'a, T>],
    multi_error_message: String,
) -> ParseResult<'a, T> {
    let mut longest_success: Option<(T, ParseState)> = None;
    let mut longest_error: (ParseError, ParseState) = (
        ParseError {
            range: state.current_token().range.clone(),
            message: multi_error_message.clone(),
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

fn expect_token(kind: TokenKind, state: ParseState) -> ParseResult<Token> {
    let token = state.current_token();

    if token.kind == kind {
        Ok((token.clone(), state.next()))
    } else {
        Err((
            ParseError {
                range: token.range.clone(),
                message: format!("Expected {kind:?}, but found {:?}", token.kind),
            },
            state,
        ))
    }
}

pub fn skip_whitespace<'a>(state: &ParseState<'a>) -> ParseState<'a> {
    let mut state = state.clone();

    while state.current_token().kind == TokenKind::Whitespace {
        state = state.next();
    }

    state
}

pub fn parse_binary_operator(state: ParseState) -> ParseResult<BinaryOperator> {
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

pub fn parse_postfix_unary_operator(state: ParseState) -> ParseResult<UnaryOperator> {
    longest(state, &[], "Expected a post-fix unary operator".to_string())
}

pub fn parse_primary_expression(state: ParseState) -> ParseResult<Expression> {
    let atoms: &[fn(ParseState) -> ParseResult<Expression>] = &[
        parse_integer_literal_expression,
        parse_string_literal_expression,
        parse_parenthesized_expression,
        parse_identifier_expression,
    ];

    let (mut expression, mut state) = longest(state, atoms, "Expected an expression".to_string())?;

    loop {
        let token = state.current_token();

        match token.kind {
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

pub fn parse_expression_at_precedence<'a>(
    minimum_precedence: u32,
    state: ParseState<'a>,
) -> ParseResult<Expression> {
    let (mut expression, mut state) = parse_primary_expression(state)?;

    loop {
        if let Ok((binop, next_state)) = parse_binary_operator(skip_whitespace(&state)) {
            if binop.precedence() < minimum_precedence {
                break;
            }

            let next_minimum_precedence = if binop.is_right_associative() {
                binop.precedence()
            } else {
                binop.precedence() + 1
            };

            let (rhs, next_state) = parse_expression_at_precedence(
                next_minimum_precedence,
                skip_whitespace(&next_state),
            )?;

            expression = Expression::Binary {
                left: Box::new(expression),
                op: binop,
                right: Box::new(rhs),
            };

            state = next_state;
        } else {
            break;
        }
    }

    Ok((expression, state))
}

pub fn parse_expression<'a>(state: ParseState<'a>) -> ParseResult<Expression> {
    parse_expression_at_precedence(0, state)
}

pub fn parse_integer_literal_expression<'a>(state: ParseState<'a>) -> ParseResult<'a, Expression> {
    let (token, state) = expect_token(TokenKind::IntegerLiteral, state)?;
    Ok((Expression::IntegerLiteral(token), state))
}

pub fn parse_string_literal_expression<'a>(state: ParseState<'a>) -> ParseResult<'a, Expression> {
    let (token, state) = expect_token(TokenKind::StringLiteral, state)?;
    Ok((Expression::StringLiteral(token), state))
}

pub fn parse_identifier_expression<'a>(state: ParseState<'a>) -> ParseResult<'a, Expression> {
    let (token, state) = expect_token(TokenKind::Identifier, state)?;
    Ok((Expression::Identifier(token), state))
}

pub fn parse_parenthesized_expression<'a>(state: ParseState<'a>) -> ParseResult<'a, Expression> {
    let (open_parenthesis, state) = expect_token(TokenKind::SymbolOpenParenthesis, state)?;
    let (expression, state) = parse_expression(state)?;
    let (close_parenthesis, state) = expect_token(TokenKind::SymbolCloseParenthesis, state)?;

    Ok((
        Expression::Parenthesized {
            open_parenthesis,
            expression: Box::new(expression),
            close_parenthesis,
        },
        state,
    ))
}

pub fn parse_import_path<'a>(state: ParseState<'a>) -> ParseResult<'a, ImportPath> {
    let (mut path, mut state) = expect_token(TokenKind::Identifier, state)
        .map(|(token, state)| (ImportPath::Simple(token), state))?;

    while let Ok((dot, next_state)) = expect_token(TokenKind::SymbolPeriod, skip_whitespace(&state))
    {
        let (member, next_state) = expect_token(TokenKind::Identifier, next_state)?;

        path = ImportPath::Complex {
            head: Box::new(path),
            dot,
            member,
        };
        state = next_state;
    }

    Ok((path, state))
}

pub fn parse_import_statement<'a>(state: ParseState<'a>) -> ParseResult<'a, Statement> {
    let (keyword_import, state) = expect_token(TokenKind::KeywordImport, state)?;
    let (import_path, state) = parse_import_path(skip_whitespace(&state))?;
    let (from_path, state) = if let Ok((keyword_from, state)) =
        expect_token(TokenKind::KeywordFrom, skip_whitespace(&state))
    {
        let (path, state) = parse_import_path(skip_whitespace(&state))?;
        (Some((keyword_from, path)), state)
    } else {
        (None, state)
    };
    let (semicolon, state) = expect_token(TokenKind::SymbolSemicolon, skip_whitespace(&state))?;

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

pub fn parse_expression_statement(state: ParseState) -> ParseResult<Statement> {
    let (expression, state) = parse_expression(state)?;
    let (semicolon, state) = expect_token(TokenKind::SymbolSemicolon, skip_whitespace(&state))?;

    ParseResult::Ok((Statement::Expression(expression, semicolon), state))
}

pub fn parse_statement(state: ParseState) -> ParseResult<Statement> {
    let parsers: &[fn(ParseState) -> ParseResult<Statement>] =
        &[parse_expression_statement, parse_import_statement];

    longest(state, parsers, "Expected a statement".to_string())
}

pub fn parse_script(source: TextSource, tokens: &[Token]) -> ParseResult<Script> {
    let mut state = skip_whitespace(&ParseState::new(tokens));
    let mut statements = Vec::<Statement>::new();

    while state.current_token().kind != TokenKind::EOF {
        let (statement, next_state) = parse_statement(state)?;

        statements.push(statement);
        state = skip_whitespace(&next_state);
    }

    Ok((Script::new(source, statements), state))
}
