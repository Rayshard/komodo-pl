@ECHO OFF
set KMD_BIN=Komodo/bin/Debug/net6.0/Komodo
set TESTS_DIR=tests/

dotnet build && python test.py %KMD_BIN% %TESTS_DIR%