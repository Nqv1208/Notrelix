export function LocationRedirect() {
  return () => {
    window.location.href = "/somewhere";
  };
}
