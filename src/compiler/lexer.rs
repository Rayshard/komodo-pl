use std::convert::Into;

use nom::{bytes::complete::tag, character::complete::{digit1, multispace1}, IResult};

use crate::utilities::range::Range;

#[derive(Debug, Clone, PartialEq)]
pub enum TokenKind {
    IntegerLiteral,
    SymbolPlus,
    SymbolHyphen,
    SymbolAsterisk,
    SymbolForwardSlash,
    Whitespace,
    Invalid,
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

pub fn lex(input: &str) -> Vec<Token> {
    let mut tokens = vec![];
    let mut parse_offset = 0;

    while parse_offset < input.len() {
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

        let token = longest.unwrap_or_else(|| {
            let first_char_length = input[parse_offset..]
                .char_indices()
                .next()
                .unwrap()
                .1
                .len_utf8();

            Token {
                kind: TokenKind::Invalid,
                value: &input[parse_offset..parse_offset + first_char_length],
                range: Range::new(parse_offset, first_char_length),
            }
        });

        parse_offset += token.range.length();
        tokens.push(token);
    }

    tokens
}

#[cfg(test)]
mod tests {
    use crate::{
        lexer::{lex, Token, TokenKind},
        utilities::range::Range,
    };

    #[test]
    fn interger_literal() {
        assert_eq!(
            lex("123"),
            vec![Token {
                kind: TokenKind::IntegerLiteral,
                value: "123",
                range: Range::new(0, 3)
            }]
        );
    }

    #[test]
    fn symbol_plus() {
        assert_eq!(
            lex("+"),
            vec![Token {
                kind: TokenKind::SymbolPlus,
                value: "+",
                range: Range::new(0, 1)
            }]
        );
    }

    #[test]
    fn symbol_hyphen() {
        assert_eq!(
            lex("-"),
            vec![Token {
                kind: TokenKind::SymbolHyphen,
                value: "-",
                range: Range::new(0, 1)
            }]
        );
    }

    #[test]
    fn symbol_asterisk() {
        assert_eq!(
            lex("*"),
            vec![Token {
                kind: TokenKind::SymbolAsterisk,
                value: "*",
                range: Range::new(0, 1)
            }]
        );
    }

    #[test]
    fn symbol_forward_slash() {
        assert_eq!(
            lex("/"),
            vec![Token {
                kind: TokenKind::SymbolForwardSlash,
                value: "/",
                range: Range::new(0, 1)
            }]
        );
    }

    #[test]
    fn whitespace() {
        assert_eq!(
            lex(" "),
            vec![Token {
                kind: TokenKind::Whitespace,
                value: " ",
                range: Range::new(0, 1)
            }]
        );

        assert_eq!(
            lex("\n"),
            vec![Token {
                kind: TokenKind::Whitespace,
                value: "\n",
                range: Range::new(0, 1)
            }]
        );

        assert_eq!(
            lex("\t"),
            vec![Token {
                kind: TokenKind::Whitespace,
                value: "\t",
                range: Range::new(0, 1)
            }]
        );

        assert_eq!(
            lex("\r"),
            vec![Token {
                kind: TokenKind::Whitespace,
                value: "\r",
                range: Range::new(0, 1)
            }]
        );

        assert_eq!(
            lex(" \t\r\n"),
            vec![Token {
                kind: TokenKind::Whitespace,
                value: " \t\r\n",
                range: Range::new(0, 4)
            }]
        );
    }
}
