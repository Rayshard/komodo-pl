from dataclasses import dataclass
from enum import Enum, auto
from turtle import position
from typing import Callable, List, Tuple, Optional, Pattern

from komodo.utils import Location, Position, SourceFile, Span

import re


class TokenType(Enum):
    INVALID = auto()
    INT_LIT = auto()
    PLUS = auto()
    MINUS = auto()
    ASTERISK = auto()
    FORWARD_SLASH = auto()
    EOF = auto()


@dataclass
class Token:
    type: TokenType
    location: Location
    value: Optional[str] = None


RE_WHITESPACE = re.compile(r"^\s+")
PATTERNS: List[Tuple[Pattern[str], Callable[[str, Location], Token]]] = [
    (
        re.compile(r"^(0|([1-9][0-9]*))"),
        lambda text, location: Token(TokenType.INT_LIT, location, text),
    ),
    (re.compile(r"^\+"), lambda text, location: Token(TokenType.PLUS, location, None)),
    (re.compile(r"^-"), lambda text, location: Token(TokenType.MINUS, location, None)),
    (
        re.compile(r"^\*"),
        lambda text, location: Token(TokenType.ASTERISK, location, None),
    ),
    (
        re.compile(r"^/"),
        lambda text, location: Token(TokenType.FORWARD_SLASH, location, None),
    ),
    (
        re.compile(r"^[\s\S]"),
        lambda text, location: Token(TokenType.INVALID, location, text),
    ),
]


def lex(source: SourceFile) -> List[Token]:
    offset = 0
    tokens = list[Token]()

    while offset < source.length:
        # Skip whitespace
        while True:
            re_match = RE_WHITESPACE.match(source.text[offset:])
            if re_match is None:
                break

            offset += len(re_match[0])

        if offset >= source.length:
            break

        # Find best match
        best_match: Optional[
            Tuple[re.Match[str], Callable[[str, Location], Token]]
        ] = None

        for pattern, func in PATTERNS:
            re_match = pattern.match(source.text[offset:])
            if re_match is None:
                continue

            if best_match is None or len(re_match[0]) > len(best_match[0][0]):
                best_match = (re_match, func)

        assert (
            best_match is not None
        ), f"'{source.text[offset:]}' did not match any pattern!"

        re_match, func = best_match
        token_loc = source.get_location(offset, offset + len(re_match[0]))

        tokens.append(func(re_match[0], token_loc))

        offset += len(re_match[0])

    # Add EOF token
    tokens.append(Token(TokenType.EOF, source.get_location(offset, offset)))
    
    return tokens
