import glob
import json
import pathlib


def generate_lexer_tests():
    with open("tests/lexer_tests.rs", "w") as file:
        file.write("use komodo::compiler::{lexer::*, utilities::range::Range};\n")

        for test_file_path in glob.glob("tests/lexer/*.json"):
            with open(test_file_path) as test_file:
                test_contents = json.load(test_file)

            test_name = pathlib.Path(test_file_path).stem
            test_input = test_contents["input"]
            
            test_expected_tokens = []
            
            for et in test_contents["expected_tokens"]:
                et_range = et["range"]
                et_range_start = int(et_range["start"])
                et_range_length = int(et_range["length"])

                test_expected_tokens.append(
                    f"Token::new("
                    f"TokenKind::{et['kind']}, "
                    f"Range::new({et_range_start}, {et_range_length})"
                    ")"
                )
            
            test_expected_tokens = ",\n      ".join(test_expected_tokens)

            test_expected_errors = []
            
            for ee in test_contents["expected_errors"]:
                ee_range = ee["range"]
                ee_range_start = int(ee_range["start"])
                ee_range_length = int(ee_range["length"])
                ee_kind = ee["kind"]
                
                match ee_kind:
                    case "InvalidCharacter":
                        ee_kind = f"InvalidCharacter('{ee['value']}')"
                    case kind:
                        raise f"Unknown LexErrorKind: {kind}"
                
                test_expected_errors.append(
                    f"LexError::new("
                    f"Range::new({ee_range_start}, {ee_range_length}), "
                    f"LexErrorKind::{ee_kind}"
                    ")"
                )
            
            test_expected_errors = ",\n      ".join(test_expected_errors)

            file.write(
                 "\n"
                 "#[test]\n"
                f"fn {test_name}() {{\n"
                f"  let result = lex(\"{repr(test_input)[1:-1]}\");\n\n"
                f"  assert_eq!(result.tokens(), &[\n"
                f"      {test_expected_tokens}\n"
                 "  ]);\n\n"
                f"  assert_eq!(result.errors(),&[\n"
                f"      {test_expected_errors}\n"
                 "  ]);\n"
                 "}\n"
            )

if __name__ == '__main__':
    generate_lexer_tests()