use nom::{
    bytes::complete::tag,
    character::complete::{digit1, multispace1},
    IResult,
};

use crate::utilities::range::Range;

#[derive(Debug, Clone, PartialEq)]
pub enum TokenKind {
    IntegerLiteral,
    SymbolPlus,
    SymbolHyphen,
    SymbolAsterisk,
    SymbolForwardSlash,
    Whitespace,
    EOF,
}

#[derive(Debug, PartialEq)]
pub struct Token<'a> {
    kind: TokenKind,
    value: &'a str,
    range: Range,
}

impl<'a> Token<'a> {
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
    (TokenKind::Whitespace, |input| multispace1(input)),
];

macro_rules! make_eof {
    ($offset:expr) => {
        Token {
            kind: TokenKind::EOF,
            value: "",
            range: Range::new($offset, 0),
        }
    };
}

#[derive(Debug, PartialEq, Eq)]
pub enum LexErrorKind {
    InvalidCharacter(char),
}

#[derive(Debug, PartialEq, Eq)]
pub struct LexError {
    range: Range,
    kind: LexErrorKind,
}

pub struct LexResult<'a> {
    tokens: Vec<Token<'a>>,
    errors: Vec<LexError>
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
                    value,
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
    tokens.push(make_eof!(parse_offset));

    LexResult {
        tokens,
        errors
    }
}

#[cfg(test)]
mod tests {
    use crate::{
        lexer::{lex, Token, TokenKind},
        utilities::range::Range,
    };

    use super::LexError;

    fn assert_lex(input: &str, expected_tokens: &[(TokenKind, &str)], expected_errors: &[LexError]) {
        let mut right_tokens = vec![];
        let mut offset = 0;

        for (kind, value) in expected_tokens {
            right_tokens.push(Token {
                kind: kind.clone(),
                value,
                range: Range::new(offset, value.len()),
            });

            offset += value.len();
        }

        right_tokens.push(make_eof!(offset));

        let lex_result = lex(input);
        assert_eq!(lex_result.tokens, right_tokens);
        assert_eq!(&lex_result.errors[..], expected_errors);
    }

    #[test]
    fn interger_literal() {
        assert_lex("123", &[(TokenKind::IntegerLiteral, "123")], &[]);
    }

    #[test]
    fn symbol_plus() {
        assert_lex("+", &[(TokenKind::SymbolPlus, "+")], &[]);
    }

    #[test]
    fn symbol_hyphen() {
        assert_lex("-", &[(TokenKind::SymbolHyphen, "-")], &[]);
    }

    #[test]
    fn symbol_asterisk() {
        assert_lex("*", &[(TokenKind::SymbolAsterisk, "*")], &[]);
    }

    #[test]
    fn symbol_forward_slash() {
        assert_lex("/", &[(TokenKind::SymbolForwardSlash, "/")], &[]);
    }

    #[test]
    fn symbol_eof() {
        assert_lex("", &[], &[]);
    }

    #[test]
    fn whitespace() {
        assert_lex(" ", &[(TokenKind::Whitespace, " ")], &[]);
        assert_lex("\t", &[(TokenKind::Whitespace, "\t")], &[]);
        assert_lex("\n", &[(TokenKind::Whitespace, "\n")], &[]);
        assert_lex("\r", &[(TokenKind::Whitespace, "\r")], &[]);
        assert_lex(" \t\n\r", &[(TokenKind::Whitespace, " \t\n\r")], &[]);
    }
}
