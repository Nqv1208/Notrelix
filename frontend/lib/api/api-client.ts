import { tokenStorage} from "@/lib/auth/token-storage"
import { da, th } from "date-fns/locale"
import { json } from "stream/consumers"

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5064/api"

export async function apiFetch<T>(
   url: string,
   options: RequestInit = {},
   retry: Boolean = true
): Promise<T> {
   const accessToken = tokenStorage.getAccessToken()

   const headers: HeadersInit = {
      "Content-Type": "application/json",
      ...(options.headers as Record<string, string>)
   }

   if (accessToken) {
      headers["Authorization"] = `Bearer ${accessToken}`
   }

   const response = await fetch(BASE_URL + url, {
      ...options,
      headers
   })

   if (response.status === 401 && retry) {
      return handleRefreshToken<T>(url, options)
   }

   return response.json()
}

async function handleRefreshToken<T>(
   url: string,
   options: RequestInit
): Promise<T> {
   const refreshToken = tokenStorage.getRefreshToken()

   if (!refreshToken) {
      tokenStorage.clearTokens()
      window.location.href = "/login"
      throw new Error("Unauthorized")
   }

   const res = await fetch(
      BASE_URL + "api/auth/refresh-token",
      {
         method: "POST",
         headers: {
            "Content-Type": "application/json"
         },
         body: JSON.stringify(refreshToken)
      }
   )

   if (!res.ok) {
      tokenStorage.clearTokens()
      window.location.href = "/login"
      throw new Error("Refresh token failed")
   }

   const data = await res.json()

   tokenStorage.setTokens(
      data.accessToken,
      data.refreshToken
   )

   return apiFetch<T>(url, options, false)
}

export const api = {
   get<T>(url: string) {
      return apiFetch<T>(url)
   },

   post<T>(url: string, data?: any) {
      return apiFetch<T>(url, {
         method: "POST",
         body: JSON.stringify(data)
      })
   },
   
   put<T>(url: string, data?: any) {
      return apiFetch<T>(url, {
         method: "PUT",
         body: JSON.stringify(data)
      })
   },

   patch<T>(url: string, data?: any) {
      return apiFetch<T>(url, {
         method: "PATCH",
         body: JSON.stringify(data)
      })
   },

   delete<T>(url: string, data?:any) {
      return apiFetch<T>(url, {
         method: "DELETE"
      })
   }
}