import { describe, expect, it } from "vitest";
import { personInitials } from "./person-initials";

describe("personInitials", () => {
  it("draws Vietnamese names as the design does", () => {
    expect(personInitials("Trần Minh Quân")).toBe("MQ");
    expect(personInitials("Đỗ Khánh Linh")).toBe("KL");
    expect(personInitials("Phan Ngọc Anh")).toBe("NA");
    expect(personInitials("Lê Hoàng Phúc")).toBe("HP");
    expect(personInitials("Vũ Thanh")).toBe("VT");
    expect(personInitials("Lý Thu Trang")).toBe("LT");
    expect(personInitials("Phạm Thu Hằng")).toBe("PH");
    expect(personInitials("Nguyễn Thu Hà")).toBe("HA");
    expect(personInitials("Alex Rivera (Admin)")).toBe("AR");
    expect(personInitials("Hà")).toBe("HÀ");
  });
});
