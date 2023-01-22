use crate::{
    cst::{Expression, Module},
    lexer::{Token, TokenKind},
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

#[derive(Clone)]
pub struct ParseError {
    pub range: Range,
    pub message: String,
}

pub type ParseResult<'a, T> = Result<(T, ParseState<'a>), (ParseError, ParseState<'a>)>;

fn longest<'a, T>(
    parsers: &[fn(ParseState<'a>) -> ParseResult<'a, T>],
    state: ParseState<'a>,
) -> ParseResult<'a, T> {
    let mut longest_success: Option<(T, ParseState)> = None;
    let mut longest_error: Option<(ParseError, ParseState)> = None;

    for parser in parsers {
        match parser(state.clone()) {
            Ok(result)
                if longest_success.is_none()
                    || result.1.offset > longest_success.as_ref().unwrap().1.offset =>
            {
                longest_success = Some(result);
            }
            Err(result)
                if longest_success.is_none()
                    || longest_error.is_none()
                    || result.1.offset > longest_error.as_ref().unwrap().1.offset =>
            {
                longest_error = Some(result);
            }
            _ => continue,
        }
    }

    if let Some(result) = longest_success {
        Ok(result)
    } else {
        Err(longest_error.unwrap()) // This only fails if there were 0 parsers supplied to the function
    }
}

fn expect_token(kind: TokenKind) -> Box<dyn Fn(ParseState) -> ParseResult<&Token>> {
    Box::new(move |state| {
        let token = state.current_token();

        if token.kind == kind.clone() {
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

/*
var expr = ParseAtom(stream, diagnostics);

        if (expr != null)
        {
            while (true)
            {
                var streamStart = stream.Offset;

                var (binop, _) = Try(ParseBinop, stream);
                if (binop == null || binop.Precedence < minPrecedence)
                {
                    stream.Offset = streamStart;
                    break;
                }

                var nextMinPrecedence = binop.Asssociativity == BinaryOperationAssociativity.Right ? binop.Precedence : (binop.Precedence + 1);
                var rhs = ParseExpressionAtPrecedence(nextMinPrecedence, stream, diagnostics);
                if (rhs == null)
                    return null;

                expr = new BinopExpression(expr, binop, rhs);
            }
        }

        return expr;
         */

pub fn parse_expression<'a>(state: ParseState<'a>) -> ParseResult<Expression<'a>> {
    let atoms: &[fn(ParseState) -> ParseResult<Expression>] =
        &[parse_integer_literal, parse_parenthesized_expression];

    let mut expresssion = longest(atoms, state)?;

    loop {
        
    }

    Ok(expresssion)
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

pub fn parse_module(tokens: &[Token]) -> ParseResult<Module> {
    let state = ParseState::new(tokens);

    let (expr, state) = parse_expression(state)?;

    Ok((Module::new(vec![expr]), state))
}
