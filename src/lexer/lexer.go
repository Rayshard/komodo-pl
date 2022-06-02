package lexer

import (
	"github.com/Rayshard/komodo-pl/src/lexer/token"
	"github.com/Rayshard/komodo-pl/src/utils"
)

func Lex(sourceFile *utils.SourceFile) []token.Token {
	var tokens []token.Token

	tokens = append(tokens, token.NewEOF(sourceFile.LocationFrom(0, 2)))

	return tokens
}
