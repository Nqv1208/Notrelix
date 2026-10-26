import { describe, expect, it } from "vitest";
import { onboardingSuggestions } from "../web/onboarding/onboarding-suggestions";

describe("onboardingSuggestions", () => {
  it("follows the size and the line of business read from the name", () => {
    const food = onboardingSuggestions(
      "l",
      "Công ty CP Thực phẩm Sao Mai",
      "vi",
    );
    expect(food.departments.map((entry) => entry.name)).toEqual([
      "Kinh doanh",
      "Kế toán",
      "Sản xuất",
      "Nhân sự",
    ]);
    expect(food.industry).toBe("food");
    expect(food.extras.map((entry) => entry.key)).toEqual([
      "mua-hang",
      "kho-van",
      "kiem-soat-chat-luong",
    ]);

    const studio = onboardingSuggestions("s", "Studio Hoa", "vi");
    expect(studio.departments).toHaveLength(2);
    expect(studio.industry).toBeNull();
    expect(studio.extras.map((entry) => entry.key)).toEqual([
      "marketing",
      "cham-soc-khach-hang",
    ]);
  });

  it("starts a company of more than 500 people from the 201 to 1000 list", () => {
    expect(
      onboardingSuggestions("xxl", "Tập đoàn", "vi").departments.map(
        (entry) => entry.key,
      ),
    ).toEqual(
      onboardingSuggestions("xl", "Tập đoàn", "vi").departments.map(
        (entry) => entry.key,
      ),
    );
  });

  it("keeps the same keys in both languages and words the rest in the chosen one", () => {
    const vi = onboardingSuggestions("m", "Acme", "vi");
    const en = onboardingSuggestions("m", "Acme", "en");
    expect(en.departments.map((entry) => entry.key)).toEqual(
      vi.departments.map((entry) => entry.key),
    );
    expect(en.departments.map((entry) => entry.name)).toEqual([
      "Sales",
      "Accounting",
      "Operations",
    ]);
    expect(en.boardTemplates.map((template) => template.id)).toEqual(
      vi.boardTemplates.map((template) => template.id),
    );
    expect(en.boardTemplates[0]?.stages).toEqual([
      "To do",
      "In progress",
      "In review",
      "Done",
    ]);
    expect(vi.boardTemplates[0]?.stages[0]).toBe("Cần làm");
  });
});
