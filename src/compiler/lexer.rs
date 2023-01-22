use nom::{
    bytes::complete::tag,
    character::complete::{digit1, multispace1},
    IResult,
};

use crate::utilities::range::Range;

#[derive(Debug, Clone, Copy, PartialEq)]
pub enum TokenKind {
    IntegerLiteral,
    SymbolPlus,
    SymbolHyphen,
    SymbolAsterisk,
    SymbolForwardSlash,
    SymbolSemicolon,
    SymbolOpenParenthesis,
    SymbolCloseParenthesis,
    Whitespace,
    EOF,
}

#[derive(Debug, PartialEq, Clone)]
pub struct Token {
    pub kind: TokenKind,
    pub range: Range,
}

impl Token {
    pub fn new(kind: TokenKind, range: Range) -> Token {
        Token { kind, range }
    }

    pub fn value_char_length(&self, source: &str) -> usize {
        source[self.range.start()..self.range.end()].chars().count()
    }
}

type TokenParser = (TokenKind, fn(&str) -> IResult<&str, &str, ()>);
const TOKEN_DEFINITIONS: &'static [TokenParser] = &[
    (TokenKind::IntegerLiteral, |input| digit1::<&str, ()>(input)),
    (TokenKind::SymbolPlus, |input| tag("+")(input)),
    (TokenKind::SymbolHyphen, |input| tag("-")(input)),
    (TokenKind::SymbolAsterisk, |input| tag("*")(input)),
    (TokenKind::SymbolForwardSlash, |input| tag("/")(input)),
    (TokenKind::SymbolSemicolon, |input| tag(";")(input)),
    (TokenKind::SymbolOpenParenthesis, |input| tag("(")(input)),
    (TokenKind::SymbolCloseParenthesis, |input| tag(")")(input)),
    (TokenKind::Whitespace, |input| multispace1(input)),
];

#[derive(Debug, PartialEq, Eq)]
pub enum LexErrorKind {
    InvalidCharacter(char),
}

#[derive(Debug, PartialEq, Eq)]
pub struct LexError {
    pub range: Range,
    pub kind: LexErrorKind,
}

impl LexError {
    pub fn new(range: Range, kind: LexErrorKind) -> LexError {
        LexError { range, kind }
    }
}

pub struct LexResult<'a> {
    input: &'a str,
    tokens: Vec<Token>,
    errors: Vec<LexError>,
}

impl<'a> LexResult<'a> {
    pub fn tokens(&self) -> &[Token] {
        &self.tokens[..]
    }

    pub fn errors(&self) -> &[LexError] {
        &self.errors[..]
    }

    pub fn has_errors(&self) -> bool {
        !self.errors.is_empty()
    }
}

pub fn lex(input: &str) -> LexResult {
    let mut tokens = vec![];
    let mut errors = vec![];
    let mut parse_offset = 0;

    while parse_offset < input.len() {
        // Attempt to find the first longest token
        let mut longest: Option<Token> = None;

        for (kind, parser) in TOKEN_DEFINITIONS {
            if let Ok((_, value)) = parser(&input[parse_offset..]) {
                if let Some(token) = &longest {
                    if value.chars().count() < token.value_char_length(input) {
                        continue;
                    }
                }

                longest = Some(Token {
                    kind: kind.clone(),
                    range: Range::new(parse_offset, value.len()),
                });
            }
        }

        // Insert the longest token parsed if it exists, else create an error
        // of the next character in the input
        match longest {
            Some(token) => {
                parse_offset += token.range.length();
                tokens.push(token);
            }
            None => {
                let invalid_character = input[parse_offset..].chars().next().unwrap();

                errors.push(LexError {
                    kind: LexErrorKind::InvalidCharacter(invalid_character),
                    range: Range::new(parse_offset, invalid_character.len_utf8()),
                });

                parse_offset += 1;
            }
        }
    }

    // Add EOF token at the end of the input
    tokens.push(Token {
        kind: TokenKind::EOF,
        range: Range::new(parse_offset, 0),
    });

    LexResult { input, tokens, errors }
}
