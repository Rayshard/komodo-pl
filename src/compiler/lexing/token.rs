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
    KeywordFrom,
    Identifier,
    Whitespace,
    EOF,
}

#[derive(Debug, PartialEq, Clone)]
pub struct Token {
    kind: TokenKind,
    range: Range,
}

impl Token {
    pub fn new(kind: TokenKind, range: Range) -> Token {
        Token { kind, range }
    }

    pub fn kind(&self) -> &TokenKind {
        &self.kind
    }

    pub fn range(&self) -> &Range {
        &self.range
    }

    pub fn value_char_length(&self, source: &str) -> usize {
        source[self.range.start()..self.range.end()].chars().count()
    }

    pub fn value<'a>(&self, input: &'a str) -> &'a str {
        &input[self.range.start()..self.range.end()]
    }

    pub fn is_eof(&self) -> bool {
        matches!(self.kind, TokenKind::EOF)
    }

    pub fn is_whitespace(&self) -> bool {
        matches!(self.kind, TokenKind::Whitespace)
    }
}