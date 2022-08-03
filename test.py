from abc import ABC, abstractmethod
from dataclasses import dataclass
from email import message
from io import StringIO
import sys
import os
import subprocess
from pathlib import Path
import traceback
from typing import Any, Dict, List, Optional, TypeVar, Generic
import json
import textwrap

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

            return stream.getvalue()


@dataclass
class TestCase:
    directory: str
    args: List[str]
    exit_code: int
    stdin: str
    stdout: str
    stderr: str

    @property
    def name(self) -> str:
        return os.path.basename(self.directory)

    def from_directory(directory: str) -> "TestCase":
        test_file_path = Path.joinpath(directory, "test.json")

        with open(test_file_path, "r") as file:
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
            directory=directory,
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
            cwd=tc.directory,
        )

        return TestResult(
            name=tc.name,
            exitcode=Result(tc.exit_code, process.returncode),
            stdout=Result(tc.stdout, process.stdout.decode("utf-8")),
            stderr=Result(tc.stderr, process.stderr.decode("utf-8")),
        )


def get_test_cases(test_suite_dir: Path) -> List[TestCase]:
    test_cases: List[TestCase] = []
    directories = [
        Path(path).resolve() for path in os.scandir(test_suite_dir) if path.is_dir()
    ]

    for directory in directories:
        try:
            test_cases.append(TestCase.from_directory(directory))
        except Exception as e:
            print(f"Unable to load test case from {directory}:")
            traceback.print_exception(e)
            continue

    return test_cases


if __name__ == "__main__":
    args = sys.argv[1:]

    if len(args) != 2:
        print("Usage: test [kmd binary path] [test directory]")
        exit(-1)

    KMD_BIN = Path(args[0]).resolve()
    TESTS_DIR = Path(args[1]).resolve()

    test_cases = get_test_cases(TESTS_DIR)

    for tc in test_cases:
        result = TestCase.run(KMD_BIN, tc)
        print(result)
