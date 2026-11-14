import { useEffect, useReducer, useRef } from "react";
import { Mic, Pause, Play, Send, Square, Trash2 } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import {
  ChatVoiceRecorderError,
  type ChatVoiceRecorder,
  type VoiceRecorderFailure,
  type VoiceRecording,
} from "../../core/api/chat-voice-recorder";
import {
  VOICE_LEVEL_BARS,
  VOICE_MAX_MS,
  formatVoiceClock,
  recordingFile,
  reduceVoiceRecorder,
  type VoiceRecorderState,
} from "../../core/model/voice-model";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";
import { useObjectUrl } from "./use-object-url";

const IDLE: VoiceRecorderState = { status: "idle" };

export interface VoiceRecorderPanelProps {
  readonly recorder: ChatVoiceRecorder;
  readonly copy: ChatCopy;
  /** The recording is sent as this File (its voice facts travel with it). */
  readonly onSend: (file: File) => void;
  /** The panel closes: after a send, a discard or a cancel. */
  readonly onClose: () => void;
  /** The Workspace voice limit in milliseconds; recording stops by itself at it. */
  readonly maxMs?: number;
}

/**
 * Records one voice message in place of the composer row (UC-CHT-I04): permission, recording with elapsed time and live level bars,
 * optional pause, then review with play, discard and send. Denied, unsupported and failed states explain what to do. The recorder is
 * the injected port, so a test drives every state with a fake.
 */
export function VoiceRecorderPanel({
  recorder,
  copy,
  onSend,
  onClose,
  maxMs = VOICE_MAX_MS,
}: VoiceRecorderPanelProps) {
  const [state, dispatch] = useReducer(reduceVoiceRecorder, IDLE);
  const mounted = useRef(true);
  const text = copy.voice;

  async function begin() {
    dispatch({ type: "start" });
    if (!recorder.isSupported()) {
      dispatch({ type: "refused", reason: "unsupported" });
      return;
    }
    try {
      await recorder.start(
        {
          onLevel: (level) => {
            if (mounted.current) dispatch({ type: "level", level });
          },
          onElapsed: (elapsedMs) => {
            if (mounted.current) dispatch({ type: "elapsed", elapsedMs });
          },
        },
        maxMs,
      );
      if (mounted.current) dispatch({ type: "granted" });
    } catch (error) {
      if (!mounted.current) return;
      const reason: VoiceRecorderFailure =
        error instanceof ChatVoiceRecorderError ? error.reason : "failed";
      dispatch({ type: "refused", reason });
    }
  }

  useEffect(() => {
    mounted.current = true;
    void begin();
    return () => {
      mounted.current = false;
      recorder.cancel();
    };
    // The panel records once per mount; a retry remounts it through the composer.
  }, []);

  // The Workspace limit ends the recording: it stops at the limit and the message is reviewed (UC-CHT-I04).
  const atLimit = state.status === "recording" && state.elapsedMs >= maxMs;
  useEffect(() => {
    if (atLimit) void stop();
  }, [atLimit]);

  async function stop() {
    try {
      const recording: VoiceRecording = await recorder.stop();
      if (!mounted.current) return;
      dispatch({ type: "stopped", recording });
    } catch {
      if (mounted.current) dispatch({ type: "failed" });
    }
  }

  function discard() {
    recorder.cancel();
    onClose();
  }

  function send(recording: VoiceRecording) {
    onSend(recordingFile(recording));
    onClose();
  }

  const region = (
    <section
      aria-label={text.regionLabel}
      className="flex min-w-0 flex-1 flex-wrap items-center gap-2 px-2 py-1.5"
      data-testid="chat-voice-recorder"
      data-status={state.status}
    >
      {renderBody(state)}
    </section>
  );

  function renderBody(current: VoiceRecorderState) {
    switch (current.status) {
      case "idle":
      case "requesting":
        return (
          <p role="status" className="text-sm text-muted-foreground">
            {text.requesting}
          </p>
        );
      case "recording":
        return (
          <>
            <span
              aria-hidden="true"
              className={cn(
                "size-2.5 shrink-0 rounded-full bg-destructive motion-reduce:animate-none",
                !current.paused && "animate-pulse",
              )}
            />
            <span role="status" className="text-sm tabular-nums">
              {text.recording} {formatVoiceClock(current.elapsedMs)}{" "}
              <span className="text-muted-foreground">
                {fillCopy(text.elapsed, {
                  time: formatVoiceClock(current.elapsedMs),
                  max: formatVoiceClock(maxMs),
                })}
              </span>
            </span>
            <LevelBars levels={current.levels} label={text.level} />
            <Button
              type="button"
              size="icon-sm"
              variant="ghost"
              aria-label={current.paused ? text.resume : text.pause}
              onClick={() => {
                if (current.paused) {
                  recorder.resume();
                  dispatch({ type: "resume" });
                } else {
                  recorder.pause();
                  dispatch({ type: "pause" });
                }
              }}
            >
              {current.paused ? (
                <Play aria-hidden="true" />
              ) : (
                <Pause aria-hidden="true" />
              )}
            </Button>
            <Button
              type="button"
              size="icon-sm"
              aria-label={text.stop}
              onClick={() => void stop()}
            >
              <Square aria-hidden="true" />
            </Button>
            <Button
              type="button"
              size="icon-sm"
              variant="ghost"
              aria-label={text.cancel}
              onClick={discard}
            >
              <Trash2 aria-hidden="true" />
            </Button>
          </>
        );
      case "review":
        return (
          <Review
            recording={current.recording}
            copy={copy}
            onSend={() => send(current.recording)}
            onDiscard={discard}
          />
        );
      case "too-short":
        return (
          <Notice
            text={text.tooShort}
            onRetry={() => void begin()}
            retry={text.tryAgain}
            onDismiss={onClose}
          />
        );
      case "denied":
        return (
          <Notice
            text={text.denied}
            onRetry={() => void begin()}
            retry={text.tryAgain}
          />
        );
      case "unsupported":
        return (
          <Notice text={text.unsupported} onDismiss={onClose} retry={null} />
        );
      case "failed":
        return (
          <Notice
            text={text.failed}
            onRetry={() => void begin()}
            retry={text.tryAgain}
          />
        );
    }
  }

  return (
    <>
      <Mic
        className="ml-2 size-4 shrink-0 text-muted-foreground"
        aria-hidden="true"
      />
      {region}
    </>
  );
}

function LevelBars({
  levels,
  label,
}: {
  readonly levels: readonly number[];
  readonly label: string;
}) {
  const bars = Array.from({ length: VOICE_LEVEL_BARS }, (_, index) => {
    const offset = levels.length - VOICE_LEVEL_BARS + index;
    return offset >= 0 ? (levels[offset] ?? 0) : 0;
  });
  return (
    <div
      role="img"
      aria-label={label}
      className="flex h-6 min-w-0 flex-1 items-end gap-0.5"
    >
      {bars.map((level, index) => (
        <span
          // The meter is positional: the index is the bar.
          key={index}
          aria-hidden="true"
          className="w-1 rounded-full bg-primary"
          style={{ height: `${Math.max(8, Math.round(level * 100))}%` }}
        />
      ))}
    </div>
  );
}

function Review({
  recording,
  copy,
  onSend,
  onDiscard,
}: {
  readonly recording: VoiceRecording;
  readonly copy: ChatCopy;
  readonly onSend: () => void;
  readonly onDiscard: () => void;
}) {
  const text = copy.voice;
  const url = useObjectUrl(recording.blob);
  const audioRef = useRef<HTMLAudioElement>(null);
  return (
    <>
      <span className="sr-only">{text.reviewLabel}</span>
      {url ? (
        <audio ref={audioRef} src={url} preload="auto" className="hidden" />
      ) : null}
      <Button
        type="button"
        size="icon-sm"
        variant="outline"
        aria-label={text.play}
        onClick={() => void audioRef.current?.play()}
      >
        <Play aria-hidden="true" />
      </Button>
      <span className="text-sm tabular-nums">
        {formatVoiceClock(recording.durationMs)}
      </span>
      <LevelBars
        levels={recording.waveform.map((peak) => peak / 255)}
        label={text.reviewLabel}
      />
      <Button type="button" size="xs" variant="ghost" onClick={onDiscard}>
        <Trash2 aria-hidden="true" />
        {text.discard}
      </Button>
      <Button
        type="button"
        size="icon-sm"
        aria-label={text.send}
        title={text.send}
        onClick={onSend}
      >
        <Send aria-hidden="true" />
      </Button>
    </>
  );
}

function Notice({
  text,
  retry,
  onRetry,
  onDismiss,
}: {
  readonly text: string;
  readonly retry: string | null;
  readonly onRetry?: () => void;
  readonly onDismiss?: () => void;
}) {
  return (
    <>
      <p role="alert" className="min-w-0 flex-1 text-sm text-destructive">
        {text}
      </p>
      {retry && onRetry ? (
        <Button type="button" size="xs" variant="outline" onClick={onRetry}>
          {retry}
        </Button>
      ) : null}
      {onDismiss ? (
        <Button type="button" size="xs" variant="ghost" onClick={onDismiss}>
          <Trash2 aria-hidden="true" />
        </Button>
      ) : null}
    </>
  );
}
