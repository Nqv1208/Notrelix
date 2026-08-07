#!/usr/bin/env bash
set -euo pipefail

project="${1:?project path is required}"
group_name="${2:?group name is required}"
filter="${3:?filter expression is required}"
shift 3

if [[ "$#" -lt 1 ]]; then
  echo "At least one required class name is required."
  exit 2
fi

required_classes=("$@")
configuration="${DOTNET_CONFIGURATION:-Release}"

results_dir="TestResults/required/${group_name}"
trx_file="${results_dir}/${group_name}.trx"

rm -rf "${results_dir}"
mkdir -p "${results_dir}"

set +e
dotnet test "${project}" \
  --no-build \
  --configuration "${configuration}" \
  --filter "${filter}" \
  --logger "trx;LogFileName=${group_name}.trx" \
  --results-directory "${results_dir}" \
  --blame-hang-timeout 5m
dotnet_exit=$?
set -e

python3 - "${trx_file}" "${required_classes[@]}" <<'PY_TRX'
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

path = Path(sys.argv[1])
required_classes = sys.argv[2:]

if not path.is_file():
    raise SystemExit(f"Required TRX file was not created: {path}")

root = ET.parse(path).getroot()

counters = next(
    (element for element in root.iter() if element.tag.endswith("Counters")),
    None,
)
if counters is None:
    raise SystemExit(f"TRX has no ResultSummary/Counters element: {path}")

def number(name: str) -> int:
    value = counters.attrib.get(name)
    return int(value) if value is not None else 0

total = number("total")
executed = number("executed")
passed = number("passed")
failed = number("failed")

if counters.attrib.get("aborted") == "true":
    raise SystemExit(f"TRX reports an aborted run: {path}")

# Map executed UnitTestResult.testId -> UnitTest.id -> TestMethod.className/name.
unit_tests: dict[str, tuple[str, str]] = {}
for element in root.iter():
    if element.tag.endswith("UnitTest"):
        test_id = element.attrib.get("id")
        method = next(
            (child for child in element if child.tag.endswith("TestMethod")),
            None,
        )
        if test_id and method is not None:
            class_name = method.attrib.get("className") or ""
            method_name = method.attrib.get("name") or ""
            unit_tests[test_id] = (class_name, method_name)

executed_classes: set[str] = set()
for element in root.iter():
    if not element.tag.endswith("UnitTestResult"):
        continue
    outcome = element.attrib.get("outcome")
    if outcome not in ("Passed", "Failed"):
        continue  # count only executed outcomes; ignore Skipped/NotExecuted
    test_id = element.attrib.get("testId")
    class_name, method_name = unit_tests.get(test_id, ("", ""))
    if class_name:
        executed_classes.add(class_name)
        executed_classes.add(f"{class_name}.{method_name}")

print(
    f"required-test-filter: total={total} executed={executed} "
    f"passed={passed} failed={failed}"
)

if total < 1 or executed < 1:
    raise SystemExit(
        f"Required test filter executed zero tests: total={total}, executed={executed}"
    )

missing = [
    required
    for required in required_classes
    if required not in executed_classes
]
if missing:
    rendered = ", ".join(missing)
    raise SystemExit(
        f"Required class name(s) absent from executed TRX results: {rendered}"
    )

if failed != 0:
    raise SystemExit(f"Required test filter has {failed} failed test(s)")
PY_TRX

if [[ "${dotnet_exit}" -ne 0 ]]; then
  echo "dotnet test exited with ${dotnet_exit}"
  exit "${dotnet_exit}"
fi
