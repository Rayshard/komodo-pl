use crate::lexer::token::{Token, TokenType};
use crate::ast;
use std::cmp;

struct TokenStream {
    tokens: Vec<Token>,
    offset: usize,
}

impl TokenStream {
    pub fn new(tokens: Vec<Token>) -> TokenStream {
        TokenStream {
            tokens: tokens,
            offset: 0
        }
    }

    pub fn read(&self) -> Option<&Token> {
        if let Some(token) = self.tokens.get(offset) {
            offset += 1;
            Some(token)
        }
        else {
            None
        }
    }

    pub fn peek(&self) -> Option<&Token> {
        self.tokens.get(offset)
    }

    pub fn set_offset(&mut self, offset: usize) {
        self.offset = cmp::max(offset, self.tokens.len());
    }
}

pub fn parse(tokens: Vec<Token>) -> ast::Node {

}