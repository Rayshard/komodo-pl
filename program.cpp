#include <iostream>
#include <string>
#include <vector>

typedef int64_t KOMODO_I64;
typedef bool KOMODO_BOOL;

enum class DataType
{
    I64,
    Bool
};

std::string DataTypeToString(DataType dt)
{
    switch (dt)
    {
    case DataType::I64:
        return "I64";
    case DataType::Bool:
        return "Bool";
    default:
        throw std::runtime_error("Not Implemented");
    }
}

namespace Exceptions
{
    class Expection
    {
    public:
        virtual std::string GetMessage() = 0;
    };

    class ValueDerefException : Expection
    {
        DataType expected, actual;

    public:
        ValueDerefException(DataType expected, DataType actual)
            : expected(expected), actual(actual) {}

        std::string GetMessage()
        {
            return "Unable to deference value to " + DataTypeToString(expected) + ". It is a " + DataTypeToString(actual) + ".";
        }
    };
}

class Value
{
    union
    {
        KOMODO_I64 I64;
        KOMODO_BOOL Bool;
    } value;

public:
    DataType type;

    int64_t AsI64() { return type == DataType::I64 ? value.I64 : throw Exceptions::ValueDerefException(DataType::I64, type); }
};

class Interpreter
{
    std::vector<Value> stack;

public:
    Interpreter()
    {
    }

    void PushStack(Value value) { stack.push_back(value); }
};

namespace Program
{
    void Main(Interpreter &interpreter, KOMODO_I64 arg0, KOMODO_I64 arg1)
    {
        KOMODO_I64 local0;
        KOMODO_I64 local1;
        KOMODO_I64 local2;

        // ADD I64 local0 arg1 local2
        local2 = local0 + arg1;

        std::cout << "Hello, World!" << std::endl;
    }
}

int main(int argc, char **argv)
{
    Interpreter interpreter;
    Program::Main(interpreter);

    return 0;
}