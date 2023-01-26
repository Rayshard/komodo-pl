use lazy_static::lazy_static;
use regex::Regex;
use super::utilities::range::Range;

#[derive(Debug, Clone, Copy, PartialEq)]
pub enum TokenKind {
    IntegerLiteral,
    StringLiteral,
    SymbolPlus,
    SymbolHyphen,
    SymbolAsterisk,
    SymbolForwardSlash,
    SymbolSemicolon,
    SymbolOpenParenthesis,
    SymbolCloseParenthesis,
    SymbolOpenCurlyBracket,
    SymbolCloseCurlyBracket,
    SymbolPeriod,
    KeywordImport,
    Identifier,
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

    pub fn value<'a>(&self, input: &'a str) -> &'a str {
        &input[self.range.start()..self.range.end()]
    }
}

fn lex_integer_literal(input: &str) -> Option<usize> {
    lazy_static! {
        static ref RE: Regex = Regex::new(r"^(0|([1-9][0-9]*))").unwrap();
    }

    Some(RE.find(input)?.range().len())
}

fn lex_string_literal(input: &str) -> Option<usize> {
    let mut chars = input.chars();

    if let Some('"') = chars.next() {
        let mut length = 1;

        for c in chars {
            length += 1;

            if c == '"' {
                return Some(length);
            }
        }
    }

    None
}

fn lex_identifier(input: &str) -> Option<usize> {
    lazy_static! {
        static ref RE: Regex = Regex::new(r"^[_a-zA-Z][_a-zA-Z0-9]*").unwrap();
    }

    Some(RE.find(input)?.range().len())
}

fn lex_whitespace(input: &str) -> Option<usize> {
    lazy_static! {
        static ref RE: Regex = Regex::new(r"^\s+").unwrap();
    }

    Some(RE.find(input)?.range().len())
}

fn lex_exact(input: &str, expected: &str) -> Option<usize> {
    if input.starts_with(expected) {
        Some(expected.len())
    }
    else {
        None
    }
}

type TokenParser = (TokenKind, fn(&str) -> Option<usize>);
const TOKEN_DEFINITIONS: &'static [TokenParser] = &[
    (TokenKind::IntegerLiteral, lex_integer_literal),
    (TokenKind::StringLiteral, lex_string_literal),
    (TokenKind::SymbolPlus, |input| lex_exact(input, "+")),
    (TokenKind::SymbolHyphen, |input| lex_exact(input, "-")),
    (TokenKind::SymbolAsterisk, |input| lex_exact(input, "*")),
    (TokenKind::SymbolForwardSlash, |input| lex_exact(input, "/")),
    (TokenKind::SymbolSemicolon, |input| lex_exact(input, ";")),
    (TokenKind::SymbolOpenParenthesis, |input| lex_exact(input, "(")),
    (TokenKind::SymbolCloseParenthesis, |input| lex_exact(input, ")")),
    (TokenKind::SymbolOpenCurlyBracket, |input| lex_exact(input, "{")),
    (TokenKind::SymbolCloseCurlyBracket, |input| lex_exact(input, "}")),
    (TokenKind::SymbolPeriod, |input| lex_exact(input, ".")),
    (TokenKind::KeywordImport, |input| lex_exact(input, "import")),
    (TokenKind::Identifier, lex_identifier),
    (TokenKind::Whitespace, lex_whitespace),
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

impl ToString for LexError {
    fn to_string(&self) -> String {
        let message = match self.kind {
            LexErrorKind::InvalidCharacter(c) => format!("Encounter an invaild character: {c}"),
        };

        format!("Lexing Error [{}, {}]: {}", self.range.start(), self.range.end() - 1, message)
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
            if let Some(token_length) = parser(&input[parse_offset..]) {
                if let Some(token) = &longest {
                    let token_char_count = &input[parse_offset..parse_offset + token_length].chars().count();

                    if token_char_count < &token.value_char_length(input) {
                        continue;
                    }
                }

                longest = Some(Token {
                    kind: kind.clone(),
                    range: Range::new(parse_offset, token_length),
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

    LexResult {
        input,
        tokens,
        errors,
    }
}
