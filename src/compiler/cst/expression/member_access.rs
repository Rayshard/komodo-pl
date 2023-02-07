use serde::Serialize;

use crate::compiler::{
    cst::Node,
    lexer::token::Token,
    utilities::{location::Location, range::Range},
};

use super::{identifier::Identifier, Expression};

#[derive(Serialize, Clone)]
pub struct MemberAccess<'source> {
    root: Box<Expression<'source>>,
    dot: Token<'source>,
    member: Identifier<'source>,
}

impl<'source> MemberAccess<'source> {
    pub fn new(
        root: Expression<'source>,
        dot: Token<'source>,
        member: Identifier<'source>,
    ) -> Self {
        Self {
            root: Box::new(root),
            dot,
            member,
        }
    }

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
    fn location(&self) -> Location<'source> {
        Location::new(
            self.dot.location().source(),
            Range::new(
                self.root.location().range().start(),
                self.member.location().range().end(),
            ),
        )
    }
}
