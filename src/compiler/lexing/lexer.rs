use crate::compiler::utilities::{range::Range, text_source::TextSource};
use lazy_static::lazy_static;
use regex::Regex;

use super::token::{Token, TokenKind};

#[derive(Debug, PartialEq, Eq)]
pub enum LexErrorKind {
    UnexpectedCharacter { character: char, expected: String },
    UnexpectedEOF { expected: String },
    StringLiteralMissingClosingQuotation,
}

#[derive(Debug, PartialEq)]
pub struct LexError<'source> {
    range: Range,
    kind: LexErrorKind,
    source: &'source TextSource,
}

impl<'source> LexError<'source> {
    pub fn new(range: Range, kind: LexErrorKind, source: &TextSource) -> LexError {
        LexError { range, kind, source }
    }

    pub fn range(&self) -> &Range {
        &self.range
    }

    pub fn kind(&self) -> &LexErrorKind {
        &self.kind
    }

    pub fn source(&self) -> &TextSource {
        &self.source
    }
}

impl<'source> ToString for LexError<'source> {
    fn to_string(&self) -> String {
        let message = match &self.kind {
            LexErrorKind::UnexpectedCharacter {
                character,
                expected,
            } => format!("Expected {expected}, but found {character}"),
            LexErrorKind::UnexpectedEOF { expected } => {
                format!("Expected {expected}, but found EOF")
            }
            LexErrorKind::StringLiteralMissingClosingQuotation => {
                format!("Expected \" to termintate string literal")
            }
        };

        format!(
            "ERROR ({}) {}",
            self.source.get_terminal_link(self.range.start()).unwrap(),
            message
        )
    }
}

type LexResult = Result<(TokenKind, usize), (LexErrorKind, usize)>;

fn lex_integer_literal(input: &str) -> LexResult {
    lazy_static! {
        static ref RE: Regex = Regex::new(r"^(0|([1-9][0-9]*))").unwrap();
    }

    if let Some(re_match) = RE.find(input) {
        Ok((TokenKind::IntegerLiteral, re_match.range().len()))
    } else if let Some(character) = input.chars().next() {
        Err((
            LexErrorKind::UnexpectedCharacter {
                character,
                expected: "a digit".to_string(),
            },
            character.len_utf8(),
        ))
    } else {
        Err((
            LexErrorKind::UnexpectedEOF {
                expected: "a digit".to_string(),
            },
            0,
        ))
    }
}

fn lex_string_literal(input: &str) -> LexResult {
    let mut chars = input.chars();

    match chars.next() {
        Some('"') => {
            let mut length = '"'.len_utf8();

            for c in chars {
                length += c.len_utf8();

                if c == '"' {
                    return Ok((TokenKind::StringLiteral, length));
                }
            }

            Err((LexErrorKind::StringLiteralMissingClosingQuotation, length))
        }
        Some(character) => Err((
            LexErrorKind::UnexpectedCharacter {
                character,
                expected: "\"".to_string(),
            },
            character.len_utf8(),
        )),
        None => Err((
            LexErrorKind::UnexpectedEOF {
                expected: "\"".to_string(),
            },
            0,
        )),
    }
}

fn lex_identifier(input: &str) -> LexResult {
    lazy_static! {
        static ref RE: Regex = Regex::new(r"^[_a-zA-Z][_a-zA-Z0-9]*").unwrap();
    }

    if let Some(re_match) = RE.find(input) {
        Ok((TokenKind::Identifier, re_match.range().len()))
    } else if let Some(character) = input.chars().next() {
        Err((
            LexErrorKind::UnexpectedCharacter {
                character,
                expected: "a _ or letter".to_string(),
            },
            character.len_utf8(),
        ))
    } else {
        Err((
            LexErrorKind::UnexpectedEOF {
                expected: "a _ or letter".to_string(),
            },
            0,
        ))
    }
}

fn lex_whitespace(input: &str) -> LexResult {
    lazy_static! {
        static ref RE: Regex = Regex::new(r"^\s+").unwrap();
    }

    if let Some(re_match) = RE.find(input) {
        Ok((TokenKind::Whitespace, re_match.range().len()))
    } else if let Some(character) = input.chars().next() {
        Err((
            LexErrorKind::UnexpectedCharacter {
                character,
                expected: "a whitespace character".to_string(),
            },
            character.len_utf8(),
        ))
    } else {
        Err((
            LexErrorKind::UnexpectedEOF {
                expected: "a whitespace character".to_string(),
            },
            0,
        ))
    }
}

fn lex_exact(input: &str, kind: TokenKind, expected: &str) -> LexResult {
    if input.starts_with(expected) {
        Ok((kind, expected.len()))
    } else {
        let mut input_chars = input.chars();
        let mut expected_chars = expected.chars();
        let mut read_chars = String::new();

        loop {
            match (input_chars.next(), expected_chars.next()) {
                (Some(ic), Some(ec)) if ic == ec => read_chars.push(ic),
                (Some(ic), Some(ec)) if ic != ec => {
                    return Err((
                        LexErrorKind::UnexpectedCharacter {
                            character: ic,
                            expected: ec.to_string(),
                        },
                        read_chars.len() + ic.len_utf8(),
                    ))
                }
                (None, Some(ec)) => {
                    return Err((
                        LexErrorKind::UnexpectedEOF {
                            expected: ec.to_string(),
                        },
                        input.len(),
                    ))
                }
                _ => panic!("Unexpected match arm"),
            }
        }
    }
}

const LEXERS: &'static [fn(input: &str) -> LexResult] = &[
    lex_integer_literal,
    lex_string_literal,
    |input| lex_exact(input, TokenKind::SymbolPlus, "+"),
    |input| lex_exact(input, TokenKind::SymbolHyphen, "-"),
    |input| lex_exact(input, TokenKind::SymbolAsterisk, "*"),
    |input| lex_exact(input, TokenKind::SymbolForwardSlash, "/"),
    |input| lex_exact(input, TokenKind::SymbolSemicolon, ";"),
    |input| lex_exact(input, TokenKind::SymbolOpenParenthesis, "("),
    |input| lex_exact(input, TokenKind::SymbolCloseParenthesis, ")"),
    |input| lex_exact(input, TokenKind::SymbolOpenCurlyBracket, "{"),
    |input| lex_exact(input, TokenKind::SymbolCloseCurlyBracket, "}"),
    |input| lex_exact(input, TokenKind::SymbolPeriod, "."),
    |input| lex_exact(input, TokenKind::KeywordImport, "import"),
    |input| lex_exact(input, TokenKind::KeywordFrom, "from"),
    lex_identifier,
    lex_whitespace,
];

pub fn lex(source: &TextSource) -> (Vec<Token>, Vec<LexError>) {
    let mut tokens = vec![];
    let mut errors = vec![];
    let mut parse_offset = 0;

    while parse_offset < source.len() {
        // Attempt to find the first longest token
        let mut longest: Option<Token> = None;
        let mut longest_error: Option<LexError> = None;

        for lexer in LEXERS {
            match lexer(&source.text()[parse_offset..]) {
                Ok((kind, length)) => {
                    if let Some(longest) = &longest {
                        let token_char_count =
                            source.text()[parse_offset..parse_offset + length].chars().count();

                        if token_char_count <= longest.value_char_length() {
                            continue;
                        }
                    }

                    longest = Some(Token::new(kind, Range::new(parse_offset, length), source));
                }
                Err((kind, length)) => {
                    if let Some(error) = &longest_error {
                        if length < error.range.length() {
                            continue;
                        }
                    }

                    longest_error = Some(LexError {
                        kind,
                        range: Range::new(parse_offset, length),
                        source
                    });
                }
            }
        }

        // Insert the longest token parsed if it exists, else create an error
        // of the next character in the input
        match longest {
            Some(token) => {
                parse_offset += token.range().length();
                tokens.push(token);
            }
            None => match longest_error {
                Some(error) => {
                    parse_offset += error.range.length();
                    errors.push(error);
                }
                None => todo!(),
            },
        }
    }

    // Add EOF token at the end of the input
    tokens.push(Token::new(TokenKind::EOF, Range::new(parse_offset, 0), source));

    (tokens, errors)
}
