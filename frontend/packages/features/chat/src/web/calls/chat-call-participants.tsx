/**
 * The people of a group or channel call, drawn on the call room boards (Chat-Call-Room-Grid4, Grid9, Grid16, Speaker and
 * Chat-Call-Panel-People): a tile per person with who is speaking, the microphone and the camera, and a list of participants beside the grid.
 * The server holds at most 25 people in a voice call and 12 in a video call, so the grid never draws more.
 */
import { MicOff, Video, VideoOff } from "lucide-react";
import { Avatar, AvatarFallback, cn } from "@notrelix/ui-web";
import { CHAT_CALL_CAP, joinedParticipants } from "../../core/model/call-model";
import type { ChatCallParticipant } from "../../core/types/call";
import type { ChatCopy } from "../components/chat.copy";
import { avatarToneOf, initialsOf } from "../components/chat-initials";
import { fillCopy } from "../components/use-chat-copy";
import type { ChatCallsController, ChatCurrentCall } from "./use-chat-calls";

export interface ChatCallParticipantsProps {
  readonly controller: ChatCallsController;
  readonly held: ChatCurrentCall;
  readonly viewerId: string;
  readonly nameOf: (userId: string) => string;
  readonly copy: ChatCopy;
}

/** Columns by head count: two on a phone, up to five on a wide screen. */
function columnsOf(count: number): string {
  if (count <= 1) return "grid-cols-1";
  if (count <= 4) return "grid-cols-2";
  if (count <= 9) return "grid-cols-2 sm:grid-cols-3";
  if (count <= 16) return "grid-cols-2 sm:grid-cols-3 lg:grid-cols-4";
  return "grid-cols-3 sm:grid-cols-4 lg:grid-cols-5";
}

/** What the viewer's own microphone and camera say, which is ahead of what the server last heard. */
function stateOf(
  p: ChatCallParticipant,
  { held, viewerId }: Pick<ChatCallParticipantsProps, "held" | "viewerId">,
) {
  const own = p.personId === viewerId;
  return {
    own,
    audioOn: own ? held.audioOn : p.audioOn,
    videoOn: own ? held.videoOn : p.videoOn,
  };
}

export function ChatCallParticipantGrid(props: ChatCallParticipantsProps) {
  const { controller, held, nameOf, copy } = props;
  const c = copy.calls;
  const people = joinedParticipants(held.call);
  const isVideo = held.call.kind === "video";
  return (
    <ul
      aria-label={c.groupParticipants}
      data-testid="chat-call-grid"
      data-count={people.length}
      className={cn(
        "grid min-h-0 flex-1 auto-rows-fr gap-2 overflow-y-auto p-3",
        columnsOf(people.length),
      )}
    >
      {people.map((p) => {
        const { own, audioOn, videoOn } = stateOf(p, props);
        const name = own ? c.groupYou : nameOf(p.personId);
        const speaking = controller.speaking.has(p.personId) && audioOn;
        return (
          <li
            key={p.personId}
            data-testid="chat-call-tile"
            data-person={p.personId}
            data-speaking={speaking ? "true" : "false"}
            data-audio={audioOn ? "on" : "off"}
            data-video={videoOn ? "on" : "off"}
            aria-label={[
              name,
              speaking ? c.groupSpeaking : null,
              audioOn ? null : c.groupMuted,
            ]
              .filter(Boolean)
              .join(", ")}
            className={cn(
              "relative grid min-h-24 place-items-center overflow-hidden rounded-xl bg-inverse-foreground/10 transition-shadow motion-reduce:transition-none",
              speaking && "ring-3 ring-success",
            )}
          >
            {isVideo ? (
              <video
                ref={
                  own
                    ? controller.localVideoRef
                    : controller.participantVideoRef(p.personId)
                }
                autoPlay
                playsInline
                muted={own}
                aria-label={fillCopy(own ? c.localVideo : c.remoteVideo, {
                  name,
                })}
                className={cn(
                  "absolute inset-0 size-full object-cover transition-opacity duration-200 motion-reduce:transition-none",
                  videoOn ? "opacity-100" : "opacity-0",
                )}
              />
            ) : null}
            {videoOn ? null : (
              <Avatar className="size-14 text-xl" aria-hidden="true">
                <AvatarFallback
                  className={cn(avatarToneOf(p.personId), "font-semibold")}
                >
                  {initialsOf(nameOf(p.personId))}
                </AvatarFallback>
              </Avatar>
            )}
            <span className="absolute start-2 bottom-2 inline-flex max-w-[calc(100%-1rem)] items-center gap-1 rounded-lg bg-black/70 px-2 py-[3px] text-xs text-white">
              {audioOn ? null : (
                <MicOff aria-hidden="true" className="size-3 shrink-0" />
              )}
              <span className="truncate">{name}</span>
            </span>
          </li>
        );
      })}
    </ul>
  );
}

export function ChatCallPeoplePanel(props: ChatCallParticipantsProps) {
  const { controller, held, nameOf, copy } = props;
  const c = copy.calls;
  const { call } = held;
  const people = joinedParticipants(call);
  const isVideo = call.kind === "video";
  return (
    <section
      aria-label={c.groupParticipants}
      data-testid="chat-call-people"
      className="flex max-h-64 w-full shrink-0 flex-col border-t border-inverse-foreground/10 md:max-h-none md:w-72 md:border-s md:border-t-0"
    >
      <h3 className="px-4 pt-3 pb-1 text-sm font-semibold">
        {c.groupParticipants} ·{" "}
        {fillCopy(c.groupPeople, {
          count: String(people.length),
          cap: String(CHAT_CALL_CAP[call.kind]),
        })}
      </h3>
      <ul className="min-h-0 flex-1 overflow-y-auto px-2 pb-2">
        {people.map((p) => {
          const { own, audioOn, videoOn } = stateOf(p, props);
          const speaking = controller.speaking.has(p.personId) && audioOn;
          return (
            <li
              key={p.personId}
              data-testid="chat-call-person"
              data-person={p.personId}
              data-speaking={speaking ? "true" : "false"}
              className="flex items-center gap-2.5 rounded-lg px-2 py-1.5"
            >
              <Avatar
                className={cn(
                  "size-8 text-xs",
                  speaking && "ring-2 ring-success",
                )}
                aria-hidden="true"
              >
                <AvatarFallback
                  className={cn(avatarToneOf(p.personId), "font-semibold")}
                >
                  {initialsOf(nameOf(p.personId))}
                </AvatarFallback>
              </Avatar>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm">
                  {own ? c.groupYou : nameOf(p.personId)}
                </span>
                {p.personId === call.initiatorId ? (
                  <span className="block truncate text-xs text-inverse-muted">
                    {c.groupStarter}
                  </span>
                ) : null}
              </span>
              <span className="flex shrink-0 items-center gap-1.5 text-inverse-muted">
                {audioOn ? null : (
                  <MicOff
                    role="img"
                    aria-label={c.groupMuted}
                    className="size-4"
                  />
                )}
                {isVideo ? (
                  videoOn ? (
                    <Video
                      role="img"
                      aria-label={c.groupCameraOn}
                      className="size-4"
                    />
                  ) : (
                    <VideoOff
                      role="img"
                      aria-label={c.groupCameraOff}
                      className="size-4"
                    />
                  )
                ) : null}
              </span>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
