from os import system
from pathlib import Path
from shutil import copy, rmtree, copytree

base_dir = r"C:\Users\Think\AppData\Roaming\r2modmanPlus-local\ULTRAKILL\profiles\dev\BepInEx\plugins"
def get_mod_dir(project_name: str | None = None) -> str:
    if project_name is None: project_name = Path.cwd().name
    return f"{base_dir}/Einfachirgendwa1-{project_name}"

def build(src: str = ".", res: str | None = None):
    project_name = Path(src).stem if src != "." else Path.cwd().name

    if res is None: res = f"BuildResult/{project_name}"

    print(f"Compiling project {project_name}")
    system(f"cd {src} && dotnet build")

    print(f"Copying dlls")
    copy(f"{src}/obj/Debug/netstandard2.1/{project_name}.dll", res)
    copy(f"{src}/bin/Debug/netstandard2.1/Common.dll", res)

if __name__ == "__main__":
    build()

    mod_dir = get_mod_dir()
    print(f"Cleaning '{mod_dir}'")
    rmtree(mod_dir, ignore_errors=True)

    print("Copying build results")
    copytree("BuildResult", mod_dir)
