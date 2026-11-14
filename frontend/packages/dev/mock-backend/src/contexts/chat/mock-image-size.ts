/**
 * The pixel size of an uploaded image, read from its header the same way the backend does (PNG, JPEG, GIF and WebP only, header
 * bytes only, sane sides 1..20000). Anything else, or a header that cannot be read, has no size.
 */
export interface MockImageSize {
  readonly width: number;
  readonly height: number;
}

const MAX_SIDE = 20000;

export function mockImageSize(
  bytes: Uint8Array,
  contentType: string,
): MockImageSize | null {
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  const size = (() => {
    switch (contentType) {
      case "image/png":
        return png(bytes, view);
      case "image/jpeg":
        return jpeg(bytes, view);
      case "image/gif":
        return gif(bytes, view);
      case "image/webp":
        return webp(bytes, view);
      default:
        return null;
    }
  })();
  if (!size) return null;
  const sane = (side: number) => side >= 1 && side <= MAX_SIDE;
  return sane(size.width) && sane(size.height) ? size : null;
}

const PNG_SIGNATURE = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

function ascii(bytes: Uint8Array, offset: number, text: string): boolean {
  if (bytes.length < offset + text.length) return false;
  for (let i = 0; i < text.length; i += 1) {
    if (bytes[offset + i] !== text.charCodeAt(i)) return false;
  }
  return true;
}

function png(bytes: Uint8Array, view: DataView): MockImageSize | null {
  if (bytes.length < 24) return null;
  if (!PNG_SIGNATURE.every((b, i) => bytes[i] === b)) return null;
  if (!ascii(bytes, 12, "IHDR")) return null;
  return { width: view.getUint32(16), height: view.getUint32(20) };
}

function gif(bytes: Uint8Array, view: DataView): MockImageSize | null {
  if (bytes.length < 10) return null;
  if (!ascii(bytes, 0, "GIF87a") && !ascii(bytes, 0, "GIF89a")) return null;
  return {
    width: view.getUint16(6, true),
    height: view.getUint16(8, true),
  };
}

const NOT_FRAME_MARKERS = new Set([0xc4, 0xc8, 0xcc]);

function jpeg(bytes: Uint8Array, view: DataView): MockImageSize | null {
  if (bytes.length < 4 || bytes[0] !== 0xff || bytes[1] !== 0xd8) return null;
  let pos = 2;
  while (pos + 1 < bytes.length) {
    if (bytes[pos] !== 0xff) return null;
    const marker = bytes[pos + 1] ?? 0;
    pos += 2;
    if (marker === 0xff) {
      pos -= 1;
      continue;
    }
    if (
      marker === 0x01 ||
      marker === 0xd8 ||
      (marker >= 0xd0 && marker <= 0xd7)
    ) {
      continue;
    }
    if (marker === 0xd9 || marker === 0xda || pos + 2 > bytes.length) {
      return null;
    }
    const length = view.getUint16(pos);
    if (length < 2) return null;
    const isFrame =
      marker >= 0xc0 && marker <= 0xcf && !NOT_FRAME_MARKERS.has(marker);
    if (isFrame) {
      if (pos + 7 > bytes.length) return null;
      return {
        width: view.getUint16(pos + 5),
        height: view.getUint16(pos + 3),
      };
    }
    pos += length;
  }
  return null;
}

function webp(bytes: Uint8Array, view: DataView): MockImageSize | null {
  if (bytes.length < 20 || !ascii(bytes, 0, "RIFF") || !ascii(bytes, 8, "WEBP"))
    return null;
  if (ascii(bytes, 12, "VP8 ")) {
    if (
      bytes.length < 30 ||
      bytes[23] !== 0x9d ||
      bytes[24] !== 0x01 ||
      bytes[25] !== 0x2a
    )
      return null;
    return {
      width: view.getUint16(26, true) & 0x3fff,
      height: view.getUint16(28, true) & 0x3fff,
    };
  }
  if (ascii(bytes, 12, "VP8L")) {
    if (bytes.length < 25 || bytes[20] !== 0x2f) return null;
    const bits = view.getUint32(21, true);
    return { width: (bits & 0x3fff) + 1, height: ((bits >>> 14) & 0x3fff) + 1 };
  }
  if (ascii(bytes, 12, "VP8X")) {
    if (bytes.length < 30) return null;
    const u24 = (at: number) =>
      (bytes[at] ?? 0) |
      ((bytes[at + 1] ?? 0) << 8) |
      ((bytes[at + 2] ?? 0) << 16);
    return { width: u24(24) + 1, height: u24(27) + 1 };
  }
  return null;
}
