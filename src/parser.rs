use crate::ast::{Expression, Node, NodeKind, NodeMeta};
use crate::lexer::token::{Token, TokenKind};
use crate::utils::{Position, Span};
use std::cmp;

struct TokenStream {
    tokens: Vec<Token>,
    offset: usize,
}

impl TokenStream {
    pub fn new(tokens: Vec<Token>) -> TokenStream {
        match tokens.last() {
            Some(token) => assert_eq!(
                token.kind,
                TokenKind::EndOfFile,
                "Expected the last token to be an EOF but found {:?}",
                token
            ),
            None => panic!("Input tokens must have at least one token!"),
        };

        TokenStream {
            tokens: tokens,
            offset: 0,
        }
    }

    pub fn read(&mut self) -> &Token {
        let token = self.tokens.get(self.offset).unwrap();

        if token.kind != TokenKind::EndOfFile {
            self.offset += 1;
        }

        token
    }

    pub fn peek(&self) -> &Token {
        self.tokens.get(self.offset).unwrap()
    }

    pub fn get_offset(&self) -> usize {
        self.offset
    }

    pub fn set_offset(&mut self, offset: usize) {
        self.offset = cmp::max(offset, self.tokens.len() - 1);
    }
}

#[derive(Debug)]
pub enum ParseError {
    UnexpectedToken(Token),
}

fn parse_atom(stream: &mut TokenStream) -> Result<Expression, ParseError> {
    let stream_start = stream.get_offset();
    let token = stream.read();

    match &token.kind {
        TokenKind::IntLit(value) => Ok(Expression::IntLit {
            meta: NodeMeta::new(token.span),
            value: value.to_string(),
        }),
        _ => Err(ParseError::UnexpectedToken(token.clone())),
    }
}

pub fn parse(tokens: Vec<Token>) -> Result<Box<dyn Node>, ParseError> {
    let mut stream = TokenStream::new(tokens);
    
    match parse_atom(&mut stream) {
        Ok(node) => Ok(Box::new(node)),
        Err(e) => Err(e)
    }
}
