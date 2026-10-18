import { beforeAll, describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../components/ui/select";

describe("SelectItem description", () => {
  beforeAll(() => {
    // jsdom lacks the layout APIs the Radix listbox calls when it opens.
    Element.prototype.scrollIntoView = () => undefined;
  });

  it("shows the help as visible text, announces it as the option description and keeps it out of the trigger", () => {
    render(
      <Select open defaultValue="admin">
        <SelectTrigger aria-label="Role">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="admin" description="Manages members and settings.">
            Admin
          </SelectItem>
          <SelectItem value="viewer">Viewer</SelectItem>
        </SelectContent>
      </Select>,
    );

    const admin = screen.getByRole("option", { name: "Admin" });
    const describedBy = admin.getAttribute("aria-describedby");
    expect(describedBy).toBeTruthy();
    const help = document.getElementById(describedBy ?? "");
    expect(help?.textContent).toBe("Manages members and settings.");
    expect(admin.hasAttribute("title")).toBe(false);

    expect(
      screen
        .getByRole("option", { name: "Viewer" })
        .hasAttribute("aria-describedby"),
    ).toBe(false);

    const trigger = screen.getByRole("combobox", {
      name: "Role",
      hidden: true,
    });
    expect(trigger.textContent).toBe("Admin");
  });
});
