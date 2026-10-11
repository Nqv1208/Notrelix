import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

describe("app toaster (WX-QUAL-001)", () => {
  const source = readFileSync(
    resolve(__dirname, "../providers/app-providers.tsx"),
    "utf8",
  );

  it("mounts the themed ui-web Toaster (z-toast role), not the raw sonner one with its own z-index", () => {
    expect(source).toMatch(
      /import \{[^}]*\bToaster\b[^}]*\} from "@notrelix\/ui-web"/,
    );
    expect(source).not.toMatch(/from "sonner"/);
  });
});
