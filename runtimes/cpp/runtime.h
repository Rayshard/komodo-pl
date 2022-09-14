#pragma once

#include <iostream>
#include <string>
#include <vector>
#include <unordered_map>
#include <stdlib.h>
#include <chrono>

typedef int64_t KomodoI64;
typedef bool KomodoBool;

std::string ToString(KomodoI64 i) { return std::to_string(i); }
std::string ToString(KomodoBool b) { return b ? "true" : "false"; }

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
        KomodoI64 I64;
        KomodoBool Bool;
    } value;

public:
    DataType type;

    Value() {}
    Value(KomodoI64 v) : type(DataType::I64) { value.I64 = v; }
    Value(KomodoBool v) : type(DataType::Bool) { value.Bool = v; }

    KomodoI64 AsI64() const { return type == DataType::I64 ? value.I64 : throw Exceptions::ValueDerefException(DataType::I64, type); }
    KomodoBool AsBool() const { return type == DataType::Bool ? value.Bool : throw Exceptions::ValueDerefException(DataType::Bool, type); }

    operator KomodoI64() const { return AsI64(); }
    operator KomodoBool() const { return AsBool(); }
};

class Interpreter
{
    std::vector<Value> stack;
    long long start;

public:
    Interpreter()
    {
        start = std::chrono::duration_cast<std::chrono::nanoseconds>(std::chrono::high_resolution_clock::now().time_since_epoch()).count();
    }

    void PushStack(Value value) { stack.push_back(value); }

    Value PopStack()
    {
        auto value = stack.back();
        stack.pop_back();
        return value;
    }

    void Syscall(std::string name)
    {
        static std::unordered_map<std::string, void (*)(Interpreter & interpreter)> map =
            {{"Exit", [](auto interpreter)
              {
                  auto exitcode = interpreter.PopStack().AsI64();
                  long long end = std::chrono::duration_cast<std::chrono::nanoseconds>(std::chrono::high_resolution_clock::now().time_since_epoch()).count();

                  std::cout << "Finished in " << (end - interpreter.start) / 1000000000.0 << " seconds" << std::endl;
                  std::exit(exitcode);
              }},
             {"", [](auto interpreter) {}}};

        map[name](*this);
    }
};