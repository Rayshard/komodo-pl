#include "runtime.h"
#include "program.h"

int main(int argc, char **argv)
{
    Interpreter interpreter;

    Program::Main(interpreter);
    return 0;
}