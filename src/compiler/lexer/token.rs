use serde::{ser::SerializeMap, Serialize};

use crate::compiler::utilities::location::Location;

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
    SymbolComma,
    KeywordImport,
    KeywordFrom,
    Identifier,
    Whitespace,
    EOF,
}

#[derive(Debug, PartialEq, Clone)]
pub struct Token<'source> {
    kind: TokenKind,
    location: Location<'source>,
}

impl<'source> Token<'source> {
    pub fn new(kind: TokenKind, location: Location<'source>) -> Token<'source> {
        Token { kind, location }
    }

    pub fn kind(&self) -> &TokenKind {
        &self.kind
    }

    pub fn location(&self) -> &Location<'source> {
        &self.location
    }

    pub fn value_char_length(&self) -> usize {
        self.value().chars().count()
    }

    pub fn value(&self) -> &'source str {
        self.location.text()
    }

    pub fn is_eof(&self) -> bool {
        matches!(self.kind, TokenKind::EOF)
    }

    pub fn is_whitespace(&self) -> bool {
        matches!(self.kind, TokenKind::Whitespace)
    }
}

impl<'source> Serialize for Token<'source> {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        let mut map = serializer.serialize_map(None)?;

        map.serialize_entry("kind", &self.kind)?;
        map.serialize_entry("location", &self.location)?;
        map.serialize_entry("value", &self.value())?;

        map.end()
    }
}
