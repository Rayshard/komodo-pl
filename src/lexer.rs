use crate::utils::{Position, Span};
use regex::Regex;
use serde_json;

pub mod token {
    use super::*;

    #[derive(Debug, Copy, Clone, PartialEq)]
    pub enum TokenType<'a> {
        Invalid(&'a str),
        IntLit(&'a str),
        Plus,
        Minus,
        Asterisk,
        ForwardSlash,
        EndOfFile,
    }

    impl<'a> TokenType<'a> {
        pub fn to_json(&self) -> serde_json::Value {
            match self {
                TokenType::IntLit(value) => serde_json::json!({ "IntLit": value }),
                TokenType::Invalid(value) => serde_json::json!({ "Invalid": value }),
                TokenType::Plus => serde_json::json!("Plus"),
                TokenType::Minus => serde_json::json!("Minus"),
                TokenType::Asterisk => serde_json::json!("Asterisk"),
                TokenType::ForwardSlash => serde_json::json!("ForwardSlash"),
                TokenType::EndOfFile => serde_json::json!("EndOfFile"),
                _ => unimplemented!(),
            }
        }
    }

    #[derive(Debug, PartialEq)]
    pub struct Token<'a> {
        pub r#type: TokenType<'a>,
        pub span: Span,
    }

    impl<'a> Token<'a> {
        pub fn new(r#type: TokenType, start: Position, end: Position) -> Token {
            Token {
                r#type: r#type,
                span: Span::new(start, end),
            }
        }

        pub fn to_json(&self) -> serde_json::Value {
            serde_json::json!({
                "type": self.r#type.to_json(),
                "span": self.span.to_json(),
            })
        }
    }
}

use token::{Token, TokenType};

lazy_static! {
    static ref RE_WHITESPACE: Regex = Regex::new(r"^([ \t\r\f]+|[\n])").unwrap();
    static ref PATTERNS: Vec<(Regex, fn(&str) -> TokenType)> = vec![
        (Regex::new(r"^[0-9]+").unwrap(), |text| {
            TokenType::IntLit(text)
        }),
        (Regex::new(r"^(\+|-|\*|/)").unwrap(), |text| {
            match text {
                "+" => TokenType::Plus,
                "-" => TokenType::Minus,
                "*" => TokenType::Asterisk,
                "/" => TokenType::ForwardSlash,
                _ => unimplemented!(),
            }
        }),
        (Regex::new(r"^[\s\S]").unwrap(), |text| {
            TokenType::Invalid(text)
        }),
    ];
}

pub fn lex(text: &str) -> Vec<Token> {
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
        let mut best_match: Option<(regex::Match, &fn(&str) -> TokenType)> = None;

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
                let token_type = func(re_match.as_str());
                let token_end = Position::new(
                    position.line,
                    position.column + re_match_text.chars().count(),
                );

                tokens.push(Token::new(token_type, position, token_end));
                offset += re_match_text.len();

                match token_type {
                    _ => position = token_end,
                };
            }
            None => panic!("'{}' did not match any pattern!", text[offset..].len()),
        }
    }

    tokens.push(Token::new(TokenType::EndOfFile, position, position));
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
            lex("+ +\n +\t+\r\n +"),
            vec![
                Token::new(TokenType::Plus, Position::new(1, 1), Position::new(1, 2)),
                Token::new(TokenType::Plus, Position::new(1, 3), Position::new(1, 4)),
                Token::new(TokenType::Plus, Position::new(2, 2), Position::new(2, 3)),
                Token::new(TokenType::Plus, Position::new(2, 4), Position::new(2, 5)),
                Token::new(TokenType::Plus, Position::new(3, 2), Position::new(3, 3)),
                Token::new(
                    TokenType::EndOfFile,
                    Position::new(3, 3),
                    Position::new(3, 3)
                )
            ]
        );
    }

    #[test]
    fn test_lex_token_positions() {
        assert_eq!(
            lex("123+  \n -\n\n5   "),
            vec![
                Token::new(
                    TokenType::IntLit("123"),
                    Position::new(1, 1),
                    Position::new(1, 4)
                ),
                Token::new(TokenType::Plus, Position::new(1, 4), Position::new(1, 5)),
                Token::new(TokenType::Minus, Position::new(2, 2), Position::new(2, 3)),
                Token::new(
                    TokenType::IntLit("5"),
                    Position::new(4, 1),
                    Position::new(4, 2)
                ),
                Token::new(
                    TokenType::EndOfFile,
                    Position::new(4, 5),
                    Position::new(4, 5)
                )
            ]
        );
    }

    #[test]
    fn test_lex_int_lit() {
        assert_eq!(
            lex("123"),
            vec![
                Token::new(
                    TokenType::IntLit("123"),
                    Position::new(1, 1),
                    Position::new(1, 4)
                ),
                Token::new(
                    TokenType::EndOfFile,
                    Position::new(1, 4),
                    Position::new(1, 4)
                )
            ]
        );
    }

    #[test]
    fn test_lex_invalid() {
        assert_eq!(
            lex("~"),
            vec![
                Token::new(
                    TokenType::Invalid("~"),
                    Position::new(1, 1),
                    Position::new(1, 2)
                ),
                Token::new(
                    TokenType::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2)
                )
            ]
        );
    }

    #[test]
    fn test_lex_plus() {
        assert_eq!(
            lex("+"),
            vec![
                Token::new(TokenType::Plus, Position::new(1, 1), Position::new(1, 2)),
                Token::new(
                    TokenType::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2)
                )
            ]
        );
    }

    #[test]
    fn test_lex_minus() {
        assert_eq!(
            lex("-"),
            vec![
                Token::new(TokenType::Minus, Position::new(1, 1), Position::new(1, 2)),
                Token::new(
                    TokenType::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2)
                )
            ]
        );
    }

    #[test]
    fn test_lex_asterisk() {
        assert_eq!(
            lex("*"),
            vec![
                Token::new(
                    TokenType::Asterisk,
                    Position::new(1, 1),
                    Position::new(1, 2)
                ),
                Token::new(
                    TokenType::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2)
                )
            ]
        );
    }

    #[test]
    fn test_lex_forward_slash() {
        assert_eq!(
            lex("/"),
            vec![
                Token::new(
                    TokenType::ForwardSlash,
                    Position::new(1, 1),
                    Position::new(1, 2)
                ),
                Token::new(
                    TokenType::EndOfFile,
                    Position::new(1, 2),
                    Position::new(1, 2)
                )
            ]
        );
    }
}
