use serde::{Serialize, ser::SerializeMap};

use crate::compiler::utilities::{range::Range, text_source::TextSource};

#[derive(Debug, Clone, Copy, PartialEq, Serialize)]
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
pub struct Token<'a> {
    kind: TokenKind,
    range: Range,
    source: &'a TextSource,
}

impl<'a> Token<'a> {
    pub fn new(kind: TokenKind, range: Range, source: &'a TextSource) -> Token<'a> {
        Token {
            kind,
            range,
            source,
        }
    }

    pub fn kind(&self) -> &TokenKind {
        &self.kind
    }

    pub fn range(&self) -> &Range {
        &self.range
    }

    pub fn source(&self) -> &'a TextSource {
        &self.source
    }

    pub fn value_char_length(&self) -> usize {
        self.value().chars().count()
    }

    pub fn value(&self) -> &'a str {
        self.source.text_from_range(&self.range)
    }

    pub fn is_eof(&self) -> bool {
        matches!(self.kind, TokenKind::EOF)
    }

    pub fn is_whitespace(&self) -> bool {
        matches!(self.kind, TokenKind::Whitespace)
    }
}

impl<'a> Serialize for Token<'a> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        let mut map = serializer.serialize_map(None)?;

        map.serialize_entry("kind", &self.kind)?;
        map.serialize_entry("range", &self.range)?;
        map.serialize_entry("value", &self.value())?;

        map.end()
    }
}
