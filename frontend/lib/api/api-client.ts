import { endpoints } from "@/lib/api/endpoints";
import { ApiError } from "@/lib/api/api-error";
import { AUTH_ERROR_KEYS } from "@/features/auth/i18n/auth-error-keys";
import { tokenStorage } from "@/lib/auth/token-storage";

const BASE_URL = (process.env.NEXT_PUBLIC_API_URL ?? "/api").replace(/\/$/, "");

type RefreshResponse = {
  accessToken: string;
  refreshToken: string;
};

export async function apiFetch<T>(
  url: string,
  options: RequestInit = {},
  retry = true
): Promise<T> {
  const accessToken = tokenStorage.getAccessToken();
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };

  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  const response = await fetch(`${BASE_URL}${url}`, {
    ...options,
    headers,
  });

  if (
    response.status === 401 &&
    retry &&
    url !== endpoints.auth.refresh &&
    url !== endpoints.auth.profile
  ) {
    return handleRefreshToken<T>(url, options);
  }

  const data = await parseJsonResponse(response);
  if (!response.ok) {
    throw new ApiError(response.status, extractErrorMessage(data), data);
  }

  return data as T;
}

async function handleRefreshToken<T>(
  url: string,
  options: RequestInit
): Promise<T> {
  const refreshToken = tokenStorage.getRefreshToken();
  if (!refreshToken) {
    tokenStorage.clearTokens();
    throw new ApiError(401, AUTH_ERROR_KEYS.REFRESH_INVALID);
  }

  const refreshResponse = await fetch(`${BASE_URL}${endpoints.auth.refresh}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ refreshToken }),
  });

  const refreshData = (await parseJsonResponse(refreshResponse)) as RefreshResponse;
  if (!refreshResponse.ok) {
    tokenStorage.clearTokens();
    throw new ApiError(refreshResponse.status, extractErrorMessage(refreshData), refreshData);
  }

  tokenStorage.setTokens(refreshData.accessToken, refreshData.refreshToken);
  return apiFetch<T>(url, options, false);
}

export const api = {
  get<T>(url: string) {
    return apiFetch<T>(url);
  },

  post<T>(url: string, data?: unknown) {
    return apiFetch<T>(url, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  put<T>(url: string, data?: unknown) {
    return apiFetch<T>(url, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  patch<T>(url: string, data?: unknown) {
    return apiFetch<T>(url, {
      method: "PATCH",
      body: JSON.stringify(data),
    });
  },

  delete<T>(url: string) {
    return apiFetch<T>(url, {
      method: "DELETE",
    });
  },
};

async function parseJsonResponse(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) return null;

  try {
    return JSON.parse(text);
  } catch {
    return { message: text };
  }
}

function extractErrorMessage(data: unknown): string {
  if (!data || typeof data !== "object") return "Request failed";

  const payload = data as { message?: string; detail?: string; errors?: unknown };
  if (payload.message) return payload.message;
  if (payload.detail) return payload.detail;
  if (Array.isArray(payload.errors) && payload.errors.length > 0) {
    return String(payload.errors[0]);
  }

  return "Request failed";
}