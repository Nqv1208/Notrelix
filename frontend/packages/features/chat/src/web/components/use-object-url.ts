import { useEffect, useState } from "react";

/** An object URL for a Blob, revoked when the Blob changes or the component unmounts (null while there is no Blob). */
export function useObjectUrl(blob: Blob | null | undefined): string | null {
  const [url, setUrl] = useState<string | null>(null);
  useEffect(() => {
    // No object URLs in this environment (a test DOM): no preview, never a throw.
    if (!blob || typeof URL.createObjectURL !== "function") {
      setUrl(null);
      return;
    }
    const next = URL.createObjectURL(blob);
    setUrl(next);
    return () => URL.revokeObjectURL(next);
  }, [blob]);
  return url;
}
