#!/bin/bash
KMD_BIN=Komodo/bin/Debug/net6.0/Komodo
TESTS_DIR=tests/
dotnet build && python test.py $KMD_BIN $TESTS_DIR