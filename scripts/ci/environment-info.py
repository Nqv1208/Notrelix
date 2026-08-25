#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from delivery_model import load_environments, ROOT


def main() -> int:
    parser = argparse.ArgumentParser(description="Resolve one delivery environment contract")
    parser.add_argument("--environment", required=True)
    parser.add_argument("--require-promotion-mode")
    parser.add_argument("--field")
    args = parser.parse_args()

    environments = load_environments(ROOT).get("environments", {})
    if args.environment not in environments:
        raise SystemExit(f"unknown delivery environment: {args.environment}")
    cfg = environments[args.environment]
    if args.require_promotion_mode and cfg.get("promotion_mode") != args.require_promotion_mode:
        raise SystemExit(
            f"environment {args.environment} promotion_mode={cfg.get('promotion_mode')!r}; "
            f"expected {args.require_promotion_mode!r}"
        )
    if args.field:
        if args.field not in cfg:
            raise SystemExit(f"environment {args.environment} has no field {args.field}")
        value = cfg[args.field]
        if isinstance(value, bool):
            print(str(value).lower())
        else:
            print(value)
        return 0
    print(json.dumps({"environment": args.environment, **cfg}, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
