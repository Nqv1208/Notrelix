import type { BoardMember, CardMember } from "@notrelix/work-management-core";

export function memberFixture(overrides: Partial<CardMember> = {}): CardMember {
  return {
    id: "member-test",
    userId: "user-test",
    name: "Avery Stone",
    initials: "AS",
    color: "#579bfc",
    ...overrides,
  };
}

export function boardMemberFixture(
  overrides: Partial<BoardMember> = {},
): BoardMember {
  const member = memberFixture(overrides);
  return {
    ...member,
    role: "editor",
    ...overrides,
  };
}
