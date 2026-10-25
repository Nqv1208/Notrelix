import { describe, expect, it } from "vitest";
import { qrModules } from "../web/security/account-qr-code";

const URI =
  "otpauth://totp/Notrelix:ada%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=Notrelix";

describe("qrModules", () => {
  it("is deterministic and square for the same address", () => {
    const first = qrModules(URI);
    expect(qrModules(URI)).toEqual(first);
    // Version 1 is 21 modules wide and each version adds 4.
    expect((first.size - 17) % 4).toBe(0);
    expect(first.size).toBeGreaterThanOrEqual(21);
  });

  it("draws the three finder patterns of a QR code", () => {
    const { size, path } = qrModules(URI);
    const dark = new Set(
      [...path.matchAll(/M(\d+) (\d+)h1v1h-1z/g)].map(
        ([, x, y]) => `${Number(x) - 4},${Number(y) - 4}`,
      ),
    );
    const finder = (left: number, top: number) => {
      for (let row = 0; row < 7; row += 1)
        for (let column = 0; column < 7; column += 1) {
          const edge = row === 0 || row === 6 || column === 0 || column === 6;
          const core = row >= 2 && row <= 4 && column >= 2 && column <= 4;
          expect(dark.has(`${left + column},${top + row}`)).toBe(edge || core);
        }
    };
    finder(0, 0);
    finder(size - 7, 0);
    finder(0, size - 7);
  });

  it("grows with a longer address", () => {
    expect(
      qrModules(`${URI}&digits=6&period=30&algorithm=SHA1`).size,
    ).toBeGreaterThanOrEqual(qrModules(URI).size);
  });
});
