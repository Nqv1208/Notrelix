import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";

import {
  SAO_MAI_CATALOG,
  SAO_MAI_SUMMARY,
  gettingStartedScenario,
  joinScenario,
  onboardingScenario,
  saoMaiDraft,
} from "../../../verification/workspace-onboarding-fixtures";
import { GettingStartedCard } from "../getting-started-card";
import { OnboardingJoinSurface } from "../onboarding-join-surface";
import {
  emptyDraft,
  shortCompanyName,
  slugFromName,
} from "../onboarding-models";
import { OnboardingSurface } from "../onboarding-surface";
import { onboardingCopy } from "../onboarding.copy";

const copy = onboardingCopy("vi");

/** Opens the styled Select behind a label and picks one option by its text. */
function choose(trigger: HTMLElement, option: string | RegExp) {
  fireEvent.keyDown(trigger, { key: "Enter" });
  fireEvent.click(screen.getByRole("option", { name: option }));
}

describe("onboarding: workspace step", () => {
  it("FUI[workspace.setup-flow:workspace] writes the name and size into the draft and waits for a free address", () => {
    const onDraft = vi.fn();
    const onNext = vi.fn();
    const { rerender } = renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          draft: emptyDraft(),
          address: { kind: "idle" },
          onDraft,
          onNext,
        })}
      />,
    );
    fireEvent.change(screen.getByLabelText(copy.workspace.nameLabel), {
      target: { value: "Công ty CP Thực phẩm Sao Mai" },
    });
    expect(onDraft).toHaveBeenLastCalledWith(
      expect.objectContaining({
        workspace: expect.objectContaining({
          name: "Công ty CP Thực phẩm Sao Mai",
        }),
      }),
    );
    fireEvent.click(
      screen.getByRole("radio", { name: copy.workspace.sizes.xl }),
    );
    expect(onDraft).toHaveBeenLastCalledWith(
      expect.objectContaining({
        workspace: expect.objectContaining({ size: "xl" }),
      }),
    );
    // Not free yet: Tiếp tục does not go on, and says what is missing.
    fireEvent.click(screen.getByRole("button", { name: copy.frame.next }));
    expect(onNext).not.toHaveBeenCalled();
    expect(screen.getByText(copy.workspace.nameRequired)).toBeTruthy();
    rerender(<OnboardingSurface {...onboardingScenario({ onNext })} />);
    fireEvent.click(screen.getByRole("button", { name: copy.frame.next }));
    expect(onNext).toHaveBeenCalledTimes(1);
    expect(screen.getByText("saomai.notrelix.vn còn trống")).toBeTruthy();
  });

  it("FUI[workspace.setup-flow:address] says a taken address is taken, offers the next free one and holds Tiếp tục", () => {
    const onUseSuggestion = vi.fn();
    const onNext = vi.fn();
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          address: { kind: "taken", suggestion: "saomai-food" },
          onUseSuggestion,
          onNext,
        })}
      />,
    );
    expect(
      screen.getByText("saomai đã có người dùng, thử saomai-food"),
    ).toBeTruthy();
    expect(
      screen
        .getByLabelText(copy.workspace.slugLabel)
        .getAttribute("aria-invalid"),
    ).toBe("true");
    fireEvent.click(screen.getByRole("button", { name: "Dùng saomai-food" }));
    expect(onUseSuggestion).toHaveBeenCalledWith("saomai-food");
    fireEvent.click(screen.getByRole("button", { name: copy.frame.next }));
    expect(onNext).not.toHaveBeenCalled();
    expect(screen.getByText(copy.workspace.footTaken)).toBeTruthy();
    // The picture beside the form says there is no address yet.
    expect(
      within(screen.getByLabelText(copy.preview.label)).getByText(
        copy.preview.noAddress,
      ),
    ).toBeTruthy();
  });

  it("FUI[workspace.setup-flow:locked] shows a created workspace as read-only", () => {
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          created: { workspace: true, departments: false },
        })}
      />,
    );
    expect(
      (screen.getByLabelText(copy.workspace.nameLabel) as HTMLInputElement)
        .readOnly,
    ).toBe(true);
    expect(screen.getByText(copy.workspace.lockedNote)).toBeTruthy();
  });

  it("FUI[workspace.setup-flow:exit] saves and exits from the header, and the last page has no exit", () => {
    const onExit = vi.fn();
    const { rerender } = renderPureUi(
      <OnboardingSurface {...onboardingScenario({ onExit })} />,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.frame.exit }));
    expect(onExit).toHaveBeenCalledTimes(1);
    rerender(
      <OnboardingSurface
        {...onboardingScenario({ step: "done", summary: SAO_MAI_SUMMARY })}
      />,
    );
    expect(screen.queryByRole("button", { name: copy.frame.exit })).toBeNull();
  });

  it("FUI[workspace.setup-flow:steps] marks the steps done, current and to do for assistive technology", () => {
    renderPureUi(
      <OnboardingSurface {...onboardingScenario({ step: "invite" })} />,
    );
    const steps = within(
      screen.getByRole("list", { name: copy.frame.progress }),
    ).getAllByRole("listitem");
    expect(steps).toHaveLength(4);
    expect(steps[0]?.textContent).toContain(copy.frame.srDone);
    expect(steps[2]?.getAttribute("aria-current")).toBe("step");
    expect(steps[2]?.textContent).toContain(copy.frame.srCurrent);
    expect(steps[3]?.textContent).toContain(copy.frame.srTodo);
  });
});

describe("onboarding: departments step", () => {
  it("FUI[workspace.setup-flow:departments] renames, recolors, removes and adds departments, and flags a shared color", () => {
    const onDraft = vi.fn();
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({ step: "departments", onDraft })}
      />,
    );
    // Sản xuất is the open one: its colors show, Lam is chosen.
    expect(
      screen.getByRole("radio", {
        name: copy.departments.colors.blue,
        checked: true,
      }),
    ).toBeTruthy();
    fireEvent.click(
      screen.getByRole("radio", { name: copy.departments.colors.coral }),
    );
    const colored = onDraft.mock.calls.at(-1)?.[0];
    expect(
      colored.departments.find(
        (entry: { key: string }) => entry.key === "san-xuat",
      ).color,
    ).toBe("coral");
    fireEvent.change(screen.getAllByLabelText(copy.departments.nameLabel)[0]!, {
      target: { value: "Bán hàng" },
    });
    expect(onDraft.mock.calls.at(-1)?.[0].departments[0].name).toBe("Bán hàng");
    fireEvent.click(screen.getByRole("button", { name: "Bỏ phòng Nhân sự" }));
    expect(onDraft.mock.calls.at(-1)?.[0].departments).toHaveLength(3);
    fireEvent.click(screen.getByRole("button", { name: copy.departments.add }));
    expect(onDraft.mock.calls.at(-1)?.[0].departments).toHaveLength(5);
    // A suggestion for the line of business adds that department.
    fireEvent.click(screen.getByRole("button", { name: /Mua hàng/ }));
    expect(onDraft.mock.calls.at(-1)?.[0].departments.at(-1).name).toBe(
      "Mua hàng",
    );
  });

  it("FUI[workspace.setup-flow:departments-create] counts what it creates and refuses two with one name", () => {
    const onNext = vi.fn();
    const { rerender } = renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({ step: "departments", onNext })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Tạo 4 phòng ban" }));
    expect(onNext).toHaveBeenCalledTimes(1);
    rerender(
      <OnboardingSurface
        {...onboardingScenario({
          step: "departments",
          onNext,
          draft: saoMaiDraft({
            departments: [
              { key: "a", name: "Kinh doanh", color: "coral" },
              { key: "b", name: "kinh doanh", color: "gold" },
            ],
          }),
        })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Tạo 2 phòng ban" }));
    expect(onNext).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("alert").textContent).toContain("trùng tên");
  });

  it("FUI[workspace.setup-flow:departments-locked] shows departments already created without a way to change them", () => {
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          step: "departments",
          created: { workspace: true, departments: true },
        })}
      />,
    );
    expect(screen.getByText(copy.departments.lockedNote)).toBeTruthy();
    expect(
      (
        screen.getAllByLabelText(
          copy.departments.nameLabel,
        )[0] as HTMLInputElement
      ).readOnly,
    ).toBe(true);
    expect(
      screen.queryByText(
        copy.departments.extrasIndustry.replace("{{industry}}", "thực phẩm"),
      ),
    ).toBeNull();
  });
});

describe("onboarding: invite step", () => {
  it("FUI[workspace.setup-flow:invite] fills rows, adds and pastes emails, and counts the invitations to send", () => {
    const onDraft = vi.fn();
    const onNext = vi.fn();
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({ step: "invite", onDraft, onNext })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Gửi 3 lời mời" }));
    expect(onNext).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: copy.invite.addRow }));
    expect(onDraft.mock.calls.at(-1)?.[0].invites).toHaveLength(5);
    fireEvent.click(screen.getByRole("button", { name: copy.invite.paste }));
    fireEvent.change(screen.getByLabelText(copy.invite.pasteTitle), {
      target: {
        value: "a@saomaifood.vn, b@saomaifood.vn\nTRAN.THI.LAN@saomaifood.vn",
      },
    });
    fireEvent.click(
      screen.getByRole("button", { name: copy.invite.pasteApply }),
    );
    const emails = onDraft.mock.calls
      .at(-1)?.[0]
      .invites.map((row: { email: string }) => row.email);
    // The pasted list is split, lower-cased and an email already listed is not added twice.
    expect(emails).toContain("a@saomaifood.vn");
    expect(emails).toContain("b@saomaifood.vn");
    expect(
      emails.filter((email: string) => email === "tran.thi.lan@saomaifood.vn"),
    ).toHaveLength(1);
  });

  it("FUI[workspace.setup-flow:invite-problems] says which email is wrong or repeated and does not send", () => {
    const onNext = vi.fn();
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          step: "invite",
          onNext,
          draft: saoMaiDraft({
            invites: [
              {
                id: "1",
                email: "khong-phai-email",
                departmentKey: "san-xuat",
                role: "Admin",
              },
              {
                id: "2",
                email: "lan@saomaifood.vn",
                departmentKey: "ke-toan",
                role: "Member",
              },
              {
                id: "3",
                email: "LAN@saomaifood.vn",
                departmentKey: "ke-toan",
                role: "Viewer",
              },
            ],
          }),
        })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Gửi 3 lời mời" }));
    expect(onNext).not.toHaveBeenCalled();
    expect(screen.getByText(copy.invite.invalidEmail)).toBeTruthy();
    expect(screen.getByText(copy.invite.duplicateEmail)).toBeTruthy();
  });

  it("FUI[workspace.setup-flow:invite-skip] goes on without invitations, and Back returns", () => {
    const onSkip = vi.fn();
    const onBack = vi.fn();
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({ step: "invite", onSkip, onBack })}
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.invite.skip }));
    fireEvent.click(screen.getByRole("button", { name: copy.frame.back }));
    expect(onSkip).toHaveBeenCalledTimes(1);
    expect(onBack).toHaveBeenCalledTimes(1);
  });
});

describe("onboarding: board step", () => {
  it("FUI[workspace.setup-flow:board] picks a template, names the board, puts it in a department and creates it", () => {
    const onDraft = vi.fn();
    const onNext = vi.fn();
    const { rerender } = renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          step: "board",
          onDraft,
          onNext,
          catalog: SAO_MAI_CATALOG,
          draft: saoMaiDraft({
            board: { templateId: "project", name: "", departmentKey: null },
          }),
        })}
      />,
    );
    fireEvent.click(screen.getByRole("radio", { name: /Tuyển dụng/ }));
    expect(onDraft.mock.calls.at(-1)?.[0].board.templateId).toBe("hiring");
    choose(screen.getByLabelText(copy.board.departmentLabel), "Sản xuất");
    expect(onDraft.mock.calls.at(-1)?.[0].board.departmentKey).toBe("san-xuat");
    // No name: the board is not made.
    fireEvent.click(screen.getByRole("button", { name: copy.board.create }));
    expect(onNext).not.toHaveBeenCalled();
    expect(screen.getByText(copy.board.nameRequired)).toBeTruthy();
    rerender(
      <OnboardingSurface {...onboardingScenario({ step: "board", onNext })} />,
    );
    fireEvent.click(screen.getByRole("button", { name: copy.board.create }));
    expect(onNext).toHaveBeenCalledTimes(1);
    expect(
      screen.getByText(/Thành viên phòng Sản xuất sẽ thấy board này/),
    ).toBeTruthy();
  });
});

describe("onboarding: done", () => {
  it("FUI[workspace.setup-flow:done] lists what was set up and the three tasks left, and goes to Home", () => {
    const onEnterHome = vi.fn();
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          step: "done",
          summary: SAO_MAI_SUMMARY,
          onEnterHome,
        })}
      />,
    );
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(
      "Sao Mai đã sẵn sàng",
    );
    const list = screen.getByRole("list", { name: copy.done.summaryLabel });
    expect(
      within(list).getByText("saomai.notrelix.vn", { exact: false }),
    ).toBeTruthy();
    expect(within(list).getByText("4 phòng ban")).toBeTruthy();
    expect(within(list).getByText("3 lời mời đã gửi")).toBeTruthy();
    expect(
      within(list).getByText("Board Ra mắt nước chấm Sao Mai"),
    ).toBeTruthy();
    expect(screen.getAllByText("Tạo việc đầu tiên").length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole("button", { name: copy.done.enter }));
    expect(onEnterHome).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.setup-flow:done-skipped] says plainly what was skipped", () => {
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          step: "done",
          summary: {
            ...SAO_MAI_SUMMARY,
            departments: [],
            invitesSent: 0,
            board: null,
          },
        })}
      />,
    );
    expect(screen.getByText(copy.done.noDepartments)).toBeTruthy();
    expect(screen.getByText(copy.done.noInvites)).toBeTruthy();
    expect(screen.getByText(copy.done.noBoard)).toBeTruthy();
    expect(screen.getByText(copy.done.items.firstItemNoBoard)).toBeTruthy();
  });

  it("FUI[workspace.setup-flow:pending] keeps the buttons from firing twice while a call is on its way and reports a lost network", () => {
    renderPureUi(
      <OnboardingSurface
        {...onboardingScenario({
          step: "board",
          pending: true,
          error: "unreachable",
        })}
      />,
    );
    expect(
      screen
        .getByRole("button", { name: copy.frame.saving })
        .hasAttribute("disabled"),
    ).toBe(true);
    expect(screen.getByRole("alert").textContent).toBe(copy.frame.unreachable);
  });
});

describe("onboarding: names", () => {
  it("FUI[workspace.setup-flow:names] reads the company as people say it and makes an address of it", () => {
    expect(shortCompanyName("Công ty CP Thực phẩm Sao Mai")).toBe("Sao Mai");
    expect(shortCompanyName("Công ty TNHH Hoa")).toBe("Hoa");
    expect(shortCompanyName("Minh An")).toBe("Minh An");
    expect(slugFromName("Công ty CP Thực phẩm Sao Mai")).toBe("saomai");
    expect(slugFromName("Minh An")).toBe("minhan");
  });
});

describe("onboarding: invited person", () => {
  it("FUI[workspace.onboarding-join:join] shows what they can open, edits the profile and the look, and starts", () => {
    const onDisplayName = vi.fn();
    const onMode = vi.fn();
    const onAccent = vi.fn();
    const onNotifyEmail = vi.fn();
    const onStart = vi.fn();
    const onLater = vi.fn();
    renderPureUi(
      <OnboardingJoinSurface
        {...joinScenario({
          onDisplayName,
          onMode,
          onAccent,
          onNotifyEmail,
          onStart,
          onLater,
        })}
      />,
    );
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(
      "Chào Trang, bạn đã vào Minh An Group",
    );
    expect(screen.getAllByText("Triển khai ERP Q4").length).toBeGreaterThan(0);
    expect(screen.getByText(/như Marketing/)).toBeTruthy();
    fireEvent.change(screen.getByLabelText(copy.join.nameLabel), {
      target: { value: "Lý Trang" },
    });
    expect(onDisplayName).toHaveBeenCalledWith("Lý Trang");
    fireEvent.click(screen.getByRole("radio", { name: copy.join.modes.dark }));
    expect(onMode).toHaveBeenCalledWith("dark");
    fireEvent.click(
      screen.getByRole("radio", { name: copy.join.accents.coral }),
    );
    expect(onAccent).toHaveBeenCalledWith("coral");
    fireEvent.click(
      screen.getByRole("radio", { name: copy.join.emailMentions }),
    );
    expect(onNotifyEmail).toHaveBeenCalledWith("mentions");
    expect(
      screen.getByText(/mặc định Xanh Notrelix; lựa chọn của bạn được ưu tiên/),
    ).toBeTruthy();
    // The avatar upload has no producer: it says why.
    expect(
      screen
        .getByRole("button", { name: copy.join.upload })
        .getAttribute("aria-disabled"),
    ).toBe("true");
    fireEvent.click(screen.getByRole("button", { name: copy.join.start }));
    fireEvent.click(screen.getByRole("button", { name: copy.frame.later }));
    expect(onStart).toHaveBeenCalledTimes(1);
    expect(onLater).toHaveBeenCalledTimes(1);
  });

  it("FUI[workspace.onboarding-join:none] explains an empty list of departments and an empty name", () => {
    renderPureUi(
      <OnboardingJoinSurface
        {...joinScenario({
          spaces: [],
          displayName: "",
          initials: "?",
          error: "name",
        })}
      />,
    );
    expect(screen.getByText(copy.join.noAccess)).toBeTruthy();
    expect(screen.getByRole("alert").textContent).toBe(copy.join.nameRequired);
  });
});

describe("getting started on Home", () => {
  it("FUI[workspace.getting-started:card] counts the done tasks, marks one, opens one and hides the list", () => {
    const onToggle = vi.fn();
    const onHide = vi.fn();
    const onOpen = vi.fn();
    const base = gettingStartedScenario();
    const { rerender } = renderPureUi(
      <GettingStartedCard
        {...base}
        onToggle={onToggle}
        onHide={onHide}
        onOpen={onOpen}
      />,
    );
    expect(screen.getByText("0/3")).toBeTruthy();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe(
      "0",
    );
    fireEvent.click(
      screen.getByRole("checkbox", {
        name: /Đánh dấu “Tạo việc đầu tiên” đã xong/,
      }),
    );
    expect(onToggle).toHaveBeenCalledWith("first-item");
    fireEvent.click(screen.getByRole("link", { name: "Mời thêm người" }));
    expect(onOpen).toHaveBeenCalledWith(
      expect.objectContaining({ key: "invite-more" }),
    );
    fireEvent.click(screen.getByRole("button", { name: copy.started.hide }));
    expect(onHide).toHaveBeenCalledTimes(1);
    rerender(
      <GettingStartedCard
        {...base}
        items={base.items.map((item, index) =>
          index === 0 ? { ...item, done: true } : item,
        )}
      />,
    );
    expect(screen.getByText("1/3")).toBeTruthy();
    expect(
      screen
        .getByRole("checkbox", { name: /Tạo việc đầu tiên/ })
        .hasAttribute("disabled"),
    ).toBe(true);
  });
});

describe("onboarding: the email is not confirmed yet", () => {
  it("FUI[workspace.setup-flow:workspace] offers the link again, says it went out, waits before the next and says when asking is refused", () => {
    const onResendConfirmation = vi.fn();
    const render = (resend: {
      status: "idle" | "sending" | "sent" | "limited" | "failed";
      secondsLeft: number;
    }) => (
      <OnboardingSurface
        {...onboardingScenario({
          error: "emailNotConfirmed",
          onResendConfirmation,
          resend,
        })}
      />
    );
    const { rerender } = renderPureUi(
      render({ status: "idle", secondsLeft: 0 }),
    );
    fireEvent.click(
      screen.getByRole("button", { name: copy.frame.resendConfirmation }),
    );
    expect(onResendConfirmation).toHaveBeenCalledTimes(1);

    // Sent: the notice stays, says so, and the button counts down.
    rerender(render({ status: "sent", secondsLeft: 59 }));
    expect(screen.getByText(copy.frame.emailNotConfirmed)).toBeTruthy();
    expect(screen.getByText(copy.frame.resendSent)).toBeTruthy();
    const waiting = screen.getByRole("button", { name: /Gửi lại sau 0:59/ });
    expect(waiting.hasAttribute("disabled")).toBe(true);

    rerender(render({ status: "limited", secondsLeft: 0 }));
    expect(screen.getByText(copy.frame.resendLimited)).toBeTruthy();
    rerender(render({ status: "failed", secondsLeft: 0 }));
    expect(screen.getByText(copy.frame.resendFailed)).toBeTruthy();
  });
});
