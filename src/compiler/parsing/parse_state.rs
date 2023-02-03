use crate::compiler::lexing::token::Token;

#[derive(Clone)]
pub struct ParseState<'tokens, 'source> {
    tokens: &'tokens [Token<'source>],
    offset: usize,
}

impl<'tokens, 'source> ParseState<'tokens, 'source> {
    pub fn new(tokens: &'tokens [Token<'source>]) -> ParseState<'tokens, 'source> {
        if let Some(token) = tokens.last() {
            if !token.is_eof() {
                panic!("Invalid tokens. The last token of the input tokens should be an EOF token.")
            }

            return ParseState { tokens, offset: 0 };
        } else {
            panic!(
                "Invalid tokens. Input tokens must have at least one token (which should be EOF)."
            )
        }
    }

    pub fn offset(&self) -> usize {
        self.offset
    }

    pub fn current_token(&self) -> &Token<'source> {
        &self.tokens[self.offset]
    }

    pub fn next(&self) -> ParseState<'tokens, 'source> {
        if self.offset == self.tokens.len() {
            panic!("Unable to create new parser state because there are no more tokens to read.")
        }

        ParseState {
            tokens: self.tokens,
            offset: self.offset + 1,
        }
    }
}