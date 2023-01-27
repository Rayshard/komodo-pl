use super::{
    cst::{BinaryOperator, BinaryOperatorKind, Expression, Module, Statement},
    lexing::token::{Token, TokenKind},
    utilities::range::Range,
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
            Err(result) if result.1.offset == longest_error.1.offset => longest_error = (
                ParseError {
                    range: longest_error.0.range,
                    message: multi_error_message.clone(),
                },
                longest_error.1,
            ),
            _ => continue,
        }
    }

    match longest_success {
        Some(success) if success.1.offset >= longest_error.1.offset => Ok(success),
        _ => Err(longest_error)
    }
}

fn expect_token(kind: TokenKind) -> Box<dyn Fn(ParseState) -> ParseResult<&Token>> {
    Box::new(move |state| {
        let token = state.current_token();

        if token.kind == kind {
            Ok((token, state.next()))
        } else {
            Err((
                ParseError {
                    range: token.range.clone(),
                    message: format!("Expected {kind:?}, but found {:?}", token.kind),
                },
                state,
            ))
        }
    })
}

pub fn skip_whitespace<'a>(state: &ParseState<'a>) -> ParseState<'a> {
    let mut state = state.clone();

    while state.current_token().kind == TokenKind::Whitespace {
        state = state.next();
    }

    state
}

pub fn parse_binary_operator(state: ParseState) -> ParseResult<BinaryOperator> {
    let token = state.current_token();
    let kind = match token.kind {
        TokenKind::SymbolPlus => BinaryOperatorKind::Add,
        TokenKind::SymbolHyphen => BinaryOperatorKind::Subtract,
        TokenKind::SymbolAsterisk => BinaryOperatorKind::Multiply,
        TokenKind::SymbolForwardSlash => BinaryOperatorKind::Divide,
        _ => {
            return Err((
                ParseError {
                    range: token.range.clone(),
                    message: format!("Expected a binary operator, but found {:?}", token.kind),
                },
                state,
            ))
        }
    };

    Ok((BinaryOperator::new(kind, token), state.next()))
}

pub fn parse_expression_at_precedence<'a>(
    minimum_precedence: u32,
    state: ParseState<'a>,
) -> ParseResult<Expression<'a>> {
    let atoms: &[fn(ParseState) -> ParseResult<Expression>] =
        &[parse_integer_literal, parse_parenthesized_expression];

    let (mut expression, mut state) = longest(state, atoms, "Expected an expression".to_string())?;

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

pub fn parse_expression<'a>(state: ParseState<'a>) -> ParseResult<Expression<'a>> {
    parse_expression_at_precedence(0, state)
}

pub fn parse_integer_literal<'a>(state: ParseState<'a>) -> ParseResult<'a, Expression<'a>> {
    let (token, state) = expect_token(TokenKind::IntegerLiteral)(state)?;
    Ok((Expression::IntegerLiteral(token), state))
}

pub fn parse_parenthesized_expression<'a>(
    state: ParseState<'a>,
) -> ParseResult<'a, Expression<'a>> {
    let (open_parenthesis, state) = expect_token(TokenKind::SymbolOpenParenthesis)(state)?;
    let (expression, state) = parse_expression(state)?;
    let (close_parenthesis, state) = expect_token(TokenKind::SymbolCloseParenthesis)(state)?;

    Ok((
        Expression::Parenthesized {
            open_parenthesis,
            expression: Box::new(expression),
            close_parenthesis,
        },
        state,
    ))
}

pub fn parse_statement<'a>(state: ParseState<'a>) -> ParseResult<Statement<'a>> {
    let (expression, state) = parse_expression(state)?;
    let (semicolon, state) = expect_token(TokenKind::SymbolSemicolon)(skip_whitespace(&state))?;
    ParseResult::Ok((Statement::Expression(expression, semicolon), state))
}

pub fn parse_module(tokens: &[Token]) -> ParseResult<Module> {
    let mut state = skip_whitespace(&ParseState::new(tokens));
    let mut statements = Vec::<Statement>::new();

    while state.current_token().kind != TokenKind::EOF {
        let (statement, next_state) = parse_statement(state)?;

        statements.push(statement);
        state = skip_whitespace(&next_state);
    }

    Ok((Module::new(statements), state))
}
