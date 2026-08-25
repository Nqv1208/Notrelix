#!/usr/bin/env python3
from __future__ import annotations
import argparse, json, os
from delivery_model import ROOT, compact_json, component, github_output, load_catalog, load_images


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--component", required=True)
    p.add_argument("--github-output", action="store_true")
    p.add_argument("--field", default="")
    args = p.parse_args()
    catalog = load_catalog(ROOT)
    images = load_images(ROOT).get("images", {})
    cfg = component(catalog, args.component)
    container = cfg.get("container", {})
    frontend = cfg.get("frontend", {})
    build_args: list[str] = []
    for arg_name, lock_name in sorted(container.get("build_arg_locks", {}).items()):
        lock = images.get(lock_name)
        if not lock:
            raise SystemExit(f"component {args.component}: unknown image lock {lock_name}")
        ref = lock.get("ref", "")
        if "@sha256:" not in ref:
            raise SystemExit(f"component {args.component}: mutable build image lock {lock_name}={ref}")
        build_args.append(f"{arg_name}={ref}")
    data = {
        "component_id": args.component,
        "provider": cfg.get("provider", ""),
        "workspace": cfg.get("workspace", ""),
        "deployable": bool(cfg.get("deployable")),
        "proof_profile": cfg.get("proof_profile", ""),
        "container_context": container.get("context", ""),
        "dockerfile": container.get("dockerfile", ""),
        "image_name": container.get("image_name", ""),
        "compose_service": container.get("compose_service", ""),
        "deploy_env_var": container.get("deploy_env_var", ""),
        "runtime_port": str(container.get("runtime_port", "")),
        "health_path": container.get("health_path", "/"),
        "health_scheme": container.get("health_scheme", "http"),
        "build_args": build_args,
        "frontend_runtime": frontend.get("runtime", ""),
        "build_script": frontend.get("build_script", "build"),
        "e2e_script": frontend.get("e2e_script", ""),
        "artifact_paths": frontend.get("artifact_paths", []),
    }
    if args.field:
        if args.field not in data:
            raise SystemExit(f"unknown field: {args.field}")
        value = data[args.field]
        if isinstance(value, (list, dict)):
            print(compact_json(value))
        elif isinstance(value, bool):
            print(str(value).lower())
        else:
            print(value)
        return 0
    if args.github_output:
        for key, value in data.items():
            if isinstance(value, (list, dict)):
                github_output(key + "_json", compact_json(value))
            else:
                github_output(key, str(value).lower() if isinstance(value, bool) else str(value))
        # Multiline build args are easiest to consume through a generated file.
        path = os.path.join(os.environ.get("RUNNER_TEMP", "/tmp"), f"build-args-{args.component}.txt")
        with open(path, "w", encoding="utf-8") as handle:
            handle.write("\n".join(build_args) + ("\n" if build_args else ""))
        github_output("build_args_file", path)
    else:
        print(json.dumps(data, indent=2, sort_keys=True))
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
