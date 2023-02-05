use crate::compiler::utilities::location::Location;

pub struct ParseError<'source> {
    message: String,
    location: Location<'source>,
}

impl<'source> ParseError<'source> {
    pub fn new(message: String, location: Location<'source>) -> Self {
        Self { message, location }
    }

    pub fn location(&self) -> &Location {
        &self.location
    }
}

impl<'source> ToString for ParseError<'source> {
    fn to_string(&self) -> String {
        format!(
            "ERROR ({}) {}",
            self.location.start_terminal_link(),
            self.message
        )
    }
}
