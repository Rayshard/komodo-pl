package utils

import (
	"fmt"
	"os"
)

type Position struct {
	Line   int
	Column int
}

func (p Position) String() string {
	return fmt.Sprintf("%d:%d", p.Line, p.Column)
}

type Span struct {
	Start Position
	End   Position
}

func (s Span) String() string {
	return fmt.Sprintf("%v-%v", s.Start, s.End)
}

type Location struct {
	SourceFile *SourceFile
	Span       Span
}

func (l Location) String() string {
	return fmt.Sprintf("%s:%v", l.SourceFile.path, l.Span)
}

type Error struct {
	Location Location
	Message  string
}

type SourceFile struct {
	path       string
	text       string
	lineStarts []int
}

func newSourceFile(path string, text string) SourceFile {
	lineStarts := []int{0}

	for i, c := range text {
		if c == '\n' {
			lineStarts = append(lineStarts, i+1)
		}
	}

	return SourceFile{
		path:       path,
		text:       text,
		lineStarts: lineStarts,
	}
}

func SourceFileFromFile(path string) (*SourceFile, error) {
	bytes, err := os.ReadFile(path)
	if err != nil {
		return nil, err
	}

	sf := newSourceFile(path, string(bytes))
	return &sf, nil
}

func (sourceFile *SourceFile) Path() string {
	return sourceFile.path
}

func (sourceFile *SourceFile) Text() string {
	return sourceFile.text
}

func (sourceFile *SourceFile) Length() int {
	return len(sourceFile.text)
}

func (sourceFile *SourceFile) PositionFrom(offset int) Position {
	if offset < 0 || offset > sourceFile.Length() {
		panic(fmt.Sprintf("Specified offset '%d' is out of bounds: [0, %d]", offset, sourceFile.Length()))
	}

	position := Position{Line: 1, Column: 1}

	for line, lineStart := range sourceFile.lineStarts {
		if offset < lineStart {
			break
		}

		position = Position{
			Line:   line + 1,
			Column: offset - lineStart + 1,
		}
	}

	return position
}

func (sourceFile *SourceFile) LocationFrom(start int, end int) Location {
	return Location{
		SourceFile: sourceFile,
		Span: Span{
			Start: sourceFile.PositionFrom(start),
			End:   sourceFile.PositionFrom(end),
		},
	}
}
