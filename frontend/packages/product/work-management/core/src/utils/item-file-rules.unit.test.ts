import { describe, expect, it } from "vitest";
import {
  BLOCKED_ITEM_FILE_EXTENSIONS,
  MAX_ITEM_FILE_BYTES,
  refuseItemFile,
} from "./item-file-rules";

describe("refuseItemFile (client convenience; the server decides)", () => {
  it("allows a file at the limit and refuses one byte over", () => {
    expect(
      refuseItemFile({ name: "a.pdf", size: MAX_ITEM_FILE_BYTES }),
    ).toBeNull();
    expect(
      refuseItemFile({ name: "a.pdf", size: MAX_ITEM_FILE_BYTES + 1 }),
    ).toBe("tooBig");
  });

  it.each(BLOCKED_ITEM_FILE_EXTENSIONS)("refuses .%s", (extension) => {
    expect(refuseItemFile({ name: `thing.${extension}`, size: 10 })).toBe(
      "blockedType",
    );
  });

  it("matches the extension case-insensitively and on the last dot only", () => {
    expect(refuseItemFile({ name: "SETUP.EXE", size: 1 })).toBe("blockedType");
    expect(refuseItemFile({ name: "archive.tar.sh", size: 1 })).toBe(
      "blockedType",
    );
    expect(refuseItemFile({ name: "run.sh.pdf", size: 1 })).toBeNull();
    expect(refuseItemFile({ name: "exe", size: 1 })).toBeNull();
    expect(refuseItemFile({ name: "bao-cao.xlsx", size: 1 })).toBeNull();
  });

  it("reports the size before the type when both apply", () => {
    expect(
      refuseItemFile({ name: "big.exe", size: MAX_ITEM_FILE_BYTES + 1 }),
    ).toBe("tooBig");
  });
});
