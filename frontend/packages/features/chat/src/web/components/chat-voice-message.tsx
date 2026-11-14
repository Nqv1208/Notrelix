import {
  useEffect,
  useRef,
  useState,
  type KeyboardEvent,
  type MouseEvent,
} from "react";
import { Pause, Play, RotateCw } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import {
  barsFor,
  formatVoiceClock,
  nextVoiceSpeed,
  playbackFraction,
  seekTarget,
} from "../../core/model/voice-model";
import type { ChatAttachment } from "../../core/types/chat";
import type { ChatHooks } from "../query/create-chat-hooks";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";
import { useObjectUrl } from "./use-object-url";

/** Bars drawn for the waveform. */
const BAR_COUNT = 40;
/** Seconds moved by the arrow keys. */
const SEEK_STEP_SECONDS = 5;

/**
 * Only one voice message plays at a time (UC-CHT-I05): starting one pauses the one that was playing. The handle is the pause of the
 * playing player; it is cleared when that player pauses or ends.
 */
let activePlayback: { readonly pause: () => void } | null = null;

export interface VoiceMessagePlayerProps {
  readonly attachment: ChatAttachment;
  readonly workspaceId: string;
  readonly messageId: string;
  readonly hooks: ChatHooks;
  readonly copy: ChatCopy;
}

/**
 * A voice message in the message row (UC-CHT-I05): play and pause, a waveform that seeks on click and with the arrow keys, the elapsed
 * and total time, a speed control (1x, 1.5x, 2x) and a retry when the bytes do not load. The bytes come through the authenticated
 * attachment content hook as an object URL.
 */
export function VoiceMessagePlayer({
  attachment,
  workspaceId,
  messageId,
  hooks,
  copy,
}: VoiceMessagePlayerProps) {
  const content = hooks.useAttachmentContent(
    workspaceId,
    messageId,
    attachment.id,
  );
  const url = useObjectUrl(content.data ?? null);
  const audioRef = useRef<HTMLAudioElement>(null);
  const handle = useRef({ pause: () => audioRef.current?.pause() });
  const [playing, setPlaying] = useState(false);
  const [elapsedMs, setElapsedMs] = useState(0);
  const [loadedMs, setLoadedMs] = useState<number | null>(null);
  const [speed, setSpeed] = useState<number>(1);
  const [playFailed, setPlayFailed] = useState(false);
  const text = copy.voice;

  const totalMs = attachment.durationMs ?? loadedMs ?? 0;
  const fraction = playbackFraction(elapsedMs, totalMs);
  const bars = barsFor(attachment.waveform, BAR_COUNT);

  useEffect(() => {
    if (audioRef.current) audioRef.current.playbackRate = speed;
  }, [speed, url]);

  // A player that unmounts while it plays gives up the single active slot.
  useEffect(() => {
    const self = handle.current;
    return () => {
      if (activePlayback === self) activePlayback = null;
    };
  }, []);

  function togglePlay() {
    const audio = audioRef.current;
    if (!audio) return;
    if (!audio.paused) {
      audio.pause();
      return;
    }
    if (activePlayback && activePlayback !== handle.current)
      activePlayback.pause();
    activePlayback = handle.current;
    setPlayFailed(false);
    audio.play().catch(() => {
      if (activePlayback === handle.current) activePlayback = null;
      setPlayFailed(true);
    });
  }

  function seekTo(ms: number) {
    const audio = audioRef.current;
    const clamped = Math.min(Math.max(0, ms), totalMs || ms);
    if (audio) audio.currentTime = clamped / 1000;
    setElapsedMs(clamped);
  }

  function onSeekKey(event: KeyboardEvent<HTMLDivElement>) {
    const current = audioRef.current?.currentTime ?? elapsedMs / 1000;
    const duration = totalMs / 1000;
    let target: number | null = null;
    if (event.key === "ArrowRight")
      target = seekTarget(current, SEEK_STEP_SECONDS, duration);
    else if (event.key === "ArrowLeft")
      target = seekTarget(current, -SEEK_STEP_SECONDS, duration);
    else if (event.key === "Home") target = 0;
    else if (event.key === "End") target = duration;
    if (target === null) return;
    event.preventDefault();
    seekTo(target * 1000);
  }

  function onSeekClick(event: MouseEvent<HTMLDivElement>) {
    const rect = event.currentTarget.getBoundingClientRect();
    if (rect.width <= 0) return;
    seekTo(playbackFraction(event.clientX - rect.left, rect.width) * totalMs);
  }

  if (content.isError) {
    return (
      <div
        role="alert"
        className="flex w-72 max-w-full flex-wrap items-center gap-2 rounded-2xl border border-destructive/40 bg-card px-3 py-2 text-xs"
        data-testid="chat-voice-message"
      >
        <span className="min-w-0 flex-1 text-destructive">
          {text.loadFailed}
        </span>
        <Button
          type="button"
          size="xs"
          variant="outline"
          aria-label={text.retry}
          onClick={() => void content.refetch()}
        >
          <RotateCw aria-hidden="true" />
          {text.retry}
        </Button>
      </div>
    );
  }

  return (
    <div
      className="flex w-72 max-w-full items-center gap-2 rounded-2xl border border-border bg-card px-2 py-1.5"
      data-testid="chat-voice-message"
    >
      <Button
        type="button"
        size="icon-sm"
        variant="outline"
        className="shrink-0"
        disabled={!url}
        aria-label={playing ? text.pausePlayback : text.play}
        onClick={togglePlay}
      >
        {playing ? <Pause aria-hidden="true" /> : <Play aria-hidden="true" />}
      </Button>
      <div
        role="slider"
        tabIndex={0}
        aria-label={text.seek}
        aria-valuemin={0}
        aria-valuemax={Math.round(totalMs / 1000)}
        aria-valuenow={Math.round(elapsedMs / 1000)}
        aria-valuetext={fillCopy(text.seekValue, {
          time: formatVoiceClock(elapsedMs),
          total: formatVoiceClock(totalMs),
        })}
        onKeyDown={onSeekKey}
        onClick={onSeekClick}
        className="flex h-8 min-w-0 flex-1 cursor-pointer items-center gap-px rounded-md focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
        data-testid="chat-voice-waveform"
      >
        {bars.map((height, index) => (
          <span
            // The waveform is positional: the index is the bar.
            key={index}
            aria-hidden="true"
            className={cn(
              "w-full min-w-px flex-1 rounded-full",
              index / bars.length < fraction
                ? "bg-primary"
                : "bg-muted-foreground/40",
            )}
            style={{ height: `${Math.round(height * 100)}%` }}
          />
        ))}
      </div>
      <span
        className="shrink-0 text-xs tabular-nums text-muted-foreground"
        aria-hidden="true"
      >
        {formatVoiceClock(elapsedMs)} / {formatVoiceClock(totalMs)}
      </span>
      <Button
        type="button"
        size="xs"
        variant="ghost"
        className="shrink-0 tabular-nums"
        aria-label={fillCopy(text.speed, { speed: `${speed}x` })}
        onClick={() => setSpeed((current) => nextVoiceSpeed(current))}
      >
        {speed}x
      </Button>
      {playFailed ? (
        <span role="alert" className="sr-only">
          {text.loadFailed}
        </span>
      ) : null}
      {url ? (
        <audio
          ref={audioRef}
          src={url}
          preload="metadata"
          className="hidden"
          onPlay={() => setPlaying(true)}
          onPause={() => {
            setPlaying(false);
            if (activePlayback === handle.current) activePlayback = null;
          }}
          onEnded={() => {
            setPlaying(false);
            if (activePlayback === handle.current) activePlayback = null;
          }}
          onTimeUpdate={(event) =>
            setElapsedMs(event.currentTarget.currentTime * 1000)
          }
          onLoadedMetadata={(event) => {
            const seconds = event.currentTarget.duration;
            if (Number.isFinite(seconds) && seconds > 0)
              setLoadedMs(seconds * 1000);
          }}
        />
      ) : null}
    </div>
  );
}
