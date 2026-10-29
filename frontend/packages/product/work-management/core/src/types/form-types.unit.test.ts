import { describe, expect, it } from "vitest";
import {
  duplicateFieldMappings,
  orderKeyBetween,
  parseFormAnswers,
  parseFormQuestionType,
  parseFormStatus,
  parseFormSubmissionStatus,
  type FormQuestion,
} from "./form-types";

const question = (id: string, boardFieldId: string | null): FormQuestion => ({
  id,
  key: id,
  label: id,
  type: "text",
  required: false,
  boardFieldId,
  position: id,
  placeholder: null,
  helpText: null,
  maxLength: null,
  minValue: null,
  maxValue: null,
  options: [],
});

describe("form types", () => {
  it("reads the producer's words", () => {
    expect(parseFormQuestionType("MultiSelect")).toBe("multiselect");
    expect(parseFormQuestionType("Nope")).toBe("text");
    expect(parseFormStatus("Published")).toBe("published");
    expect(parseFormStatus("Closed")).toBe("closed");
    expect(parseFormStatus("Draft")).toBe("draft");
  });

  it("sorts a response into its inbox tab", () => {
    expect(parseFormSubmissionStatus("Accepted", null)).toBe("new");
    expect(parseFormSubmissionStatus("Accepted", "item-1")).toBe("processed");
    expect(parseFormSubmissionStatus("Rejected", null)).toBe("rejected");
    expect(parseFormSubmissionStatus("Spam", null)).toBe("spam");
    expect(parseFormSubmissionStatus("Deleted", null)).toBe("deleted");
  });

  it("keeps only an object of answers", () => {
    expect(parseFormAnswers('{"title":"a"}')).toEqual({ title: "a" });
    expect(parseFormAnswers("[1]")).toEqual({});
    expect(parseFormAnswers("bad")).toEqual({});
    expect(parseFormAnswers(null)).toEqual({});
  });

  it("finds the fields two questions share", () => {
    const dup = duplicateFieldMappings([
      question("a", "f1"),
      question("b", "f1"),
      question("c", "f2"),
      question("d", null),
    ]);
    expect([...dup.keys()]).toEqual(["f1"]);
    expect(dup.get("f1")?.map((q) => q.id)).toEqual(["a", "b"]);
  });

  it("makes order keys that sort where they were asked to go", () => {
    const first = orderKeyBetween(null, null);
    const second = orderKeyBetween(first, null);
    const before = orderKeyBetween(null, first);
    expect(before < first).toBe(true);
    expect(second > first).toBe(true);
    // Inserting again and again into the same gap keeps the order.
    let upper = second;
    for (let n = 0; n < 40; n += 1) {
      const next = orderKeyBetween(first, upper);
      expect(next > first && next < upper).toBe(true);
      upper = next;
    }
    expect(() => orderKeyBetween(second, first)).toThrow();
  });
});
