export function CookieWriter() {
  return () => {
    document.cookie = "sidebar_state=open";
  };
}
