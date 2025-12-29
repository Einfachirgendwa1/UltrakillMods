from os import listdir, system
from pathlib import Path

for entry in listdir():
    if Path(entry).is_dir() and Path(f"{entry}/dev.py").exists() and entry != "ModTemplate":
        system(f"cd {entry} && python dev.py")
