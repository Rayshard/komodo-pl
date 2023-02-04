use serde::Serialize;

use crate::compiler::{
    cst::Node,
    lexer::token::Token,
    utilities::{range::Range, text_source::TextSource},
};

use super::{identifier::Identifier, Expression};

#[derive(Serialize)]
pub struct MemberAccess<'source> {
    root: Box<Expression<'source>>,
    dot: Token<'source>,
    member: Identifier<'source>,
}

impl<'source> MemberAccess<'source> {
    pub fn root(&self) -> &Expression<'source> {
        self.root.as_ref()
    }

    pub fn dot(&self) -> &Token<'source> {
        &self.dot
    }

    pub fn member(&self) -> &Identifier<'source> {
        &self.member
    }
}

impl<'source> Node<'source> for MemberAccess<'source> {
    fn range(&self) -> Range {
        Range::new(self.root.range().start(), self.member.range().end())
    }

    fn source(&self) -> &'source TextSource {
        self.root.source()
    }
}
