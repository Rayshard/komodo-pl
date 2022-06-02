package main

import (
	"fmt"
	"os"

	"github.com/Rayshard/komodo-pl/src/lexer"
	"github.com/Rayshard/komodo-pl/src/utils"
)

func main() {
	args := os.Args[1:]

	if len(args) != 1 {
		fmt.Println("Usage: komodo [input file path]")
		os.Exit(1)
	}

	inputFilePath := args[0]
	sourceFile, err := utils.SourceFileFromFile(inputFilePath)
	if err != nil {
		fmt.Printf("ERROR: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("%+v\n", lexer.Lex(sourceFile))
}
