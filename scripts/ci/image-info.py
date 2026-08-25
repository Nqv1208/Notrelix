#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re

from delivery_model import ROOT, github_output, load_images

_TAG_RE = re.compile(r"^v?(\d+\.\d+\.\d+)(?:-([A-Za-z0-9][\w.-]*))?$")


def _derive_tag_fields(source: str) -> dict[str, str]:
    """Extract version and optional variant from an image tag like v1.61.1-jammy."""
    tag = source.rsplit(":", 1)[-1] if ":" in source else ""
    m = _TAG_RE.fullmatch(tag)
    if not m:
        return {}
    fields: dict[str, str] = {"version": m.group(1)}
    if m.group(2):
        fields["variant"] = m.group(2)
    return fields


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--image", required=True)
    p.add_argument("--field", default="")
    p.add_argument("--github-output", action="store_true")
    args = p.parse_args()
    images = load_images(ROOT).get("images", {})
    if args.image not in images:
        raise SystemExit(f"unknown image lock: {args.image}")
    data: dict[str, object] = {"name": args.image, **images[args.image]}
    # For tooling images, derive semantic version fields from the source tag.
    if data.get("class") == "tooling":
        data.update(_derive_tag_fields(str(data.get("source", ""))))
    if args.field:
        if args.field not in data:
            raise SystemExit(f"unknown field {args.field}")
        print(data[args.field])
        return 0
    if args.github_output:
        for k, v in data.items():
            github_output(k, str(v).lower() if isinstance(v, bool) else str(v))
    else:
        print(json.dumps(data, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
