
const ACCESS_TOKEN_KEY = "access_token"
const REFRESH_TOKEN_KEY = "refresh_token"
const TOKEN_CHANGED_EVENT = "auth:token-changed"

export const tokenStorage = {
    setTokens(accessToken: string, refreshToken: string) {
        if (typeof window === "undefined") return
        localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)
        localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
        window.dispatchEvent(new Event(TOKEN_CHANGED_EVENT))
    },

    getAccessToken() {
        if (typeof window === "undefined") return null
        return localStorage.getItem(ACCESS_TOKEN_KEY)
    },

    getRefreshToken() {
        if (typeof window === "undefined") return null
        return localStorage.getItem(REFRESH_TOKEN_KEY)
    },

    clearTokens() {
        if (typeof window === "undefined") return
        localStorage.removeItem(ACCESS_TOKEN_KEY)
        localStorage.removeItem(REFRESH_TOKEN_KEY)
        window.dispatchEvent(new Event(TOKEN_CHANGED_EVENT))
    },

    onTokenChanged(callback: () => void) {
        if (typeof window === "undefined") return () => {}

        const handler = () => callback()
        window.addEventListener(TOKEN_CHANGED_EVENT, handler)
        return () => window.removeEventListener(TOKEN_CHANGED_EVENT, handler)
    }
}