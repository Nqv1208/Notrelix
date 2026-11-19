import { describe, expect, it } from "vitest";
import { parseChatAttachmentFrame } from "../core/realtime/chat-frames";

const added = {
  messageId: "m1",
  change: "added",
  attachmentId: "a1",
  fileName: "photo.png",
  sizeBytes: 10,
  contentType: "image/png",
  uploadedBy: "u1",
  createdAt: "2026-11-19T09:00:00Z",
  downloadPath: "/api/v1/x",
};

describe("attachment frame (UC-CHT-I01)", () => {
  it("carries an added image's pixel size", () => {
    expect(
      parseChatAttachmentFrame({ ...added, width: 640, height: 480 }),
    ).toMatchObject({ width: 640, height: 480 });
  });
  it("has no size for a file without one, and ignores a size that is not a positive whole number", () => {
    expect(parseChatAttachmentFrame(added)).toMatchObject({
      width: null,
      height: null,
    });
    expect(
      parseChatAttachmentFrame({ ...added, width: -1, height: "9" }),
    ).toMatchObject({ width: null, height: null });
  });
});
