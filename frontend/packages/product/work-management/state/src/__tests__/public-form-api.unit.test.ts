import { describe, expect, it, vi } from "vitest";
import type { NotrelixClient } from "@notrelix/contracts";
import { createFormApi } from "../api/form.api";
import {
  publicAccessFailure,
  publicSubmitFailure,
} from "../mutations/use-form-writes";

function apiWith(methods: Record<string, unknown>) {
  return createFormApi({ api: methods } as unknown as NotrelixClient);
}

describe("public form api", () => {
  it("reads and switches the public access of a form", async () => {
    const get = vi.fn(async () => ({ isPublic: true, publicKey: "k1" }));
    const put = vi.fn(async () => ({ isPublic: false, publicKey: null }));
    const api = apiWith({ get, put });
    expect(await api.getFormPublicAccess("f-1")).toEqual({
      isPublic: true,
      publicKey: "k1",
    });
    expect(get).toHaveBeenCalledWith("/forms/f-1/public-access");
    expect(await api.setFormPublicAccess("f-1", false)).toEqual({
      isPublic: false,
      publicKey: null,
    });
    expect(put).toHaveBeenCalledWith("/forms/f-1/public-access", {
      enabled: false,
    });
  });

  it("reads the public form with no session refresh and maps its questions", async () => {
    const get = vi.fn(async () => ({
      name: "Khảo sát",
      description: null,
      captchaSiteKey: "site",
      questions: [
        {
          key: "kind",
          label: "Loại",
          type: "Select",
          required: true,
          options: [{ key: "a", label: "A" }],
        },
        { label: "no key" },
      ],
    }));
    const form = await apiWith({ get }).getPublicForm("a b");
    expect(get).toHaveBeenCalledWith("/public/forms/a%20b", {
      skipAuthRefresh: true,
      skipGlobalErrorToast: true,
    });
    expect(form).toMatchObject({
      name: "Khảo sát",
      description: "",
      captchaSiteKey: "site",
    });
    expect(form.questions).toHaveLength(1);
    expect(form.questions[0]).toMatchObject({
      id: "kind",
      key: "kind",
      type: "select",
      required: true,
      options: [{ key: "a", label: "A" }],
    });
  });

  it("sends answers, captcha token and honeypot to the key's submissions with no session refresh", async () => {
    const post = vi.fn(async () => null);
    await apiWith({ post }).submitPublicForm("k1", { title: "x" }, "tok");
    expect(post).toHaveBeenCalledWith(
      "/public/forms/k1/submissions",
      { answers: { title: "x" }, captchaToken: "tok", website: "" },
      { skipAuthRefresh: true, skipGlobalErrorToast: true },
    );
  });
});

describe("why a public write was refused", () => {
  it("tells links off from not allowed from failed", () => {
    expect(
      publicAccessFailure({
        status: 409,
        ruleCode: "WorkManagement_Form_PublicSharingDisabled",
      }),
    ).toEqual({ ok: false, reason: "linksOff" });
    expect(publicAccessFailure({ status: 403 })).toEqual({
      ok: false,
      reason: "denied",
    });
    expect(publicAccessFailure({ status: 404 }).reason).toBe("denied");
    expect(publicAccessFailure({ status: 500 }).reason).toBe("failed");
    expect(publicAccessFailure(new Error("offline")).reason).toBe("failed");
  });

  it("tells a refused captcha from refused answers, a missing form, a limit and a failure", () => {
    expect(
      publicSubmitFailure({
        status: 400,
        validationErrors: { captchaToken: ["Captcha verification failed."] },
      }),
    ).toEqual({ ok: false, reason: "captcha", errors: {} });
    expect(
      publicSubmitFailure({
        status: 400,
        validationErrors: { CaptchaToken: ["x"], email: ["Bad email"] },
      }),
    ).toEqual({ ok: false, reason: "captcha", errors: { email: "Bad email" } });
    expect(
      publicSubmitFailure({
        status: 400,
        validationErrors: { email: ["Bad email"] },
      }),
    ).toEqual({
      ok: false,
      reason: "validation",
      errors: { email: "Bad email" },
    });
    expect(publicSubmitFailure({ status: 404 }).reason).toBe("missing");
    expect(
      publicSubmitFailure({ status: 429, message: "Quá nhiều lần" }),
    ).toEqual({ ok: false, reason: "limited", message: "Quá nhiều lần" });
    expect(publicSubmitFailure({ status: 500 }).reason).toBe("failed");
  });
});
