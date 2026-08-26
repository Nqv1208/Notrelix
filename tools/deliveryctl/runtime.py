from pathlib import Path
import sys
MIN_PYTHON=(3,11)
ROOT=Path(__file__).resolve().parents[2]
def require_runtime():
    if sys.version_info < MIN_PYTHON:
        raise SystemExit(f"deliveryctl requires Python >=3.11; got {sys.version.split()[0]}")
    import tomllib  # noqa: F401
