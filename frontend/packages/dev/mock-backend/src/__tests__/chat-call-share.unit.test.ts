import { describe, expect, it } from "vitest";
import { actAs, chat, key, storeAs, USERS } from "./chat-test-kit";

type Store = ReturnType<typeof storeAs>;

/** The owner calls the member in a direct conversation and the member answers: an active two-person call. */
async function activeCall(
  store: Store,
  label: string,
  kind: "voice" | "video",
) {
  const dm = await chat(store, "POST", "/conversations", {
    idempotencyKey: key(`share-dm-${label}`),
    body: { kind: "direct", userId: USERS.member },
  });
  const space = dm.body.conversation?.id ?? dm.body.id;
  const started = await chat(store, "POST", `/spaces/${space}/calls`, {
    idempotencyKey: key(`share-call-${label}`),
    body: { kind },
  });
  const id = started.body.call.id as string;
  actAs(store, "member");
  const answered = await chat(store, "POST", `/calls/${id}/answer`, {
    body: { expectedVersion: 1 },
  });
  return { id, version: answered.body.call.version as number };
}

const share = (
  store: Store,
  id: string,
  version: number,
  sharingScreen: boolean,
  replaceShare?: boolean,
) =>
  chat(store, "PUT", `/calls/${id}/participants/me`, {
    body: {
      audioOn: true,
      videoOn: false,
      sharingScreen,
      expectedVersion: version,
      ...(replaceShare === undefined ? {} : { replaceShare }),
    },
  });

const sharers = (body: {
  participants: { personId: string; sharingScreen: boolean }[];
}) => body.participants.filter((p) => p.sharingScreen).map((p) => p.personId);

describe("MFB-CHAT-CALLS: one screen share at a time in the mock", () => {
  it("accepts a share in a voice call, and still refuses a camera there with the server's code", async () => {
    const store = storeAs("owner");
    const { id, version } = await activeCall(store, "voice", "voice");

    const shared = await share(store, id, version, true);
    expect(shared.status).toBe(200);
    expect(sharers(shared.body)).toEqual([USERS.member]);

    const camera = await chat(store, "PUT", `/calls/${id}/participants/me`, {
      body: {
        audioOn: true,
        videoOn: true,
        sharingScreen: false,
        expectedVersion: shared.body.version,
      },
    });
    expect(camera.status).toBe(400);
    expect(camera.body.errorCode).toBe("call-media-not-allowed");
  });

  it("shares, refuses a second share with 409, and replaces only when the caller confirms", async () => {
    const store = storeAs("owner");
    const { id, version } = await activeCall(store, "video", "video");

    const first = await share(store, id, version, true);
    expect(first.status).toBe(200);
    expect(sharers(first.body)).toEqual([USERS.member]);

    actAs(store, "owner");
    const taken = await share(store, id, first.body.version, true);
    expect(taken.status).toBe(409);
    expect(taken.body.errorCode).toBe("call-screen-share-taken");

    const replaced = await share(store, id, first.body.version, true, true);
    expect(replaced.status).toBe(200);
    expect(sharers(replaced.body)).toEqual([USERS.owner]);
    expect(replaced.body.version).toBe(first.body.version + 1);
  });

  it("clears the share when the sharer stops, and when the call ends", async () => {
    const store = storeAs("owner");
    const { id, version } = await activeCall(store, "stop", "video");
    const started = await share(store, id, version, true);
    const stopped = await share(store, id, started.body.version, false);
    expect(sharers(stopped.body)).toEqual([]);

    const again = await share(store, id, stopped.body.version, true);
    const ended = await chat(store, "POST", `/calls/${id}/end`, {
      body: { expectedVersion: again.body.version },
    });
    expect(ended.body.state).toBe("ended");
    expect(sharers(ended.body)).toEqual([]);
  });
});
