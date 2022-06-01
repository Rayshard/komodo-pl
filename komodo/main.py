import sys

from utils import SourceFile

if __name__ == '__main__':
    if len(sys.argv) != 2:
        print("Expected 1 input file!")
        exit(1)

    source_file_path = sys.argv[1]
    source_file = SourceFile.from_file(source_file_path)

    print(source_file.text)
    
