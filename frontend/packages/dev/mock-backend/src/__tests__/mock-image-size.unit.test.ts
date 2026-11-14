import { describe, expect, it } from "vitest";
import { mockImageSize } from "../contexts/chat/mock-image-size";

/** The mock reads the header the same way as the backend reader (UC-CHT-I01). */

function pngHeader(width: number, height: number): Uint8Array {
  const bytes = new Uint8Array(24);
  bytes.set(
    [
      0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0x0d, 0x49, 0x48,
      0x44, 0x52,
    ],
    0,
  );
  const view = new DataView(bytes.buffer);
  view.setUint32(16, width);
  view.setUint32(20, height);
  return bytes;
}

function gifHeader(width: number, height: number): Uint8Array {
  const bytes = new Uint8Array(13);
  bytes.set(new TextEncoder().encode("GIF89a"), 0);
  const view = new DataView(bytes.buffer);
  view.setUint16(6, width, true);
  view.setUint16(8, height, true);
  return bytes;
}

describe("mockImageSize", () => {
  it("reads a png and a gif", () => {
    expect(mockImageSize(pngHeader(640, 480), "image/png")).toEqual({
      width: 640,
      height: 480,
    });
    expect(mockImageSize(gifHeader(10, 20), "image/gif")).toEqual({
      width: 10,
      height: 20,
    });
  });

  it("reads a jpeg whose frame header is behind an application segment", () => {
    const jpeg = new Uint8Array([
      0xff, 0xd8, 0xff, 0xe0, 0x00, 0x04, 0x00, 0x00, 0xff, 0xc0, 0x00, 0x11,
      0x08, 0x01, 0x2c, 0x02, 0x58, 0x03, 0x01, 0x22, 0x00, 0x02, 0x11, 0x01,
      0x03, 0x11, 0x01,
    ]);

    expect(mockImageSize(jpeg, "image/jpeg")).toEqual({
      width: 600,
      height: 300,
    });
  });

  it("reads a lossless webp", () => {
    const bits = 99 | (49 << 14);
    const bytes = new Uint8Array(25);
    bytes.set(new TextEncoder().encode("RIFF"), 0);
    bytes.set(new TextEncoder().encode("WEBP"), 8);
    bytes.set(new TextEncoder().encode("VP8L"), 12);
    bytes[20] = 0x2f;
    new DataView(bytes.buffer).setUint32(21, bits, true);

    expect(mockImageSize(bytes, "image/webp")).toEqual({
      width: 100,
      height: 50,
    });
  });

  it("has no size for a truncated, garbage or non-image file, or an out of range side", () => {
    expect(
      mockImageSize(pngHeader(64, 32).slice(0, 20), "image/png"),
    ).toBeNull();
    expect(
      mockImageSize(
        new TextEncoder().encode("plain text, not an image at all"),
        "image/png",
      ),
    ).toBeNull();
    expect(mockImageSize(pngHeader(64, 32), "text/plain")).toBeNull();
    expect(mockImageSize(pngHeader(0, 32), "image/png")).toBeNull();
    expect(mockImageSize(pngHeader(20001, 32), "image/png")).toBeNull();
  });
});
