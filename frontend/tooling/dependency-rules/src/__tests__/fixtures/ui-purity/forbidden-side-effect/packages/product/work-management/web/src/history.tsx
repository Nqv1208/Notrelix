export function HistoryMover() {
  return () => {
    history.pushState({}, "");
  };
}
