import { beforeAll, describe, expect, it } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { erpViewBar } from "../../../verification/view-state-fixtures";

beforeAll(() => {
  // Radix menus measure and scroll items; JSDOM has no layout.
  Element.prototype.scrollIntoView = () => undefined;
  Element.prototype.hasPointerCapture = () => false;
  Element.prototype.releasePointerCapture = () => undefined;
});

const dated = {
  filter: {
    join: "and" as const,
    conditions: [
      {
        fieldId: "field-erp-due",
        operator: "between" as const,
        values: ["2026-10-12", "2026-10-18"],
      },
    ],
  },
};

describe("the time-zone note on date filters", () => {
  it("names the workspace zone under the date row", () => {
    renderPureUi(
      <>{erpViewBar({ state: dated, timeZone: "Pacific/Kiritimati" })}</>,
    );
    fireEvent.click(screen.getByRole("button", { name: /^Lọc/ }));
    expect(
      screen.getByText(
        "Ngày tính theo múi giờ của không gian làm việc: Pacific/Kiritimati.",
      ),
    ).toBeTruthy();
  });

  it("tells a member who cannot read the setting that the default applies", () => {
    renderPureUi(
      <>
        {erpViewBar({
          state: dated,
          timeZone: "Asia/Ho_Chi_Minh",
          timeZoneIsDefault: true,
        })}
      </>,
    );
    fireEvent.click(screen.getByRole("button", { name: /^Lọc/ }));
    expect(
      screen.getByText(
        "Không đọc được múi giờ của không gian làm việc, đang dùng múi giờ mặc định Asia/Ho_Chi_Minh.",
      ),
    ).toBeTruthy();
  });
});
