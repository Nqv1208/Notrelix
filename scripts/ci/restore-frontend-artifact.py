#!/usr/bin/env python3
from __future__ import annotations
import argparse, tarfile
from pathlib import Path
from delivery_model import ROOT


def safe_extract(tar: tarfile.TarFile, destination: Path) -> None:
    root = destination.resolve()
    for member in tar.getmembers():
        target = (destination / member.name).resolve()
        if target != root and root not in target.parents:
            raise SystemExit(f"unsafe tar member: {member.name}")
    tar.extractall(destination)


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--archive", required=True, type=Path)
    args = p.parse_args()
    with tarfile.open(args.archive, "r:gz") as tar:
        safe_extract(tar, ROOT / "frontend")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
