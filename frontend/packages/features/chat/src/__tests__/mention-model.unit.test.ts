import { describe, expect, it } from "vitest";
import {
  activeTrigger,
  buildMentionInputs,
  filterByQuery,
  insertMention,
  mentionWarnings,
  mentionsViewer,
  needsBroadcastConfirm,
  splitByMentions,
  trackedFromMessage,
  typesBroadcast,
  type TrackedMention,
} from "../core/model/mention-model";
import type { ChatMention, ChatMentionables } from "../core/types/chat";

const minh: TrackedMention = {
  kind: "user",
  label: "@Minh Quân",
  targetId: "u-minh",
};

describe("activeTrigger", () => {
  it("finds the @ the caret follows and the text typed after it", () => {
    expect(activeTrigger("hi @Mi", 6)).toEqual({
      trigger: "@",
      start: 3,
      query: "Mi",
    });
    expect(activeTrigger("#gen", 4)).toEqual({
      trigger: "#",
      start: 0,
      query: "gen",
    });
  });

  it("ignores an @ inside a word, after a new line, or followed by a space", () => {
    expect(activeTrigger("mail me@home", 12)).toBeNull();
    expect(activeTrigger("@Minh\nnext", 10)).toBeNull();
    expect(activeTrigger("# Title", 7)).toBeNull();
    expect(activeTrigger("no trigger", 10)).toBeNull();
  });

  it("closes when the query grows past a name", () => {
    expect(activeTrigger(`@${"a".repeat(40)}`, 41)).toBeNull();
  });
});

describe("filterByQuery", () => {
  const people = ["Trần Minh Quân", "Đặng Dũng", "Quang Anh"];
  it("matches without accents or case, names that start with the query first", () => {
    expect(filterByQuery(people, "quan", (n) => n)).toEqual([
      "Quang Anh",
      "Trần Minh Quân",
    ]);
    expect(filterByQuery(people, "dung", (n) => n)).toEqual(["Đặng Dũng"]);
    expect(filterByQuery(people, "", (n) => n)).toEqual(people);
  });
});

describe("insertMention", () => {
  it("replaces the trigger and its query with the label and a space", () => {
    const active = activeTrigger("hi @Mi there", 6)!;
    expect(insertMention("hi @Mi there", active, 6, "@Minh Quân")).toEqual({
      text: "hi @Minh Quân there",
      caret: 14,
    });
    const end = activeTrigger("hi @Mi", 6)!;
    expect(insertMention("hi @Mi", end, 6, "@Minh Quân").text).toBe(
      "hi @Minh Quân ",
    );
  });
});

describe("buildMentionInputs", () => {
  it("gives each mention its UTF-16 position in the content as sent", () => {
    const content = "😀 @Minh Quân, see #general and @here";
    const inputs = buildMentionInputs(
      content,
      [minh, { kind: "channel-link", label: "#general", targetId: "s-1" }],
      { canBroadcast: true },
    );
    expect(inputs).toEqual([
      { kind: "user", start: 3, length: 10, userId: "u-minh" },
      { kind: "channel-link", start: 19, length: 8, spaceId: "s-1" },
      { kind: "broadcast-here", start: 32, length: 5 },
    ]);
    for (const input of inputs) {
      expect(content.slice(input.start, input.start + input.length)).toMatch(
        /^[@#]/,
      );
    }
  });

  it("drops a label the user edited and one inside a longer word", () => {
    expect(
      buildMentionInputs("@Minh Quan", [minh], { canBroadcast: false }),
    ).toEqual([]);
    expect(
      buildMentionInputs("x@Minh Quân", [minh], { canBroadcast: false }),
    ).toEqual([]);
    expect(
      buildMentionInputs("@Minh Quânh", [minh], { canBroadcast: false }),
    ).toEqual([]);
  });

  it("prefers the longer label where two start at the same place", () => {
    const short: TrackedMention = {
      kind: "user",
      label: "@An",
      targetId: "u-a",
    };
    const long: TrackedMention = {
      kind: "user",
      label: "@An Binh",
      targetId: "u-b",
    };
    expect(
      buildMentionInputs("@An Binh hi", [short, long], { canBroadcast: false }),
    ).toEqual([{ kind: "user", start: 0, length: 8, userId: "u-b" }]);
  });

  it("sends @channel and @here only when the caller may broadcast", () => {
    expect(buildMentionInputs("@channel", [], { canBroadcast: false })).toEqual(
      [],
    );
    expect(buildMentionInputs("@Channel!", [], { canBroadcast: true })).toEqual(
      [{ kind: "broadcast-channel", start: 0, length: 8 }],
    );
    expect(typesBroadcast("hello @here")).toBe(true);
    expect(typesBroadcast("a@here")).toBe(false);
  });
});

describe("trackedFromMessage", () => {
  it("rebuilds the labels of a stored message so an edit keeps its mentions", () => {
    const mentions: ChatMention[] = [
      { kind: "user", start: 0, length: 10, userId: "u-minh", spaceId: null },
      {
        kind: "broadcast-here",
        start: 12,
        length: 5,
        userId: null,
        spaceId: null,
      },
    ];
    expect(trackedFromMessage("@Minh Quân @here", mentions)).toEqual([minh]);
  });
});

describe("splitByMentions", () => {
  it("splits the text at its mentions and shows a span that does not fit as text", () => {
    const mentions: ChatMention[] = [
      { kind: "user", start: 3, length: 4, userId: "u", spaceId: null },
      { kind: "user", start: 90, length: 4, userId: "u", spaceId: null },
    ];
    expect(splitByMentions("hi @Bao ok", mentions)).toEqual([
      { type: "text", text: "hi " },
      { type: "mention", mention: mentions[0], text: "@Bao" },
      { type: "text", text: " ok" },
    ]);
    expect(splitByMentions("plain", undefined)).toEqual([
      { type: "text", text: "plain" },
    ]);
  });
});

describe("mentionsViewer", () => {
  const base = { authorId: "u-other", deleted: false, kind: "user" as const };
  it("is true for the viewer's id, @channel and @here, never for their own message", () => {
    const own: ChatMention = {
      kind: "user",
      start: 0,
      length: 2,
      userId: "me",
      spaceId: null,
    };
    expect(mentionsViewer({ ...base, mentions: [own] }, "me")).toBe(true);
    expect(mentionsViewer({ ...base, mentions: [own] }, "you")).toBe(false);
    expect(
      mentionsViewer(
        {
          ...base,
          mentions: [{ ...own, kind: "broadcast-channel", userId: null }],
        },
        "you",
      ),
    ).toBe(true);
    expect(
      mentionsViewer({ ...base, authorId: "me", mentions: [own] }, "me"),
    ).toBe(false);
    expect(
      mentionsViewer({ ...base, deleted: true, mentions: [own] }, "me"),
    ).toBe(false);
  });
});

describe("warnings and confirmation", () => {
  const picker: ChatMentionables = {
    people: [
      { userId: "u-minh", inSpace: false },
      { userId: "u-in", inSpace: true },
    ],
    canBroadcast: true,
    broadcastConfirmAbove: 50,
    memberCount: 80,
  };

  it("warns about a person outside the space once, by the name the draft shows", () => {
    const inputs = buildMentionInputs("@Minh Quân @Minh Quân", [minh], {
      canBroadcast: true,
    });
    expect(
      mentionWarnings(inputs, [minh], picker, "@Minh Quân @Minh Quân", false),
    ).toEqual([{ code: "cannot-view", userId: "u-minh", label: "@Minh Quân" }]);
  });

  it("says a broadcast is plain text without the action or with no one else to reach", () => {
    expect(
      mentionWarnings(
        [],
        [],
        { ...picker, canBroadcast: false },
        "@here",
        true,
      ).map((w) => w.code),
    ).toEqual(["broadcast-not-allowed"]);
    expect(
      mentionWarnings([], [], { ...picker, memberCount: 1 }, "@here", true).map(
        (w) => w.code,
      ),
    ).toEqual(["no-audience"]);
  });

  it("asks for the confirmation only above the member threshold", () => {
    const inputs = buildMentionInputs("@channel", [], { canBroadcast: true });
    expect(needsBroadcastConfirm(inputs, picker)).toBe(true);
    expect(needsBroadcastConfirm(inputs, { ...picker, memberCount: 50 })).toBe(
      false,
    );
    expect(needsBroadcastConfirm([], picker)).toBe(false);
    expect(needsBroadcastConfirm(inputs, undefined)).toBe(false);
  });
});
