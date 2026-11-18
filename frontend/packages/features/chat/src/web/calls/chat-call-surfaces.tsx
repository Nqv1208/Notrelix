/**
 * The call surfaces of Chat, drawn on the call boards. The call this person holds takes the whole conversation pane (`ChatCallStage`:
 * ringing out, in the call, ended); an incoming ring is a top-right toast on a desktop or tablet and a full-screen sheet on a phone;
 * while the call goes on and the person reads a conversation, a slim bar sits in the pane's flow (`ChatCallStrip`). Nothing floats
 * over the composer or the message list: the board's picture-in-picture would cover the composer's right end, so the bar is in flow.
 * No control carries a shortcut hint; every state grows in place.
 */
import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  CheckIcon,
  ChevronDownIcon,
  InfoIcon,
  MessageSquareIcon,
  Maximize2,
  Mic,
  MicOff,
  Phone,
  PhoneOff,
  SettingsIcon,
  Video,
  VideoOff,
  WifiOff,
  X,
} from "lucide-react";
import { Avatar, AvatarFallback, Button, cn } from "@notrelix/ui-web";
import {
  callPhaseOf,
  counterpartOf,
  elapsedSeconds,
  formatCallTimer,
  type ChatCallPhase,
} from "../../core/model/call-model";
import type { ChatCall } from "../../core/types/call";
import type { ChatCopy } from "../components/chat.copy";
import { avatarToneOf, initialsOf } from "../components/chat-initials";
import { fillCopy } from "../components/use-chat-copy";
import { CALL_RECONNECT_ATTEMPTS } from "./chat-call-media";
import { useChatCallDevices } from "./chat-call-devices";
import { ChatCallDevicesDialog } from "./chat-call-devices-dialog";
import type { ChatCallsController, ChatCurrentCall } from "./use-chat-calls";

export interface ChatCallPeople {
  readonly viewerId: string;
  readonly nameOf: (userId: string) => string;
  /** The other person of a direct conversation, for a call the viewer started (nobody else has joined yet). */
  readonly peerOf?: (spaceId: string) => string | null;
}

function PersonAvatar({
  id,
  name,
  className,
}: {
  id: string;
  name: string;
  className: string;
}) {
  return (
    <Avatar className={className} aria-hidden="true">
      <AvatarFallback className={cn(avatarToneOf(id), "font-semibold")}>
        {initialsOf(name)}
      </AvatarFallback>
    </Avatar>
  );
}

function useNow(active: boolean): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    if (!active) return;
    setNow(Date.now());
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [active]);
  return now;
}

interface CallFacts {
  readonly call: ChatCall;
  readonly phase: ChatCallPhase;
  readonly otherId: string;
  readonly name: string;
  readonly isVideo: boolean;
  readonly timer: string;
  readonly status: string;
}

/** What every surface needs to say about the held call: who, which phase and the line under the name. */
function useCallFacts(
  held: ChatCurrentCall,
  people: ChatCallPeople,
  copy: ChatCopy,
): CallFacts {
  const c = copy.calls;
  const { call } = held;
  const peer = people.peerOf?.(call.spaceId) ?? null;
  const otherId =
    counterpartOf(call, people.viewerId, peer ? [peer] : []) ??
    call.initiatorId;
  const phase = callPhaseOf(call, people.viewerId, held.media === "connected");
  const now = useNow(phase === "connected");
  const timer = formatCallTimer(elapsedSeconds(call.answeredAt, now));
  const status =
    phase === "outgoing-ringing"
      ? c.ringingDots
      : held.media === "lost"
        ? c.lostShort
        : held.media === "reconnecting"
          ? c.reconnecting
          : phase === "connected"
            ? timer
            : phase === "connecting"
              ? c.connecting
              : "";
  return {
    call,
    phase,
    otherId,
    name: people.nameOf(otherId),
    isVideo: call.kind === "video",
    timer,
    status,
  };
}

const ROUND =
  "grid size-12 shrink-0 place-items-center rounded-full bg-inverse-foreground/15 text-inverse-foreground outline-none transition-colors hover:bg-inverse-foreground/25 focus-visible:ring-3 focus-visible:ring-ring/60 disabled:opacity-45 motion-reduce:transition-none [&_svg]:size-5";
const ROUND_ON =
  "bg-inverse-foreground text-inverse-surface hover:bg-inverse-foreground/90";
const ROUND_END =
  "bg-destructive text-destructive-foreground hover:bg-destructive/90 focus-visible:ring-destructive/50";

function RoundButton({
  label,
  caption = label,
  showLabel,
  className,
  children,
  ...rest
}: {
  label: string;
  /** The short words under the button; the full label stays its accessible name. */
  caption?: string;
  showLabel: boolean;
  className?: string;
  children: ReactNode;
} & Omit<React.ComponentProps<"button">, "children" | "className">) {
  return (
    <div className="flex flex-col items-center gap-1.5">
      <button
        type="button"
        aria-label={label}
        title={label}
        className={cn(ROUND, className)}
        {...rest}
      >
        {children}
      </button>
      {showLabel ? (
        <span aria-hidden="true" className="text-xs text-inverse-muted">
          {caption}
        </span>
      ) : null}
    </div>
  );
}

const PILL =
  "inline-flex h-12 items-center justify-center gap-2 rounded-full px-6 text-sm font-semibold outline-none transition-colors focus-visible:ring-3 focus-visible:ring-ring/60 motion-reduce:transition-none [&_svg]:size-4";
const PILL_NEUTRAL =
  "bg-inverse-foreground/15 text-inverse-foreground hover:bg-inverse-foreground/25";
const PILL_PRIMARY = "bg-primary text-primary-foreground hover:bg-primary/90";

function kindWord(isVideo: boolean, c: ChatCopy["calls"]): string {
  return isVideo ? c.kindVideo : c.kindVoice;
}

function endedTitle(
  reason: ChatCall["endReason"],
  name: string,
  c: ChatCopy["calls"],
): string {
  switch (reason) {
    case "declined":
      return fillCopy(c.endedDeclined, { name });
    case "missed":
      return fillCopy(c.endedMissed, { name });
    case "busy":
      return fillCopy(c.endedBusy, { name });
    case "cancelled":
      return c.endedCancelled;
    case "failed":
      return c.endedFailed;
    default:
      return c.endedCompleted;
  }
}

const clock = (iso: string | null): string =>
  iso
    ? new Intl.DateTimeFormat(undefined, {
        hour: "2-digit",
        minute: "2-digit",
        hourCycle: "h23",
      }).format(new Date(iso))
    : "";

export interface ChatCallStageProps extends ChatCallPeople {
  readonly controller: ChatCallsController;
  readonly held: ChatCurrentCall;
  /** False while the person reads a conversation: the stage stays mounted (the pictures keep playing) but hidden. */
  readonly visible: boolean;
  readonly onMinimize: () => void;
  readonly copy: ChatCopy;
}

export function ChatCallStage({
  controller,
  held,
  visible,
  onMinimize,
  copy,
  ...people
}: ChatCallStageProps) {
  const c = copy.calls;
  const facts = useCallFacts(held, people, copy);
  const { call, phase, otherId, name, isVideo } = facts;
  const ref = useRef<HTMLDivElement | null>(null);
  // The conversation under the stage is not reachable while the stage covers it.
  useEffect(() => {
    const host = ref.current?.parentElement;
    if (!visible || !host) return;
    const others = [...host.children].filter((el) => el !== ref.current);
    others.forEach((el) => el.setAttribute("inert", ""));
    return () => others.forEach((el) => el.removeAttribute("inert"));
  }, [visible]);

  const ended = call.state === "ended";
  const inCall = !ended && (phase === "connecting" || phase === "connected");
  const stageVideo = isVideo && inCall;
  const other = call.participants.find((p) => p.personId === otherId);
  const remoteVideoOn = Boolean(other?.videoOn && !other.leftAt);
  const failed = held.media === "failed" && !ended;
  const lost = held.media === "lost" && !ended;
  const reconnecting = held.media === "reconnecting" && !ended;
  // While the connection is down the stage underneath cannot be reached: only the overlay's buttons can.
  useEffect(() => {
    const stage = ref.current;
    if (!stage || !(lost || reconnecting)) return;
    const under = [...stage.children].filter(
      (el) => el.getAttribute("data-testid") !== "chat-call-reconnect",
    );
    under.forEach((el) => el.setAttribute("inert", ""));
    return () => under.forEach((el) => el.removeAttribute("inert"));
  }, [lost, reconnecting]);
  const completed = ended && call.endReason === "completed";
  const lasted =
    completed && call.answeredAt && call.endedAt
      ? formatCallTimer(
          (Date.parse(call.endedAt) - Date.parse(call.answeredAt)) / 1000,
        )
      : null;
  const topic = ended
    ? kindWord(isVideo, c)
    : phase === "outgoing-ringing"
      ? isVideo
        ? c.topicVideoOut
        : c.topicVoiceOut
      : kindWord(isVideo, c);
  const showMic = !ended;
  const showCamera = !ended && isVideo;

  // Devices are listed once the room is live (the browser shows their names only after it let the call use them).
  const [devicesOpen, setDevicesOpen] = useState(false);
  const [unplugged, setUnplugged] = useState<string | null>(null);
  const devices = useChatCallDevices({
    active: !ended && held.media === "connected" && call.state === "active",
    inUse: held.devices,
    onUnplugged: (kind, label) => {
      void controller.switchDevice(kind, null, false);
      setUnplugged(label || c.deviceDefault);
    },
  });

  return (
    <div
      ref={ref}
      role="region"
      aria-label={c.dockLabel}
      data-testid="chat-call-stage"
      data-phase={ended ? `ended-${call.endReason ?? "completed"}` : phase}
      data-video={stageVideo ? "on" : "off"}
      hidden={!visible}
      className={cn(
        "absolute inset-0 z-10 flex-col bg-inverse-surface leading-[1.45] text-inverse-foreground animate-in fade-in duration-200 motion-reduce:animate-none",
        visible ? "flex" : "hidden",
      )}
    >
      {stageVideo ? null : (
        <header className="flex h-16 shrink-0 items-center gap-3 border-b border-inverse-foreground/10 px-5">
          <h2 className="truncate text-lg leading-[1.45] font-semibold">
            {name}
          </h2>
          <span className="truncate text-xs text-inverse-muted">{topic}</span>
          <button
            type="button"
            aria-label={c.minimize}
            title={c.minimize}
            onClick={onMinimize}
            className="ms-auto grid size-9 shrink-0 place-items-center rounded-full text-inverse-muted outline-none transition-colors hover:bg-inverse-foreground/10 hover:text-inverse-foreground focus-visible:ring-3 focus-visible:ring-ring/60 motion-reduce:transition-none"
          >
            <ChevronDownIcon aria-hidden="true" className="size-5" />
          </button>
        </header>
      )}

      {stageVideo ? (
        <div className="relative min-h-0 flex-1" data-testid="chat-call-video">
          <video
            ref={controller.remoteVideoRef}
            autoPlay
            playsInline
            aria-label={fillCopy(c.remoteVideo, { name })}
            className={cn(
              "absolute inset-0 size-full object-cover transition-opacity duration-200 motion-reduce:transition-none",
              remoteVideoOn ? "opacity-100" : "opacity-0",
            )}
          />
          {remoteVideoOn ? null : (
            <div
              className="absolute inset-0 grid place-items-center bg-inverse-foreground/10"
              data-testid="chat-call-remote-tile"
            >
              <div className="flex flex-col items-center gap-2">
                <PersonAvatar
                  id={otherId}
                  name={name}
                  className="size-24 text-3xl"
                />
                <p className="text-sm text-inverse-muted">
                  {fillCopy(c.cameraOffTile, { name })}
                </p>
              </div>
            </div>
          )}
          <span
            role="timer"
            aria-label={c.timerLabel}
            className="absolute start-3.5 top-3.5 inline-flex h-6 items-center rounded-full bg-inverse-foreground/15 px-2.5 text-xs tabular-nums"
          >
            {facts.status}
          </span>
          <button
            type="button"
            aria-label={c.minimize}
            title={c.minimize}
            onClick={onMinimize}
            className="absolute end-3.5 top-3.5 grid size-9 place-items-center rounded-full bg-black/50 text-white outline-none transition-colors hover:bg-black/70 focus-visible:ring-3 focus-visible:ring-ring/60 motion-reduce:transition-none"
          >
            <ChevronDownIcon aria-hidden="true" className="size-5" />
          </button>
          <span className="absolute start-2 bottom-2 inline-flex items-center rounded-lg bg-black/70 px-2 py-[3px] text-xs text-white">
            {name}
          </span>
          <div
            data-testid="chat-call-self"
            className="absolute end-4 bottom-4 h-[210px] w-40 overflow-hidden rounded-[14px] border-2 border-white/70 bg-inverse-foreground/10"
          >
            <video
              ref={controller.localVideoRef}
              autoPlay
              muted
              playsInline
              aria-label={c.localVideo}
              className={cn(
                "size-full object-cover transition-opacity duration-200 motion-reduce:transition-none",
                held.videoOn ? "opacity-100" : "opacity-0",
              )}
            />
            <span className="absolute start-1.5 bottom-1.5 inline-flex items-center rounded-lg bg-black/70 px-2 py-[3px] text-xs text-white">
              {c.you}
            </span>
          </div>
        </div>
      ) : (
        <div className="flex min-h-0 flex-1 flex-col items-center justify-center gap-2.5 p-6 text-center">
          {completed ? (
            <span
              data-testid="chat-call-ended-icon"
              className="mb-2.5 grid size-[72px] place-items-center bg-success/20 text-success-text"
            >
              <CheckIcon aria-hidden="true" className="size-6" />
            </span>
          ) : (
            <span
              data-testid="chat-call-halo"
              className="mb-[34px] rounded-full shadow-[0_0_0_16px_rgb(255_255_255/0.07),0_0_0_36px_rgb(255_255_255/0.04)]"
            >
              <PersonAvatar
                id={otherId}
                name={name}
                className="size-[120px] text-4xl"
              />
            </span>
          )}
          <h3 className="text-2xl leading-[1.45] font-semibold">
            {ended ? endedTitle(call.endReason, name, c) : name}
          </h3>
          {completed ? (
            <>
              <p
                className="my-1 font-medium tabular-nums"
                // The board draws the length at 34px, between two type steps.
                style={{ fontSize: 34 }}
              >
                {lasted ?? "0:00"}
              </p>
              <p className="text-sm text-inverse-muted">
                {fillCopy(c.endedWith, {
                  kind: kindWord(isVideo, c),
                  name,
                })}
              </p>
            </>
          ) : ended ? (
            <p className="text-sm text-inverse-muted">
              {fillCopy(c.endedAt, { time: clock(call.endedAt) })}
            </p>
          ) : (
            <>
              <p
                role="timer"
                aria-label={c.timerLabel}
                className="text-sm text-inverse-muted tabular-nums"
              >
                {facts.status}
              </p>
              <span
                data-testid="chat-call-kind"
                className="mt-1.5 inline-flex h-7 items-center gap-1.5 rounded-full bg-inverse-foreground/10 px-2.5 text-xs"
              >
                {isVideo ? (
                  <Video aria-hidden="true" className="size-3.5" />
                ) : (
                  <Phone aria-hidden="true" className="size-3.5" />
                )}
                {kindWord(isVideo, c)}
              </span>
            </>
          )}
          {ended ? (
            <div
              data-testid="chat-call-actions"
              className="mt-[18px] flex flex-wrap items-center justify-center gap-2.5"
            >
              <button
                type="button"
                className={cn(PILL, PILL_PRIMARY)}
                onClick={() => {
                  controller.dismiss();
                  controller.start(call.spaceId, call.kind);
                }}
              >
                {isVideo ? (
                  <Video aria-hidden="true" />
                ) : (
                  <Phone aria-hidden="true" />
                )}
                {c.redial}
              </button>
              {completed ? null : (
                <button
                  type="button"
                  className={cn(PILL, PILL_NEUTRAL)}
                  onClick={controller.dismiss}
                >
                  <MessageSquareIcon aria-hidden="true" />
                  {c.message}
                </button>
              )}
              <button
                type="button"
                aria-label={c.close}
                className={cn(PILL, PILL_NEUTRAL)}
                onClick={controller.dismiss}
              >
                {c.close}
              </button>
            </div>
          ) : null}
        </div>
      )}

      {unplugged && !ended ? (
        <p
          role="status"
          className="mx-auto mb-2 flex max-w-xl items-center gap-2 rounded-full bg-warning-subtle px-4 py-1.5 text-sm text-warning-subtle-foreground"
        >
          <InfoIcon aria-hidden="true" className="size-4 shrink-0" />
          {fillCopy(c.deviceUnplugged, { label: unplugged })}
        </p>
      ) : null}
      {failed ? (
        <div
          role="alert"
          className="mx-auto mb-2 flex items-center gap-3 rounded-full bg-destructive/20 px-4 py-1.5 text-sm text-inverse-danger"
        >
          <span>{c.failedConnect}</span>
          <Button
            variant="outline"
            size="xs"
            className="border-inverse-foreground/30 bg-transparent text-inverse-foreground hover:bg-inverse-foreground/15"
            onClick={controller.retryConnect}
          >
            {c.retry}
          </Button>
        </div>
      ) : null}
      {held.deviceNotice ? (
        <p
          role="status"
          className="mx-auto mb-2 max-w-md px-4 text-center text-xs text-inverse-muted"
        >
          {held.deviceNotice === "mic-denied" ? c.micDenied : c.cameraDenied}
        </p>
      ) : null}

      {ended ? null : (
        <div
          className={cn(
            "flex shrink-0 items-center justify-center",
            stageVideo ? "h-20 gap-3.5 p-4" : "h-[104px] gap-2.5 px-4",
          )}
          data-testid="chat-call-controls"
        >
          {showMic ? (
            <RoundButton
              label={held.audioOn ? c.mute : c.unmute}
              caption={held.audioOn ? c.muteShort : c.unmuteShort}
              showLabel={!stageVideo}
              aria-pressed={!held.audioOn}
              className={held.audioOn ? undefined : ROUND_ON}
              onClick={controller.toggleMicrophone}
            >
              {held.audioOn ? (
                <Mic aria-hidden="true" />
              ) : (
                <MicOff aria-hidden="true" />
              )}
            </RoundButton>
          ) : null}
          {showCamera ? (
            <RoundButton
              label={held.videoOn ? c.cameraOff : c.cameraOn}
              caption={c.cameraShort}
              showLabel={!stageVideo}
              aria-pressed={!held.videoOn}
              className={held.videoOn ? undefined : ROUND_ON}
              onClick={controller.toggleCamera}
            >
              {held.videoOn ? (
                <Video aria-hidden="true" />
              ) : (
                <VideoOff aria-hidden="true" />
              )}
            </RoundButton>
          ) : null}
          <RoundButton
            label={c.devicesOpen}
            caption={c.devicesShort}
            showLabel={!stageVideo}
            onClick={() => {
              devices.refresh();
              setDevicesOpen(true);
            }}
          >
            <SettingsIcon aria-hidden="true" />
          </RoundButton>
          <RoundButton
            label={phase === "outgoing-ringing" ? c.cancel : c.hangUp}
            showLabel={!stageVideo}
            className={ROUND_END}
            onClick={controller.hangUp}
          >
            <PhoneOff aria-hidden="true" />
          </RoundButton>
        </div>
      )}

      {lost || reconnecting ? (
        <section
          role={lost ? "alert" : "status"}
          aria-label={lost ? c.lostTitle : c.reconnectTitle}
          data-testid="chat-call-reconnect"
          data-state={lost ? "lost" : "reconnecting"}
          className="absolute inset-0 z-20 flex flex-col items-center justify-center gap-3 bg-inverse-surface/80 p-8 text-center animate-in fade-in duration-200 motion-reduce:animate-none"
        >
          <span className="grid size-16 place-items-center rounded-full bg-inverse-foreground/10">
            <WifiOff aria-hidden="true" className="size-8" />
          </span>
          <h3 className="text-xl font-semibold">
            {lost ? c.lostTitle : c.reconnectTitle}
          </h3>
          <p className="max-w-md text-inverse-muted">
            {lost
              ? c.lostBody
              : held.attempt
                ? fillCopy(c.reconnectAttempt, {
                    attempt: String(held.attempt),
                    max: String(CALL_RECONNECT_ATTEMPTS),
                  })
                : c.reconnectWait}
          </p>
          <div className="mt-1.5 flex flex-wrap items-center justify-center gap-2.5">
            {lost ? (
              <button
                type="button"
                className={cn(PILL, PILL_PRIMARY)}
                onClick={controller.retryConnect}
              >
                {c.retry}
              </button>
            ) : null}
            <button
              type="button"
              className={cn(PILL, PILL_NEUTRAL)}
              onClick={controller.hangUp}
            >
              {c.leaveCall}
            </button>
          </div>
        </section>
      ) : null}
      {ended ? null : (
        <ChatCallDevicesDialog
          open={devicesOpen}
          onOpenChange={setDevicesOpen}
          controller={controller}
          held={held}
          devices={devices}
          onChosen={() => setUnplugged(null)}
          copy={copy}
        />
      )}
    </div>
  );
}

const TOAST_BTN =
  "inline-flex h-10 flex-1 items-center justify-center gap-2 rounded-full px-[18px] text-sm font-semibold outline-none transition-colors focus-visible:ring-3 focus-visible:ring-ring/60 motion-reduce:transition-none [&_svg]:size-4";

export interface ChatCallIncomingProps {
  readonly controller: ChatCallsController;
  readonly nameOf: (userId: string) => string;
  readonly isPhone: boolean;
  readonly copy: ChatCopy;
}

/** The incoming rings: a top-right toast (top-left of the pane on a tablet), a full-screen sheet on a phone. */
export function ChatCallIncoming({
  controller,
  nameOf,
  isPhone,
  copy,
}: ChatCallIncomingProps) {
  const c = copy.calls;
  const { incoming } = controller;
  if (incoming.length === 0) return null;
  if (isPhone) {
    const ring = incoming[0]!;
    const name = nameOf(ring.initiatorId);
    const video = ring.kind === "video";
    return (
      <section
        role="alertdialog"
        aria-label={fillCopy(video ? c.incomingVideo : c.incomingVoice, {
          name,
        })}
        data-testid="chat-call-incoming"
        data-layout="sheet"
        className="fixed inset-0 z-modal flex flex-col items-center bg-inverse-surface px-6 pt-[calc(env(safe-area-inset-top)+72px)] pb-[calc(env(safe-area-inset-bottom)+32px)] leading-[1.45] text-inverse-foreground animate-in fade-in duration-200 motion-reduce:animate-none"
      >
        <p className="inline-flex items-center gap-1.5 text-sm text-inverse-muted">
          {video ? (
            <Video aria-hidden="true" className="size-3.5" />
          ) : (
            <Phone aria-hidden="true" className="size-3.5" />
          )}
          {video ? c.incomingVideoTitle : c.incomingVoiceTitle}
        </p>
        <div className="flex flex-1 flex-col items-center justify-center gap-2.5 text-center">
          <span className="mb-6 rounded-full shadow-[0_0_0_16px_rgb(255_255_255/0.07),0_0_0_36px_rgb(255_255_255/0.04)]">
            <PersonAvatar
              id={ring.initiatorId}
              name={name}
              className="size-[120px] text-4xl"
            />
          </span>
          <h2 className="text-2xl leading-[1.45] font-semibold">{name}</h2>
          <p className="text-sm text-inverse-muted">
            {video ? c.incomingVideoSub : c.incomingVoiceSub}
          </p>
        </div>
        <div className="flex items-start justify-center gap-9">
          <RoundButton
            label={c.decline}
            showLabel
            className={cn(ROUND_END, "size-16")}
            onClick={() => controller.decline(ring.callId)}
          >
            <PhoneOff aria-hidden="true" />
          </RoundButton>
          {video ? (
            <RoundButton
              label={c.answerVoice}
              showLabel
              className="size-16"
              onClick={() => controller.answer(ring.callId, true)}
            >
              <Phone aria-hidden="true" />
            </RoundButton>
          ) : null}
          <RoundButton
            label={c.answer}
            showLabel
            className="size-16 bg-success text-success-foreground hover:bg-success/90"
            onClick={() => controller.answer(ring.callId)}
          >
            {video ? (
              <Video aria-hidden="true" />
            ) : (
              <Phone aria-hidden="true" />
            )}
          </RoundButton>
        </div>
      </section>
    );
  }
  return (
    <div
      data-testid="chat-call-toasts"
      className="pointer-events-none absolute start-2.5 top-4 z-20 flex flex-col gap-2 xl:start-auto xl:end-5 xl:top-5"
    >
      {incoming.map((ring) => {
        const name = nameOf(ring.initiatorId);
        const video = ring.kind === "video";
        return (
          <section
            key={ring.callId}
            role="alertdialog"
            aria-label={fillCopy(video ? c.incomingVideo : c.incomingVoice, {
              name,
            })}
            data-testid="chat-call-incoming"
            data-layout="toast"
            className="pointer-events-auto w-[min(440px,calc(100vw-1.25rem))] rounded-[16px] border border-inverse-foreground/15 bg-inverse-surface p-3.5 leading-[1.45] text-inverse-foreground shadow-lg animate-in fade-in slide-in-from-top-2 duration-200 motion-reduce:animate-none xl:w-[400px]"
          >
            <div className="flex items-center gap-3">
              <span className="rounded-full shadow-[0_0_0_6px_rgb(255_255_255/0.07),0_0_0_12px_rgb(255_255_255/0.04)]">
                <PersonAvatar
                  id={ring.initiatorId}
                  name={name}
                  className="size-12 text-base"
                />
              </span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-semibold">{name}</p>
                <p className="truncate text-xs text-inverse-muted">
                  {video ? c.incomingVideoSub : c.incomingVoiceSub}
                </p>
              </div>
            </div>
            <div className="mt-3.5 flex gap-2">
              <button
                type="button"
                className={cn(
                  TOAST_BTN,
                  "bg-destructive text-destructive-foreground hover:bg-destructive/90",
                )}
                onClick={() => controller.decline(ring.callId)}
              >
                <PhoneOff aria-hidden="true" />
                {c.decline}
              </button>
              {video ? (
                <button
                  type="button"
                  className={cn(
                    TOAST_BTN,
                    "bg-inverse-foreground/15 text-inverse-foreground hover:bg-inverse-foreground/25",
                  )}
                  onClick={() => controller.answer(ring.callId, true)}
                >
                  <Phone aria-hidden="true" />
                  {c.answerVoice}
                </button>
              ) : null}
              <button
                type="button"
                className={cn(
                  TOAST_BTN,
                  "bg-success text-success-foreground hover:bg-success/90",
                )}
                onClick={() => controller.answer(ring.callId)}
              >
                {video ? (
                  <Video aria-hidden="true" />
                ) : (
                  <Phone aria-hidden="true" />
                )}
                {c.answer}
              </button>
            </div>
          </section>
        );
      })}
    </div>
  );
}

const STRIP_BTN =
  "grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-muted text-foreground outline-none transition-colors hover:bg-accent focus-visible:ring-3 focus-visible:ring-ring/50 motion-reduce:transition-none [&_svg]:size-4";

export interface ChatCallStripProps extends ChatCallPeople {
  readonly controller: ChatCallsController;
  readonly held: ChatCurrentCall;
  /** Brings the call view back over the conversation it belongs to. */
  readonly onReturn: () => void;
  readonly copy: ChatCopy;
}

/** The connection bar: in the flow of the pane (a row under the header), so the composer and the latest message never sit under it. */
export function ChatCallStrip({
  controller,
  held,
  onReturn,
  copy,
  ...people
}: ChatCallStripProps) {
  const c = copy.calls;
  const facts = useCallFacts(held, people, copy);
  const { call, phase, name, isVideo } = facts;
  const ended = call.state === "ended";
  const ringing = phase === "outgoing-ringing";
  return (
    <section
      aria-label={c.dockLabel}
      data-testid="chat-call-strip"
      data-phase={ended ? "ended" : phase}
      className="mx-2 mt-2 mb-1 flex shrink-0 items-center gap-2 rounded-[12px] border border-border bg-card px-2.5 py-2 text-card-foreground shadow-xs animate-in fade-in duration-200 motion-reduce:animate-none"
    >
      {isVideo ? (
        <Video
          aria-hidden="true"
          className="size-4 shrink-0 text-success-text"
        />
      ) : (
        <Phone
          aria-hidden="true"
          className="size-4 shrink-0 text-success-text"
        />
      )}
      <div className="min-w-0 flex-1 leading-tight">
        <p className="truncate text-xs font-semibold text-success-text">
          {ended
            ? endedTitle(call.endReason, name, c)
            : ringing
              ? c.ringingDots
              : held.media === "lost"
                ? c.lostShort
                : held.media === "reconnecting"
                  ? c.reconnecting
                  : phase === "connecting"
                    ? c.connecting
                    : c.connected}
        </p>
        <p className="truncate text-xs text-muted-foreground">{name}</p>
      </div>
      {ended ? null : (
        <span
          role="timer"
          aria-label={c.timerLabel}
          className="text-xs text-muted-foreground tabular-nums"
        >
          {ringing ? "" : facts.timer}
        </span>
      )}
      {ended ? (
        <button
          type="button"
          aria-label={c.close}
          title={c.close}
          className={STRIP_BTN}
          onClick={controller.dismiss}
        >
          <X aria-hidden="true" />
        </button>
      ) : (
        <>
          <button
            type="button"
            aria-label={held.audioOn ? c.mute : c.unmute}
            title={held.audioOn ? c.mute : c.unmute}
            aria-pressed={!held.audioOn}
            className={cn(
              STRIP_BTN,
              held.audioOn ? null : "bg-destructive/15 text-destructive",
            )}
            onClick={controller.toggleMicrophone}
          >
            {held.audioOn ? (
              <Mic aria-hidden="true" />
            ) : (
              <MicOff aria-hidden="true" />
            )}
          </button>
          {isVideo ? (
            <button
              type="button"
              aria-label={held.videoOn ? c.cameraOff : c.cameraOn}
              title={held.videoOn ? c.cameraOff : c.cameraOn}
              aria-pressed={!held.videoOn}
              className={cn(
                STRIP_BTN,
                held.videoOn ? null : "bg-destructive/15 text-destructive",
              )}
              onClick={controller.toggleCamera}
            >
              {held.videoOn ? (
                <Video aria-hidden="true" />
              ) : (
                <VideoOff aria-hidden="true" />
              )}
            </button>
          ) : null}
          <button
            type="button"
            aria-label={c.returnToCall}
            title={c.returnToCall}
            className={STRIP_BTN}
            onClick={onReturn}
          >
            <Maximize2 aria-hidden="true" />
          </button>
          <button
            type="button"
            aria-label={ringing ? c.cancel : c.hangUp}
            title={ringing ? c.cancel : c.hangUp}
            className="grid h-8 w-11 shrink-0 place-items-center rounded-lg bg-destructive text-destructive-foreground outline-none transition-colors hover:bg-destructive/90 focus-visible:ring-3 focus-visible:ring-destructive/40 motion-reduce:transition-none [&_svg]:size-4"
            onClick={controller.hangUp}
          >
            <PhoneOff aria-hidden="true" />
          </button>
        </>
      )}
    </section>
  );
}
