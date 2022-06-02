package token

import "github.com/Rayshard/komodo-pl/src/utils"

type TokenKind int

const (
	TokenKindInvalid TokenKind = iota
	TokenKindIntLit
	TokenKindPlus
	TokenKindMinus
	TokenKindAsterisk
	TokenKindForwardSlash
	TokenKindEOF
)

type Token struct {
	kind     TokenKind
	location utils.Location
	value    string
}

func NewInvalid(value string, location utils.Location) Token {
	return Token{
		kind:     TokenKindInvalid,
		location: location,
		value:    value,
	}
}

func NewIntLit(value string, location utils.Location) Token {
	return Token{
		kind:     TokenKindIntLit,
		location: location,
		value:    value,
	}
}

func NewPlus(location utils.Location) Token {
	return Token{
		kind:     TokenKindPlus,
		location: location,
		value:    "",
	}
}

func NewMinus(location utils.Location) Token {
	return Token{
		kind:     TokenKindMinus,
		location: location,
		value:    "",
	}
}

func NewAsterisk(location utils.Location) Token {
	return Token{
		kind:     TokenKindAsterisk,
		location: location,
		value:    "",
	}
}

func NewForwardSlash(location utils.Location) Token {
	return Token{
		kind:     TokenKindForwardSlash,
		location: location,
		value:    "",
	}
}

func NewEOF(location utils.Location) Token {
	return Token{
		kind:     TokenKindEOF,
		location: location,
		value:    "",
	}
}
