using Komodo.Utilities;
using Newtonsoft.Json.Schema;

namespace Komodo.Compilation.Bytecode;

public static class Schemas
{
    public static JSchema DataType() => Utility.EnumToJSchema<DataType>();

    public static class Instructions
    {
        public static JSchema PushI64() => JSchema.Parse($@"
            {{
                'type': 'object',
                'properties': {{
                    'opcode': {{
                        'type': 'string',
                        'pattern': '^PushI64$'
                    }},
                    'value': {{ 'type': 'integer' }}
                }},
                'required': [ 'opcode', 'value' ],
                'additionalProperties': false
            }}
        ");

        public static JSchema AddI64() => JSchema.Parse($@"
            {{
                'type': 'object',
                'properties': {{
                    'opcode': {{
                        'type': 'string',
                        'pattern': '^AddI64$'
                    }},
                }},
                'required': [ 'opcode' ],
                'additionalProperties': false
            }}
        ");

        public static JSchema Syscall() => JSchema.Parse($@"
            {{
                'type': 'object',
                'properties': {{
                    'opcode': {{
                        'type': 'string',
                        'pattern': '^Syscall$'
                    }},
                    'code': {Utility.EnumToJSchema<SyscallCode>()}
                }},
                'required': [ 'opcode', 'code' ],
                'additionalProperties': false
            }}
        ");

        public static JSchema GetSchema(Opcode opcode) => opcode switch
        {
            Opcode.PushI64 => PushI64(),  
            Opcode.AddI64 => AddI64(),  
            Opcode.Syscall => Syscall(),  
            _ => throw new Exception($"Schema for '{opcode}' not implemented")
        };
    }

    public static JSchema Instruction() => JSchema.Parse($@"
        {{ 'oneOf': [{Utility.Stringify(Enum.GetValues<Opcode>().Select(opcode => Instructions.GetSchema(opcode)), ", ")}] }}
    ");

    public static JSchema BasicBlock() => JSchema.Parse($@"
        {{
            'type': 'object',
            'properties': {{
                'name': {{ 'type': 'string' }},
                'instructions': {{
                    'type': 'array',
                    'items': {Instruction()}
                }}
            }},
            'required': [ 'name', 'instructions' ],
            'additionalProperties': false
        }}
    ");

    public static JSchema Function() => JSchema.Parse($@"
        {{
            'type': 'object',
            'properties': {{
                'name': {{ 'type': 'string' }},
                'args': {{
                    'type': 'array',
                    'items': {DataType()}
                }},
                'locals': {{
                    'type': 'array',
                    'items': {DataType()}
                }},
                'ret': {DataType()},
                'basicBlocks': {{
                    'type': 'array',
                    'items': {BasicBlock()}
                }}
            }},
            'required': [ 'name', 'args', 'locals', 'ret', 'basicBlocks' ],
            'additionalProperties': false
        }}
    ");

    public static JSchema Module() => JSchema.Parse($@"
        {{
            'type': 'object',
            'properties': {{
                'name': {{ 'type': 'string' }},
                'functions': {{
                    'type': 'array',
                    'items': {Function()}
                }}
            }},
            'required': [ 'name', 'functions' ],
            'additionalProperties': false
        }}
    ");

    public static JSchema Program() => JSchema.Parse($@"
        {{
            'type': 'object',
            'properties': {{
                'name': {{ 'type': 'string' }},
                'entry': {{
                    'type': 'object',
                    'properties': {{
                        'module': {{ 'type': 'string' }},
                        'function': {{ 'type': 'string' }}
                    }},
                    'required': [ 'module', 'function' ],
                    'additionalProperties': false
                }},
                'modules': {{
                    'type': 'array',
                    'items': {Module()}
                }}
            }},
            'required': [ 'name', 'entry', 'modules' ],
            'additionalProperties': false
        }}
    ");
}