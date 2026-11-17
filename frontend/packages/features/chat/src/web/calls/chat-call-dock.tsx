/**
 * The call surface of Chat: incoming rings, the call this person holds (ringing out, connecting, connected, ended) and the
 * failures that stop a call from starting. One dock, bottom right (full width on a phone); a card grows in place from ringing to
 * connected instead of swapping, and no control carries a shortcut hint.
 */
import { useEffect, useState } from "react";
import { Mic, MicOff, Phone, PhoneOff, Video, VideoOff, X } from "lucide-react";
import { Avatar, AvatarFallback, Button, cn } from "@notrelix/ui-web";
import {
  callPhaseOf,
  counterpartOf,
  elapsedSeconds,
  formatCallTimer,
} from "../../core/model/call-model";
import type { ChatCopy } from "../components/chat.copy";
import { avatarToneOf, initialsOf } from "../components/chat-initials";
import { fillCopy } from "../components/use-chat-copy";
import type {
  ChatCallError,
  ChatCallsController,
  ChatCurrentCall,
} from "./use-chat-calls";

export interface ChatCallDockProps {
  readonly controller: ChatCallsController;
  readonly viewerId: string;
  readonly nameOf: (userId: string) => string;
  /** The other person of a direct conversation, for a call the viewer started (nobody else has joined yet). */
  readonly peerOf?: (spaceId: string) => string | null;
  readonly copy: ChatCopy;
}

function PersonAvatar({
  id,
  name,
  size,
}: {
  id: string;
  name: string;
  size: "md" | "lg";
}) {
  return (
    <Avatar
      className={size === "lg" ? "size-16" : "size-10"}
      aria-hidden="true"
    >
      <AvatarFallback
        className={cn(
          avatarToneOf(id),
          size === "lg" ? "text-lg" : "text-sm",
          "font-medium",
        )}
      >
        {initialsOf(name)}
      </AvatarFallback>
    </Avatar>
  );
}

const CARD =
  "pointer-events-auto flex w-full flex-col gap-3 overflow-hidden rounded-xl border border-border bg-card p-4 text-card-foreground shadow-lg animate-in fade-in slide-in-from-bottom-2 duration-200 motion-reduce:animate-none";

const ERROR_COPY: Record<ChatCallError, keyof ChatCopy["calls"]> = {
  unavailable: "unavailable",
  "start-failed": "startFailed",
  "you-busy": "youBusy",
  "already-active": "alreadyActive",
  full: "full",
};

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

function EndedLine({
  held,
  name,
  copy,
}: {
  held: ChatCurrentCall;
  name: string;
  copy: ChatCopy;
}) {
  const c = copy.calls;
  const reason = held.call.endReason;
  const text =
    reason === "declined"
      ? fillCopy(c.endedDeclined, { name })
      : reason === "missed"
        ? fillCopy(c.endedMissed, { name })
        : reason === "busy"
          ? fillCopy(c.endedBusy, { name })
          : reason === "cancelled"
            ? c.endedCancelled
            : reason === "failed"
              ? c.endedFailed
              : c.endedCompleted;
  const lasted =
    reason === "completed" && held.call.answeredAt && held.call.endedAt
      ? formatCallTimer(
          (Date.parse(held.call.endedAt) - Date.parse(held.call.answeredAt)) /
            1000,
        )
      : null;
  return (
    <div className="min-w-0 flex-1">
      <p className="truncate text-sm font-medium">{text}</p>
      {lasted ? (
        <p className="text-xs text-muted-foreground tabular-nums">
          {fillCopy(c.endedLasted, { duration: lasted })}
        </p>
      ) : null}
    </div>
  );
}

function CurrentCall({
  controller,
  held,
  viewerId,
  nameOf,
  peerOf,
  copy,
}: {
  controller: ChatCallsController;
  held: ChatCurrentCall;
  viewerId: string;
  nameOf: (id: string) => string;
  peerOf?: (spaceId: string) => string | null;
  copy: ChatCopy;
}) {
  const c = copy.calls;
  const { call } = held;
  const peer = peerOf?.(call.spaceId) ?? null;
  const otherId =
    counterpartOf(call, viewerId, peer ? [peer] : []) ?? call.initiatorId;
  const name = nameOf(otherId);
  const connected = held.media === "connected";
  const phase = callPhaseOf(call, viewerId, connected);
  const now = useNow(phase === "connected");
  const isVideo = call.kind === "video";
  const other = call.participants.find((p) => p.personId === otherId);
  const remoteVideoOn = Boolean(other?.videoOn && !other.leftAt);
  const showStage =
    isVideo && (phase === "connecting" || phase === "connected");
  const status =
    phase === "outgoing-ringing"
      ? c.ringing
      : held.media === "reconnecting"
        ? c.reconnecting
        : phase === "connected"
          ? formatCallTimer(elapsedSeconds(call.answeredAt, now))
          : phase === "connecting"
            ? c.connecting
            : "";
  const title =
    phase === "outgoing-ringing"
      ? fillCopy(isVideo ? c.outgoingVideo : c.outgoingVoice, { name })
      : name;
  const failed = held.media === "failed" && call.state !== "ended";

  return (
    <section
      className={CARD}
      aria-label={c.dockLabel}
      data-testid="chat-call-card"
      data-phase={phase}
    >
      {showStage ? (
        <div className="relative aspect-video w-full overflow-hidden rounded-lg bg-muted">
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
              className="absolute inset-0 flex flex-col items-center justify-center gap-2"
              data-testid="chat-call-remote-tile"
            >
              <PersonAvatar id={otherId} name={name} size="lg" />
              <p className="text-xs text-muted-foreground">
                {fillCopy(c.cameraOffTile, { name })}
              </p>
            </div>
          )}
          <video
            ref={controller.localVideoRef}
            autoPlay
            muted
            playsInline
            aria-label={c.localVideo}
            className={cn(
              "absolute end-2 bottom-2 h-16 w-24 rounded-md border border-border bg-card object-cover transition-opacity duration-200 motion-reduce:transition-none",
              held.videoOn ? "opacity-100" : "opacity-0",
            )}
          />
        </div>
      ) : null}

      <div className="flex items-center gap-3">
        {showStage ? null : <PersonAvatar id={otherId} name={name} size="md" />}
        {call.state === "ended" ? (
          <EndedLine held={held} name={name} copy={copy} />
        ) : (
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium">{title}</p>
            <p
              className="text-xs text-muted-foreground tabular-nums"
              role="timer"
              aria-label={c.timerLabel}
            >
              {status}
            </p>
          </div>
        )}
        {call.state === "ended" ? (
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={c.close}
            title={c.close}
            onClick={controller.dismiss}
          >
            <X aria-hidden="true" />
          </Button>
        ) : null}
      </div>

      {failed ? (
        <div
          role="alert"
          className="flex items-center justify-between gap-2 text-xs text-destructive"
        >
          <span>{c.failedConnect}</span>
          <Button variant="outline" size="xs" onClick={controller.retryConnect}>
            {c.retry}
          </Button>
        </div>
      ) : null}
      {held.deviceNotice ? (
        <p role="status" className="text-xs text-muted-foreground">
          {held.deviceNotice === "mic-denied" ? c.micDenied : c.cameraDenied}
        </p>
      ) : null}

      {call.state === "ended" ? null : (
        <div className="flex items-center justify-center gap-2">
          {phase === "outgoing-ringing" ? null : (
            <Button
              variant="secondary"
              size="icon"
              aria-label={held.audioOn ? c.mute : c.unmute}
              title={held.audioOn ? c.mute : c.unmute}
              aria-pressed={!held.audioOn}
              disabled={phase !== "connected"}
              onClick={controller.toggleMicrophone}
            >
              {held.audioOn ? (
                <Mic aria-hidden="true" />
              ) : (
                <MicOff aria-hidden="true" />
              )}
            </Button>
          )}
          {isVideo && phase !== "outgoing-ringing" ? (
            <Button
              variant="secondary"
              size="icon"
              aria-label={held.videoOn ? c.cameraOff : c.cameraOn}
              title={held.videoOn ? c.cameraOff : c.cameraOn}
              aria-pressed={!held.videoOn}
              disabled={phase !== "connected"}
              onClick={controller.toggleCamera}
            >
              {held.videoOn ? (
                <Video aria-hidden="true" />
              ) : (
                <VideoOff aria-hidden="true" />
              )}
            </Button>
          ) : null}
          <Button
            variant="destructive"
            size={phase === "outgoing-ringing" ? "default" : "icon"}
            aria-label={phase === "outgoing-ringing" ? c.cancel : c.hangUp}
            title={phase === "outgoing-ringing" ? c.cancel : c.hangUp}
            onClick={controller.hangUp}
          >
            <PhoneOff aria-hidden="true" />
            {phase === "outgoing-ringing" ? c.cancel : null}
          </Button>
        </div>
      )}
    </section>
  );
}

export function ChatCallDock({
  controller,
  viewerId,
  nameOf,
  peerOf,
  copy,
}: ChatCallDockProps) {
  const c = copy.calls;
  const { current, incoming, error } = controller;
  const nothing = !current && incoming.length === 0 && !error;
  if (nothing) return null;
  return (
    <div
      className="pointer-events-none fixed inset-x-4 bottom-4 z-toast flex flex-col gap-2 md:inset-x-auto md:end-4 md:w-[360px]"
      data-testid="chat-call-dock"
    >
      {incoming.map((ring) => {
        const name = nameOf(ring.initiatorId);
        return (
          <section
            key={ring.callId}
            role="alertdialog"
            aria-label={fillCopy(
              ring.kind === "video" ? c.incomingVideo : c.incomingVoice,
              { name },
            )}
            className={CARD}
            data-testid="chat-call-incoming"
          >
            <div className="flex items-center gap-3">
              <PersonAvatar id={ring.initiatorId} name={name} size="md" />
              <p className="min-w-0 flex-1 text-sm font-medium">
                {fillCopy(
                  ring.kind === "video" ? c.incomingVideo : c.incomingVoice,
                  { name },
                )}
              </p>
            </div>
            <div className="flex gap-2">
              <Button
                className="flex-1"
                onClick={() => controller.answer(ring.callId)}
              >
                {ring.kind === "video" ? (
                  <Video aria-hidden="true" />
                ) : (
                  <Phone aria-hidden="true" />
                )}
                {c.answer}
              </Button>
              <Button
                className="flex-1"
                variant="destructive"
                onClick={() => controller.decline(ring.callId)}
              >
                <PhoneOff aria-hidden="true" />
                {c.decline}
              </Button>
            </div>
          </section>
        );
      })}
      {current ? (
        <CurrentCall
          controller={controller}
          held={current}
          viewerId={viewerId}
          nameOf={nameOf}
          peerOf={peerOf}
          copy={copy}
        />
      ) : null}
      {error ? (
        <div role="alert" className={cn(CARD, "flex-row items-center")}>
          <p className="min-w-0 flex-1 text-sm">{c[ERROR_COPY[error]]}</p>
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={c.close}
            title={c.close}
            onClick={controller.dismiss}
          >
            <X aria-hidden="true" />
          </Button>
        </div>
      ) : null}
    </div>
  );
}
