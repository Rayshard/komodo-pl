use crate::utils::{Position, Span};
use regex::Regex;
use serde_json;

pub mod token {
    use super::*;

    #[derive(Debug, Clone, PartialEq)]
    pub enum TokenKind {
        Invalid(String),
        IntLit(String),
        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        EndOfFile,
    }

    impl TokenKind {
        pub fn to_json(&self) -> serde_json::Value {
            match self {
                TokenKind::IntLit(value) => serde_json::json!({ "IntLit": value }),
                TokenKind::Invalid(value) => serde_json::json!({ "Invalid": value }),
                TokenKind::Plus => serde_json::json!("Plus"),
                TokenKind::Minus => serde_json::json!("Minus"),
                TokenKind::Asterisk => serde_json::json!("Asterisk"),
                TokenKind::ForwardSlash => serde_json::json!("ForwardSlash"),
                TokenKind::EndOfFile => serde_json::json!("EndOfFile"),
                _ => unimplemented!(),
            }
        }
    }

    #[derive(Debug, PartialEq, Clone)]
    pub struct Token<'a> {
        pub kind: TokenKind,
        pub span: Span<'a>,
    }

    impl<'a> Token<'a> {
        pub fn new(kind: TokenKind, start: Position, end: Position, file_name: &String) -> Token {
            Token {
                kind: kind,
                span: Span::new(start, end, file_name),
            }
        }

        pub fn to_json(&self) -> serde_json::Value {
            serde_json::json!({
                "kind": self.kind.to_json(),
                "span": self.span.to_json(),
            })
        }
    }
}

use token::*;

lazy_static! {
    static ref RE_WHITESPACE: Regex = Regex::new(r"^([ \t\r\f]+|[\n])").unwrap();
    static ref PATTERNS: Vec<(Regex, fn(&str) -> TokenKind)> = vec![
        (Regex::new(r"^[0-9]+").unwrap(), |text| {
            TokenKind::IntLit(text.to_string())
        }),
        (Regex::new(r"^(\+|-|\*|/)").unwrap(), |text| {
            match text {
                "+" => TokenKind::Plus,
                "-" => TokenKind::Minus,
                "*" => TokenKind::Asterisk,
                "/" => TokenKind::ForwardSlash,
                _ => unimplemented!(),
            }
        }),
        (Regex::new(r"^[\s\S]").unwrap(), |text| {
            TokenKind::Invalid(text.to_string())
        }),
    ];
}

pub fn lex<'a>(text: &str, file_name: &'static String) -> Vec<Token<'a>> {
    let mut offset: usize = 0;
    let mut position: Position = Position::new(1, 1);
    let mut tokens: Vec<Token> = Vec::new();

    while offset < text.len() {
        //Skip whitespace
        while let Some(re_match) = RE_WHITESPACE.find(&text[offset..]) {
            match re_match.as_str() {
                "\n" => position = Position::new(position.line + 1, 1),
                _ => {
                    position = Position::new(
                        position.line,
                        position.column + re_match.as_str().chars().count(),
                    )
                }
            }

            offset += re_match.as_str().len();
        }

        if offset >= text.len() {
            break;
        }

        //Find best match
        let mut best_match: Option<(regex::Match, &fn(&str) -> TokenKind)> = None;

        for (re, func) in PATTERNS.iter() {
            if let Some(re_match) = re.find(&text[offset..]) {
                if best_match.is_none()
                    || re_match.as_str().chars().count()
                        > best_match.unwrap().0.as_str().chars().count()
                {
                    best_match = Some((re_match, func));
                }
            }
        }

        match best_match {
            Some((re_match, func)) => {
                let re_match_text = re_match.as_str();
                let token_kind = func(re_match.as_str());
                let token_end = Position::new(
                    position.line,
                    position.column + re_match_text.chars().count(),
                );

                tokens.push(Token::new(token_kind, position, token_end, file_name));
                offset += re_match_text.len();

                position = token_end;
            }
            None => panic!("'{}' did not match any pattern!", text[offset..]),
        }
    }

    tokens.push(Token::new(
        TokenKind::EndOfFile,
        position,
        position,
        file_name,
    ));
    return tokens;
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_all_patterns_have_start_of_line_character() {
        assert!(
            RE_WHITESPACE.as_str().chars().nth(0) == Some('^'),
            "RE_WHITESPACE does not start with a '^'."
        );

        for pattern in PATTERNS.iter() {
            assert_eq!(
                pattern.0.as_str().chars().nth(0),
                Some('^'),
                "The pattern ({}) does not start with a '^'.",
                pattern.0.as_str()
            );
        }
    }

    #[test]
    fn test_lex_whitespace() {
        assert_eq!(
            lex("+ +\n +\t+\r\n +", &"".to_string()),
            vec![
                Token::new(
                    TokenKind::Plus,
                    Position::new(1, 1),
                    Position::new(1, 2),
                    &"".to_string()
                ),
                Token::new(
                    TokenKind::Plus,
                    Position::new(1, 3),
                    Position::new(1, 4),
                    &"".to_string()
                ),
                Token::new(
                    TokenKind::Plus,
                    Position::new(2, 2),
                    Position::new(2, 3),
                    &"".to_string()
                ),
                Token::new(
                    TokenKind::Plus,
                    Position::new(2, 4),
                    Position::new(2, 5),
                    &"".to_string()
                ),
                Token::new(
                    TokenKind::Plus,
                    Position::new(3, 2),
                    Position::new(3, 3),
                    &"".to_string()
                ),
                Token::new(
                    TokenKind::EndOfFile,
                    Position::new(3, 3),
                    Position::new(3, 3),
                    &"".to_string(),
                )
            ]
        );
    }

    #[test]
    fn test_lex_token_positions() {
        assert_eq!(
            lex("123+  \n -\n\n5   ", &"".to_string()),
            vec![
                Token::new(
                    TokenKind::IntLit("123".to_string()),
                    Position::new(1, 1),
                    Position::new(1, 4),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::Plus,
                    Position::new(1, 4),
                    Position::new(1, 5),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::Minus,
                    Position::new(2, 2),
                    Position::new(2, 3),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::IntLit("5".to_string()),
                    Position::new(4, 1),
                    Position::new(4, 2),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::EndOfFile,
                    Position::new(4, 5),
                    Position::new(4, 5),
                    &"".to_string(),
                )
            ]
        );
    }

    #[test]
    fn test_lex_int_lit() {
        assert_eq!(
            lex("123", &"".to_string()),
            vec![
                Token::new(
                    TokenKind::IntLit("123".to_string()),
                    Position::new(1, 1),
                    Position::new(1, 4),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::EndOfFile,
                    Position::new(1, 4),
                    Position::new(1, 4),
                    &"".to_string(),
                )
            ]
        );
    }

    #[test]
    fn test_lex_invalid() {
        assert_eq!(
            lex("~", &"".to_string()),
            vec![
                Token::new(
                    TokenKind::Invalid("~".to_string()),
                    Position::new(1, 1),
                    Position::new(1, 2),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2),
                    &"".to_string(),
                )
            ]
        );
    }

    #[test]
    fn test_lex_plus() {
        assert_eq!(
            lex("+", &"".to_string()),
            vec![
                Token::new(
                    TokenKind::Plus,
                    Position::new(1, 1),
                    Position::new(1, 2),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2),
                    &"".to_string(),
                )
            ]
        );
    }

    #[test]
    fn test_lex_minus() {
        assert_eq!(
            lex("-", &"".to_string()),
            vec![
                Token::new(
                    TokenKind::Minus,
                    Position::new(1, 1),
                    Position::new(1, 2),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2),
                    &"".to_string(),
                )
            ]
        );
    }

    #[test]
    fn test_lex_asterisk() {
        assert_eq!(
            lex("*", &"".to_string()),
            vec![
                Token::new(
                    TokenKind::Asterisk,
                    Position::new(1, 1),
                    Position::new(1, 2),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2),
                    &"".to_string(),
                )
            ]
        );
    }

    #[test]
    fn test_lex_forward_slash() {
        assert_eq!(
            lex("/", &"".to_string()),
            vec![
                Token::new(
                    TokenKind::ForwardSlash,
                    Position::new(1, 1),
                    Position::new(1, 2),
                    &"".to_string(),
                ),
                Token::new(
                    TokenKind::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2),
                    &"".to_string(),
                )
            ]
        );
    }
}
