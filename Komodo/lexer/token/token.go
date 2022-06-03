package token

import (
	"fmt"

	"github.com/Rayshard/komodo-pl/src/utils"
)

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

func (tk TokenKind) String() string {
	switch tk {
	case TokenKindInvalid:
		return "Invalid"
	case TokenKindIntLit:
		return "IntLit"
	case TokenKindPlus:
		return "Plus"
	case TokenKindMinus:
		return "Minus"
	case TokenKindAsterisk:
		return "Asterisk"
	case TokenKindForwardSlash:
		return "ForwardSlash"
	case TokenKindEOF:
		return "EOF"
	default:
		panic(fmt.Sprintf("Not Implemented: %v", int(tk)))
	}
}

type Token struct {
	kind     TokenKind
	location utils.Location
	value    string
}

func (t Token) String() string {
	return fmt.Sprintf("%v(%s) %v", t.kind, t.value, t.location)
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
