use std::{fs, io};

use super::{position::Position, range::Range};

#[derive(Debug, Clone)]
pub struct TextSource {
    name: String,
    text: String,
    lines: Vec<Range>,
}

impl TextSource {
    pub fn new(name: String, text: String) -> TextSource {
        let mut lines = Vec::<Range>::new();
        let mut offset = 0usize;

        for line in text.split('\n') {
            let line_length = line.len() + '\n'.len_utf8();

            lines.push(Range::new(offset, line_length));
            offset += line_length;
        }

        TextSource { name, text, lines }
    }

    pub fn name(&self) -> &str {
        &self.name
    }

    pub fn text(&self) -> &str {
        &self.text
    }

    pub fn get_position(&self, offset: usize) -> Option<Position> {
        if offset > self.text.len() {
            return None;
        }

        let (mut line, mut column) = (1, 1);

        for (line_number, line_range) in self.lines.iter().enumerate() {
            let line_number = line_number + 1;

            if offset < line_range.start() {
                break;
            }

            line = line_number;
            column = offset - line_range.start() + 1;
        }

        Some(Position::new(line, column))
    }

    pub fn get_terminal_link(&self, offset: usize) -> Option<String> {
        let position = self.get_position(offset)?;
        Some(format!("{}:{}", self.name, position))
    }

    pub fn from_file(path: &str) -> io::Result<TextSource> {
        let text = fs::read_to_string(path)?;
        Ok(TextSource::new(path.to_string(), text))
    }
}

#[cfg(test)]
mod tests {
    use crate::compiler::utilities::{position::Position, text_source::TextSource};

    #[test]
    fn single_line_text_source_get_position() {
        let text_source = TextSource::new("test".to_string(), "123".to_string());

        assert_eq!(text_source.get_position(0), Some(Position::new(1, 1)));
        assert_eq!(text_source.get_position(1), Some(Position::new(1, 2)));
        assert_eq!(text_source.get_position(2), Some(Position::new(1, 3)));
        assert_eq!(text_source.get_position(3), Some(Position::new(1, 4)));
        assert_eq!(text_source.get_position(4), None);
    }

    #[test]
    fn multi_line_text_source_get_position() {
        let text_source = TextSource::new("test".to_string(), "12\n345".to_string());

        assert_eq!(text_source.get_position(0), Some(Position::new(1, 1)));
        assert_eq!(text_source.get_position(1), Some(Position::new(1, 2)));
        assert_eq!(text_source.get_position(2), Some(Position::new(1, 3)));
        assert_eq!(text_source.get_position(3), Some(Position::new(2, 1)));
        assert_eq!(text_source.get_position(4), Some(Position::new(2, 2)));
        assert_eq!(text_source.get_position(5), Some(Position::new(2, 3)));
        assert_eq!(text_source.get_position(6), Some(Position::new(2, 4)));
        assert_eq!(text_source.get_position(7), None);
    }

    #[test]
    fn text_source_get_terminal_link() {
        let text_source = TextSource::new("test".to_string(), "12\n345".to_string());

        assert_eq!(text_source.get_terminal_link(1), Some(format!("test:1:2")));
        assert_eq!(text_source.get_terminal_link(5), Some(format!("test:2:3")));
    }
}