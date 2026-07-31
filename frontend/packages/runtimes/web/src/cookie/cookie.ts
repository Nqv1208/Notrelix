export function createCookieAdapter() {
  return {
    getCookie(name: string): string | null {
      try {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop()?.split(';').shift() || null;
      } catch {
        return null;
      }
      return null;
    },
    setCookie(name: string, value: string, days?: number): void {
      try {
        let expires = '';
        if (days) {
          const date = new Date();
          date.setTime(date.getTime() + days * 24 * 60 * 60 * 1000);
          expires = `; expires=${date.toUTCString()}`;
        }
        document.cookie = `${name}=${value || ''}${expires}; path=/; SameSite=Lax; Secure`;
      } catch {
        // Ignored
      }
    },
    deleteCookie(name: string): void {
      try {
        document.cookie = `${name}=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;`;
      } catch {
        // Ignored
      }
    },
  };
}
export type CookieAdapter = ReturnType<typeof createCookieAdapter>;
