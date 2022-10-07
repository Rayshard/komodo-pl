from abc import ABC, abstractmethod
from dataclasses import dataclass
from io import StringIO
import sys
import os
import subprocess
from pathlib import Path
from time import time
import traceback
from typing import Any, Dict, List, TypeVar, Generic
import json
import textwrap
import termcolor

T = TypeVar("T")


def indent(string: str, size: int) -> str:
    return textwrap.indent(string, " " * size)


@dataclass
class Result(Generic[T]):
    expected: T
    actual: T

    def ok(self) -> bool:
        return self.expected == self.actual


class TestResult:
    def __init__(
        self,
        name: str,
        exitcode: Result[int],
        stdout: Result[str],
        stderr: Result[str],
    ) -> None:
        self.__name = name
        self.__results: Dict[str, Result] = {
            "exitcode": exitcode,
            "stdout": stdout,
            "stderr": stderr,
        }

    def is_success(self) -> bool:
        return all([result.ok() for _, result in self.__results.items()])

    def __str__(self) -> str:
        if self.is_success():
            return f"SUCCESS: {self.__name}"

        with StringIO() as stream:
            stream.write(f"FAILED: {self.__name}\n")

            for key, result in self.__results.items():
                if result.ok():
                    continue

                print(f"    Mismatch on {key}", file=stream)
                print(f"        Expected", file=stream)
                print(textwrap.indent(str(result.expected), " " * 12), file=stream)
                print(f"        Actual", file=stream)
                print(textwrap.indent(str(result.actual), " " * 12), file=stream)
                print(bytes(result.expected, "utf-8"))
                print(bytes(result.actual, "utf-8"))

            return stream.getvalue()


@dataclass
class TestCase:
    name: str
    path: Path
    args: List[str]
    exit_code: int
    stdin: str
    stdout: str
    stderr: str

    def from_file(name: str, path: Path) -> "TestCase":
        with open(path, "r") as file:
            data = json.load(file)

        assert isinstance(data, dict)

        assert "args" in data
        assert isinstance(data["args"], list)
        assert all([isinstance(item, str) for item in data["args"]])

        assert "exitcode" in data and isinstance(data["exitcode"], int)

        assert "stdin" in data
        assert isinstance(data["stdin"], list)
        assert all([isinstance(item, str) for item in data["stdin"]])

        assert "stdout" in data
        assert isinstance(data["stdout"], list)
        assert all([isinstance(item, str) for item in data["stdout"]])

        assert "stderr" in data
        assert isinstance(data["stderr"], list)
        assert all([isinstance(item, str) for item in data["stderr"]])

        return TestCase(
            name=name,
            path=path,
            args=data["args"],
            exit_code=data["exitcode"],
            stdin=data["stdin"],
            stdout="\n".join(data["stdout"]),
            stderr="\n".join(data["stderr"]),
        )

    def run(kmd_bin_path: str, tc: "TestCase") -> TestResult:
        process = subprocess.run(
            [kmd_bin_path] + tc.args,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            cwd=tc.path.parent,
        )

        return TestResult(
            name=tc.name,
            exitcode=Result(tc.exit_code, process.returncode),
            stdout=Result(tc.stdout, process.stdout.decode("utf-8")),
            stderr=Result(tc.stderr, process.stderr.decode("utf-8")),
        )


def get_test_cases(test_suite_dir: Path) -> List[TestCase]:
    test_cases: List[TestCase] = []
    test_suite_dir = test_suite_dir.resolve()

    def helper(directory: Path) -> None:
        test_file_path = directory.joinpath("test.json")

        if test_file_path.exists():
            try:
                name = os.path.relpath(directory, test_suite_dir)

                if name == ".":
                    name = directory.name

                test_cases.append(TestCase.from_file(name, test_file_path))
            except Exception as e:
                print(f"Unable to load test case from {directory}:")
                traceback.print_exception(e)
        else:
            for path in os.scandir(directory):
                if path.is_dir():
                    helper(Path(path).resolve())

    helper(test_suite_dir)
    return test_cases


if __name__ == "__main__":
    args = sys.argv[1:]

    if len(args) != 2:
        print("Usage: test [kmd binary path] [test directory]")
        exit(-1)

    KMD_BIN = Path(args[0]).resolve()
    TESTS_DIR = Path(args[1]).resolve()

    test_cases = get_test_cases(TESTS_DIR)

    successes: List[TestResult] = []
    failures: List[TestResult] = []

    start_time = time()

    for tc in test_cases:
        result = TestCase.run(KMD_BIN, tc)
        if result.is_success():
            successes.append(result)
        else:
            failures.append(result)

    total_time = time() - start_time

    for success in successes:
        print(termcolor.colored(success, "green"))

    for failure in failures:
        print(termcolor.colored(failure, "red"))

    print(
        f"{termcolor.colored('Testing took', 'blue')}: {termcolor.colored(f'{total_time} seconds', 'yellow')}"
    )
    
    print(
        f"{termcolor.colored('Report', 'blue')}: {termcolor.colored(f'Passed {len(successes)}', 'green')}, {termcolor.colored(f'Failed {len(failures)}', 'red')}"
    )

    if len(failures) != 0:
        exit(-1)
