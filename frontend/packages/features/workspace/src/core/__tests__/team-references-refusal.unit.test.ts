import { describe, expect, it } from "vitest";

import {
  parseTeamReferencesRefusal,
  TEAM_STILL_REFERENCED_CODE,
} from "../model/errors";

const refusal = (body: Record<string, unknown>) => ({
  status: 409,
  details: { errorCode: TEAM_STILL_REFERENCED_CODE, ...body },
});

describe("parseTeamReferencesRefusal", () => {
  it("reads the visible Spaces and the hidden flag, never a count", () => {
    const parsed = parseTeamReferencesRefusal(
      refusal({
        references: [
          {
            kind: "space",
            id: "s1",
            name: "Phòng Công nghệ",
            spaceType: "Department",
          },
        ],
        moreReferencesHidden: true,
      }),
    );
    expect(parsed).toEqual({
      references: [
        {
          kind: "space",
          id: "s1",
          name: "Phòng Công nghệ",
          spaceType: "Department",
        },
      ],
      moreHidden: true,
    });
  });

  it("an empty list with the flag stays a refusal", () => {
    expect(
      parseTeamReferencesRefusal(
        refusal({ references: [], moreReferencesHidden: true }),
      ),
    ).toEqual({ references: [], moreHidden: true });
  });

  it("drops malformed rows and defaults the flag to false", () => {
    expect(
      parseTeamReferencesRefusal(
        refusal({ references: [{ id: 3 }, null, { id: "s2", name: "B" }] }),
      ),
    ).toEqual({
      references: [{ kind: "space", id: "s2", name: "B", spaceType: "" }],
      moreHidden: false,
    });
  });

  it("any other failure is not a refusal", () => {
    expect(parseTeamReferencesRefusal(new Error("boom"))).toBeNull();
    expect(
      parseTeamReferencesRefusal({
        status: 409,
        details: { errorCode: "other" },
      }),
    ).toBeNull();
    expect(parseTeamReferencesRefusal(undefined)).toBeNull();
  });
});
