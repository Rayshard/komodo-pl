use crate::compiler::utilities::range::Range;

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