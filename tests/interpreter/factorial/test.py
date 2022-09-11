from time import time


def factorial(number: int) -> int:
    return 1 if number == 0 else number * factorial(number - 1)

if __name__ == "__main__":
    start_time = time()
    result = factorial(20)
    end_time = time()

    print(result)
    print(f"Finished in {(end_time - start_time)} seconds")