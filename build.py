import zipfile
from os import system, listdir, walk
from os.path import join, isdir, exists, relpath
from shutil import copy

base_dir = r"C:\Users\Think\AppData\Roaming\r2modmanPlus-local\ULTRAKILL\profiles\dev\BepInEx\plugins"

print("Compiling projects")
system("dotnet build")

entries = []


# noinspection PyTypeChecker
def zip_directory(src: str, filename: str):
    with zipfile.ZipFile(filename, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in walk(src):
            for file in files:
                file_path = join(root, file)
                relative_name = relpath(file_path, src)
                zipf.write(file_path, relative_name)


for entry in listdir():
    if isdir(entry) and exists(join(entry, "BuildResult")):
        entries.append(entry)

        mod_dir = join(base_dir, f"Einfachirgendwa1-{entry}")
        print(f"[{entry}] Copying dlls")
        copy(f"{entry}/obj/Debug/netstandard2.1/{entry}.dll", f"{entry}/BuildResult/{entry}")
        copy(f"{entry}/bin/Debug/netstandard2.1/Common.dll", f"{entry}/BuildResult/{entry}")

        print(f"[{entry}] Zipping directory")
        zip_directory(f"{entry}/BuildResult", f"{mod_dir}.zip")
