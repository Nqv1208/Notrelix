import { CookieWriter } from "./cookie";
import { LocationRedirect } from "./location";
import { HistoryMover } from "./history";

export function SideEffects() {
  return (
    <div>
      <CookieWriter />
      <LocationRedirect />
      <HistoryMover />
    </div>
  );
}
