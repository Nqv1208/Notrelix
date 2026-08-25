#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, re
from pathlib import Path
from delivery_model import ROOT, component, load_catalog, load_images

ENV_NAME = re.compile(r"^[A-Z][A-Z0-9_]*$")


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--application-manifest", required=True, type=Path)
    p.add_argument("--output", required=True, type=Path)
    p.add_argument("--combined-manifest", type=Path)
    args = p.parse_args()
    catalog = load_catalog(ROOT)
    locks = load_images(ROOT).get("images", {})
    app = json.loads(args.application_manifest.read_text(encoding="utf-8"))
    entries = []
    env: dict[str, str] = {}
    for artifact in app.get("artifacts", []):
        cid = artifact["component_id"]
        cfg = component(catalog, cid)
        var = cfg.get("container", {}).get("deploy_env_var", "")
        image = artifact.get("image", "")
        if not ENV_NAME.fullmatch(var) or "@sha256:" not in image:
            raise SystemExit(f"invalid application artifact {cid}: {var}={image}")
        env[var] = image
        entries.append({"id": cid, "kind": "application", "ref": image, "deploy_env_var": var, "compose_service": cfg.get("container", {}).get("compose_service", ""), "stateful": False})
    for image_id, lock in sorted(locks.items()):
        if lock.get("class") != "runtime":
            continue
        var = lock.get("deploy_env_var", "")
        ref = lock.get("ref", "")
        if not ENV_NAME.fullmatch(var) or "@sha256:" not in ref:
            raise SystemExit(f"invalid runtime image lock {image_id}: {var}={ref}")
        env[var] = ref
        entries.append({"id": image_id, "kind": "infrastructure", "ref": ref, "deploy_env_var": var, "compose_service": lock.get("compose_service", ""), "stateful": bool(lock.get("stateful"))})
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("".join(f"{key}={env[key]}\n" for key in sorted(env)), encoding="utf-8")
    combined = {"schema_version": 4, "images": sorted(entries, key=lambda x: (x["kind"], x["id"]))}
    if args.combined_manifest:
        args.combined_manifest.parent.mkdir(parents=True, exist_ok=True)
        args.combined_manifest.write_text(json.dumps(combined, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(combined, separators=(",", ":"), sort_keys=True))
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
