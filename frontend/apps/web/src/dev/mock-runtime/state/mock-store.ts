import type { MockPersona, MockScenario } from "../config/mock-runtime-config";
import { createMockDatabase } from "../fixtures/default-dataset";
import { mockIds } from "./mock-ids";
import type { MockDatabase } from "./mock-database";
import { validateMockRelations } from "./validate-relations";

export class MockStore {
  private data: MockDatabase;
  private authenticated = true;
  private createdWorkspaceSequence = 0;
  private createdSequences = new Map<string, number>();

  constructor(
    private readonly persona: MockPersona,
    private readonly scenario: MockScenario,
  ) {
    this.data = createMockDatabase(persona, scenario);
    validateMockRelations(this.data);
  }

  getSnapshot(): Readonly<MockDatabase> {
    return structuredClone(this.data);
  }

  getCurrentUser() {
    const id = mockIds.users[this.persona];
    const user = this.data.users.find((candidate) => candidate.id === id);
    if (!user) throw new Error(`[Mock Runtime] Persona user missing: ${id}`);
    return structuredClone(user);
  }

  isAuthenticated(): boolean {
    return this.authenticated;
  }

  signOut(): void {
    this.authenticated = false;
  }

  getVisibleWorkspaces() {
    const userId = this.getCurrentUser().id;
    const visibleIds = new Set(
      this.data.memberships
        .filter((membership) => membership.userId === userId)
        .map((membership) => membership.workspaceId),
    );
    return structuredClone(
      this.data.workspaces.filter((workspace) => visibleIds.has(workspace.id)),
    );
  }

  nextWorkspaceId(): string {
    this.createdWorkspaceSequence += 1;
    return `mock-created-workspace-${String(this.createdWorkspaceSequence).padStart(4, "0")}`;
  }

  nextId(kind: string): string {
    const next = (this.createdSequences.get(kind) ?? 0) + 1;
    this.createdSequences.set(kind, next);
    return `mock-created-${kind}-${String(next).padStart(4, "0")}`;
  }

  update(mutator: (draft: MockDatabase) => void): void {
    const draft = structuredClone(this.data);
    mutator(draft);
    validateMockRelations(draft);
    this.data = draft;
  }

  reset(): void {
    this.data = createMockDatabase(this.persona, this.scenario);
    this.authenticated = true;
    this.createdWorkspaceSequence = 0;
    this.createdSequences.clear();
    validateMockRelations(this.data);
  }
}
