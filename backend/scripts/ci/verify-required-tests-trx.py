#!/usr/bin/env python3
"""Verify that required test classes actually executed in a TRX result file."""

from __future__ import annotations

import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def fail(message: str) -> None:
    raise SystemExit(message)


if len(sys.argv) < 3:
    fail(
        "Usage: verify-required-tests-trx.py "
        "<trx-file> <required-class-or-method> [...]"
    )

trx_path = Path(sys.argv[1])
required = sys.argv[2:]

if not trx_path.is_file():
    fail(f"TRX file does not exist: {trx_path}")

try:
    root = ET.parse(trx_path).getroot()
except ET.ParseError as exc:
    fail(f"TRX is not valid XML: {trx_path}: {exc}")

counters = next(
    (element for element in root.iter() if local_name(element.tag) == "Counters"),
    None,
)
if counters is None:
    fail(f"TRX has no ResultSummary/Counters element: {trx_path}")


def count(name: str) -> int:
    value = counters.attrib.get(name)
    if value is None:
        return 0
    try:
        return int(value)
    except ValueError:
        fail(f"TRX counter '{name}' is not an integer: {value!r}")
        return 0


total = count("total")
executed = count("executed")
passed = count("passed")
failed = count("failed")
errors = count("error")
timeouts = count("timeout")
aborted = count("aborted")
passed_but_aborted = count("passedButRunAborted")

if total < 1 or executed < 1:
    fail(
        "Required test suite executed zero tests: "
        f"total={total}, executed={executed}, trx={trx_path}"
    )

if failed or errors or timeouts or aborted or passed_but_aborted:
    fail(
        "TRX contains unsuccessful execution results: "
        f"failed={failed}, error={errors}, timeout={timeouts}, "
        f"aborted={aborted}, passedButRunAborted={passed_but_aborted}, "
        f"trx={trx_path}"
    )

definitions: dict[str, tuple[str, str]] = {}

for element in root.iter():
    if local_name(element.tag) != "UnitTest":
        continue

    test_id = element.attrib.get("id")
    method = next(
        (child for child in element.iter() if local_name(child.tag) == "TestMethod"),
        None,
    )

    if not test_id or method is None:
        continue

    definitions[test_id] = (
        method.attrib.get("className") or "",
        method.attrib.get("name") or "",
    )

executed_names: set[str] = set()
executed_results = 0

for element in root.iter():
    if local_name(element.tag) != "UnitTestResult":
        continue

    outcome = (element.attrib.get("outcome") or "").strip()
    if outcome in {"", "Skipped", "NotExecuted"}:
        continue

    test_id = element.attrib.get("testId")
    if not test_id:
        continue

    class_name, method_name = definitions.get(test_id, ("", ""))
    if not class_name:
        continue

    executed_results += 1
    executed_names.add(class_name)

    if method_name:
        executed_names.add(f"{class_name}.{method_name}")

if executed_results < 1:
    fail(
        "TRX counters report execution but no executed UnitTestResult could be "
        f"mapped to a TestMethod: {trx_path}"
    )

missing = [name for name in required if name not in executed_names]

if missing:
    fail(
        "Required test class/method absent from executed TRX results: "
        + ", ".join(missing)
    )

print(
    "required-tests-trx: "
    f"total={total} executed={executed} passed={passed} "
    f"mapped_results={executed_results}"
)

for name in required:
    print(f"required-tests-trx: verified {name}")
