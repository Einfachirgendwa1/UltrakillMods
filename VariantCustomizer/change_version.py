import json
import pathlib


def replace_in_file(filename: str, old: str, new: str):
    with open(filename, "r") as file_read:
        content = file_read.read()

    new_content = content.replace(old, new)
    with open(filename, "w") as file_write:
        file_write.write(new_content)


with open("BuildResult/manifest.json", "r") as manifest:
    json_content = json.load(manifest)

current_version = json_content["version_number"]
print(f"Current version is: {current_version}")

new_version = input("Enter new version: ")

files_to_update = [
    "BuildResult/manifest.json",
    "BuildResult/README.md",
    "README.md",
]

cs_files = list(pathlib.Path("Src").rglob("*.cs"))

for file in files_to_update:
    replace_in_file(file, current_version, new_version)

for file in cs_files:
    replace_in_file(str(file), current_version, new_version)
