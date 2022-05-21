use crate::ast::{Expression, Node, NodeMeta, NodeType};
use crate::lexer::token::{Token, TokenType};
use crate::utils::{Position, Span};
use std::cmp;

struct TokenStream<'a> {
    tokens: Vec<Token<'a>>,
    offset: usize,
}

impl<'a> TokenStream<'a> {
    pub fn new(tokens: Vec<Token>) -> TokenStream {
        TokenStream {
            tokens: tokens,
            offset: 0,
        }
    }

    pub fn read(&mut self) -> Option<&Token> {
        if let Some(token) = self.tokens.get(self.offset) {
            self.offset += 1;
            Some(token)
        } else {
            None
        }
    }

    pub fn peek(&self) -> Option<&Token> {
        self.tokens.get(self.offset)
    }

    pub fn set_offset(&mut self, offset: usize) {
        self.offset = cmp::max(offset, self.tokens.len());
    }
}

pub fn parse(tokens: Vec<Token>) -> Box<dyn Node> {
    let left = Box::new(Expression::IntLit {
        meta: NodeMeta::new(Position::new(1, 1), Position::new(1, 2)),
        value: String::from("123"),
    });

    let right = Box::new(Expression::IntLit {
        meta: NodeMeta::new(Position::new(1, 4), Position::new(1, 5)),
        value: String::from("123"),
    });

    Box::new(Expression::Binop {
        meta: NodeMeta::new(left.get_span().start, right.get_span().end),
        left: left,
        op: String::from("+"),
        right: right,
    })
}
