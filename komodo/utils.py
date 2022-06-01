from dataclasses import dataclass
from turtle import position


@dataclass
class Position:
    line: int
    column: int

    def __str__(self) -> str:
        return f"{self.line}:{self.column}"


@dataclass
class Span:
    start: Position
    end: Position

    def __str__(self) -> str:
        return f"{self.start}::{self.end}"


@dataclass
class Location:
    source_path: str
    span: Span

    def __str__(self) -> str:
        return f"{self.source_path}:{self.span}"


@dataclass
class Error:
    location: Location
    message: str

    def __str__(self) -> str:
        f"ERROR {self.location}\n\t{self.message}"


class SourceFile:
    def __init__(self, path: str, text: str) -> None:
        self.__path = path
        self.__text = text
        self.__line_starts = [0] + [
            i + 1 for i, letter in enumerate(self.__text) if letter == "\n"
        ]

    @property
    def path(self) -> str:
        return self.__path

    @property
    def text(self) -> str:
        return self.__text

    @property
    def length(self) -> int:
        return len(self.__text)

    # TODO: test
    def get_position(self, offset: int) -> Position:
        assert (
            0 <= offset <= len(self.__text)
        ), f"Specified offset '{offset}' is out of bounds: [0, {len(self.__text)}]"

        position = Position(1, 1)

        for line, line_start in enumerate(self.__line_starts):
            if offset < line_start:
                break

            position = Position(line + 1, offset - line_start + 1)

        return position

    def get_location(self, start: int, stop: int) -> Location:
        return Location(
            self.__path, Span(self.get_position(start), self.get_position(stop))
        )

    @staticmethod
    def from_file(path: str) -> "SourceFile":
        try:
            with open(path) as file:
                return SourceFile(path, file.read())
        except Exception as e:
            print(f"Could not open source file: {e}")
            exit(1)
