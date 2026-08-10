import { describe, expect, it } from "vitest";
import type {
  MobileButtonProps,
  MobileCardProps,
  MobileInputProps,
} from "../index";

describe("ui-mobile primitives contract", () => {
  it("MobileButtonProps requires title and is fully readonly", () => {
    const button: MobileButtonProps = { title: "Save" };
    expect(button.title).toBe("Save");
    expect(button.variant).toBeUndefined();
    expect(button.disabled).toBeUndefined();
  });

  it("MobileButtonProps accepts all variants", () => {
    const variants: MobileButtonProps["variant"][] = [
      "primary",
      "secondary",
      "outline",
      "ghost",
    ];
    for (const variant of variants) {
      const button: MobileButtonProps = { title: "x", variant };
      expect(button.variant).toBe(variant);
    }
  });

  it("MobileCardProps carries identity and display fields", () => {
    const card: MobileCardProps = {
      id: "c-1",
      title: "Board",
      subtitle: "sub",
    };
    expect(card.id).toBe("c-1");
    expect(card.title).toBe("Board");
    expect(card.subtitle).toBe("sub");
  });

  it("MobileInputProps carries text-field contract", () => {
    const input: MobileInputProps = {
      value: "",
      placeholder: "Search",
      disabled: false,
    };
    expect(input.value).toBe("");
    expect(input.placeholder).toBe("Search");
    expect(input.onChangeText).toBeUndefined();
  });

  it("ui-mobile barrel exports mobile primitives", async () => {
    const ui = await import("../index");
    expect(Object.keys(ui)).toEqual([
      "MobileButton",
      "MobileCard",
      "MobileInput",
    ]);
  });
});
