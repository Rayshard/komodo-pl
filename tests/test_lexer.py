import unittest

from komodo.lexer import PATTERNS, RE_WHITESPACE, Token, TokenType, lex
from komodo.utils import Location, Position, SourceFile, Span


class Test(unittest.TestCase):
    def test_all_patterns_have_start_of_line_character(self) -> None:
        self.assertTrue(
            RE_WHITESPACE.pattern.startswith("^"),
            "RE_WHITESPACE does not start with a '^'.",
        )

        for pattern, _ in PATTERNS:
            self.assertTrue(
                pattern.pattern.startswith("^"),
                f"The pattern ({pattern.pattern}) does not start with a '^'.",
            )

    def test_lex_whitespace(self) -> None:
        self.assertEqual(
            lex(SourceFile("", "+ +\n +\t+\r\n +")),
            [
                Token(
                    TokenType.PLUS, Location("", Span(Position(1, 1), Position(1, 2)))
                ),
                Token(
                    TokenType.PLUS, Location("", Span(Position(1, 3), Position(1, 4)))
                ),
                Token(
                    TokenType.PLUS, Location("", Span(Position(2, 2), Position(2, 3)))
                ),
                Token(
                    TokenType.PLUS, Location("", Span(Position(2, 4), Position(2, 5)))
                ),
                Token(
                    TokenType.PLUS, Location("", Span(Position(3, 2), Position(3, 3)))
                ),
                Token(
                    TokenType.EOF, Location("", Span(Position(3, 3), Position(3, 3)))
                ),
            ],
        )

    def test_lex_token_positions(self):
        self.fail("TODO")

    def test_lex_int_lit(self):
        self.fail("TODO")

    def test_lex_invalid(self):
        self.fail("TODO")

    def test_lex_plus(self):
        self.fail("TODO")

    def test_lex_minus(self):
        self.fail("TODO")

    def test_lex_asterisk(self):
        self.fail("TODO")

    def test_lex_forward_slash(self):
        self.fail("TODO")

if __name__ == "__main__":
    unittest.main()
