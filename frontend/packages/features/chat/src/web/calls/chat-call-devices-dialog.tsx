/**
 * The call settings dialog of the call boards (Chat-Call-Devices and its variants): the microphone, the speaker and, in a
 * video call, the camera. Every state grows in place: the lists while the browser answers, the empty "no device" row with
 * its check-again button, the browser's refusal with the steps to allow it. Choosing a device moves the call at once.
 */
import { useState } from "react";
import { AlertTriangleIcon, InfoIcon, RefreshCwIcon } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Skeleton,
  Tabs,
  TabsList,
  TabsTrigger,
  TabsContent,
} from "@notrelix/ui-web";
import type { ChatCopy } from "../components/chat.copy";
import {
  CHAT_CALL_DEFAULT_DEVICE,
  supportsSpeakerChoice,
  type ChatCallDevicesState,
} from "./chat-call-devices";
import type { ChatCallDeviceKind } from "./chat-call-media";
import type { ChatCallsController, ChatCurrentCall } from "./use-chat-calls";

export interface ChatCallDevicesDialogProps {
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly controller: ChatCallsController;
  readonly held: ChatCurrentCall;
  readonly devices: ChatCallDevicesState;
  /** Called after a choice the call took, so a leftover "unplugged" notice goes away. */
  readonly onChosen: () => void;
  readonly copy: ChatCopy;
}

export function ChatCallDevicesDialog({
  open,
  onOpenChange,
  controller,
  held,
  devices,
  onChosen,
  copy,
}: ChatCallDevicesDialogProps) {
  const c = copy.calls;
  const isVideo = held.call.kind === "video";
  const [failed, setFailed] = useState(false);
  const { status, lists } = devices;
  const denied = status === "denied";
  const loading = status === "loading";

  const choose = async (kind: ChatCallDeviceKind, value: string) => {
    const failure = await controller.switchDevice(
      kind,
      value === CHAT_CALL_DEFAULT_DEVICE ? null : value,
    );
    setFailed(failure !== null);
    if (!failure) onChosen();
  };

  // A plain function, not a component: a component declared here would remount (and close its list) on every render.
  const field = (
    kind: ChatCallDeviceKind,
    label: string,
    /** The browser refused this kind of device: the field shows it and cannot be opened. */
    blocked: boolean,
  ) => {
    const id = `chat-call-device-${kind}`;
    const list = lists[kind];
    const inUse = held.devices[kind];
    const value =
      inUse && list.some((d) => d.id === inUse)
        ? inUse
        : CHAT_CALL_DEFAULT_DEVICE;
    const empty = !loading && !blocked && list.length === 0;
    const bad = blocked || (empty && kind !== "audiooutput");
    return (
      <div key={kind} className="flex min-w-0 flex-col gap-1.5">
        <label
          htmlFor={id}
          className="text-sm font-semibold text-muted-foreground"
        >
          {label}
        </label>
        {loading ? (
          <Skeleton className="h-9 w-full rounded-lg" />
        ) : (
          <Select
            value={bad ? "" : value}
            onValueChange={(next) => void choose(kind, next)}
            disabled={bad}
          >
            <SelectTrigger
              id={id}
              className="w-full"
              aria-invalid={bad || undefined}
            >
              <SelectValue
                placeholder={blocked ? c.deviceNotAllowed : c.deviceNone}
              />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={CHAT_CALL_DEFAULT_DEVICE}>
                {c.deviceDefault}
              </SelectItem>
              {list.map((d, index) => (
                <SelectItem key={d.id} value={d.id}>
                  {d.label || `${label} ${index + 1}`}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      </div>
    );
  };

  const noMic = !loading && !denied && lists.audioinput.length === 0;
  const noCamera =
    isVideo && !loading && !denied && lists.videoinput.length === 0;
  const voiceTab = (
    <div className="flex flex-col gap-4">
      {noMic ? (
        <p
          role="status"
          className="flex items-center gap-2.5 rounded-[10px] bg-warning-subtle px-4 py-2 text-sm font-semibold text-warning-subtle-foreground"
        >
          <InfoIcon aria-hidden="true" className="size-4 shrink-0" />
          {c.noMicrophone}
        </p>
      ) : null}
      <div className="grid gap-4 sm:grid-cols-2">
        {field("audioinput", c.microphone, denied)}
        {supportsSpeakerChoice()
          ? field("audiooutput", c.speaker, false)
          : null}
      </div>
    </div>
  );
  const videoTab = (
    <div className="flex flex-col gap-4">
      {noCamera ? (
        <p
          role="status"
          className="flex items-center gap-2.5 rounded-[10px] bg-warning-subtle px-4 py-2 text-sm font-semibold text-warning-subtle-foreground"
        >
          <InfoIcon aria-hidden="true" className="size-4 shrink-0" />
          {c.noCamera}
        </p>
      ) : null}
      {field("videoinput", c.camera, denied)}
    </div>
  );

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-xl"
        data-testid="chat-call-devices"
        // The choice is applied on the spot, so the first field is not focused (it would open its list).
        onOpenAutoFocus={(event) => event.preventDefault()}
      >
        <DialogHeader className="pr-8">
          <DialogTitle>{c.devicesTitle}</DialogTitle>
          <DialogDescription className="sr-only">
            {c.devicesOpen}
          </DialogDescription>
        </DialogHeader>

        {denied ? (
          <div
            role="alert"
            className="rounded-xl border border-destructive p-3.5"
          >
            <b className="flex items-center gap-2 text-destructive">
              <AlertTriangleIcon aria-hidden="true" className="size-4" />
              {c.blockedTitle}
            </b>
            <ol className="mt-2.5 list-decimal ps-5 leading-7 text-muted-foreground">
              <li>{c.blockedStep1}</li>
              <li>{c.blockedStep2}</li>
              <li>{c.blockedStep3}</li>
            </ol>
            <Button className="mt-3" onClick={() => window.location.reload()}>
              <RefreshCwIcon aria-hidden="true" />
              {c.reloadPage}
            </Button>
          </div>
        ) : null}

        {loading ? (
          <p role="status" className="text-sm text-muted-foreground">
            {c.devicesLoading}
          </p>
        ) : null}

        {isVideo ? (
          <Tabs defaultValue="voice">
            <TabsList>
              <TabsTrigger value="voice">{c.devicesVoiceTab}</TabsTrigger>
              <TabsTrigger value="video">{c.devicesVideoTab}</TabsTrigger>
            </TabsList>
            <TabsContent value="voice" className="pt-3">
              {voiceTab}
            </TabsContent>
            <TabsContent value="video" className="pt-3">
              {videoTab}
            </TabsContent>
          </Tabs>
        ) : (
          voiceTab
        )}

        {failed ? (
          <p role="alert" className="text-sm text-destructive">
            {c.deviceSwitchFailed}
          </p>
        ) : null}

        <DialogFooter>
          {noMic || noCamera ? (
            <Button variant="outline" onClick={devices.refresh}>
              <RefreshCwIcon aria-hidden="true" />
              {c.recheck}
            </Button>
          ) : null}
          <Button onClick={() => onOpenChange(false)}>{c.devicesDone}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
