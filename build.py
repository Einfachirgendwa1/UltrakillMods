from os import system, listdir
from os.path import join, isdir, exists
from shutil import copy, rmtree, copytree
from subprocess import Popen

base_dir = r"C:\Users\Think\AppData\Roaming\r2modmanPlus-local\ULTRAKILL\profiles\dev\BepInEx\plugins"

print("Compiling projects")
system("dotnet build")

entries = []

for entry in listdir():
    print(f"Checking {entry}")
    if isdir(entry) and exists(join(entry, "BuildResult")):
        entries.append(entry)

        mod_dir = join(base_dir, f"Einfachirgendwa1-{entry}")
        print(f"[{entry}] Copying dlls")
        copy(f"{entry}/obj/Debug/netstandard2.1/{entry}.dll", f"{entry}/BuildResult/{entry}")
        copy(f"{entry}/bin/Debug/netstandard2.1/Common.dll", f"{entry}/BuildResult/{entry}")

        print(f"[{entry}] Deleting '{mod_dir}'")
        rmtree(mod_dir, ignore_errors=True)

        print(f"[{entry}] Copying new content to '{mod_dir}'")
        copytree(f"{entry}/BuildResult", mod_dir)

print(f"Installed: {entries}")

print("Opening ULTRAKILL")
Popen([r"C:\Program Files (x86)\Steam\steam.exe", "-applaunch", "1229490"])
