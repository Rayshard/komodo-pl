#!/bin/bash
KMD_BIN=Komodo/bin/Debug/net6.0/Komodo
TESTS_DIR=${1-tests}
dotnet build Komodo/Komodo.csproj && python test.py $KMD_BIN $TESTS_DIR