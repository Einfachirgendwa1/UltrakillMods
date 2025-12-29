import zipfile
from os import walk
from os.path import join, relpath

from dev import *


# noinspection PyTypeChecker
def zip_directory(src: str, filename: str):
    with zipfile.ZipFile(filename, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in walk(src):
            for file in files:
                file_path = join(root, file)
                relative_name = relpath(file_path, src)
                zipf.write(file_path, relative_name)


build()
res = f"{Path.cwd().name}.zip"

print(f"Zipping directory to '{res}'")
zip_directory(f"BuildResult", res)
