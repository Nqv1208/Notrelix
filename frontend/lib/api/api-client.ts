import { endpoints } from "@/lib/api/endpoints";
import { ApiError } from "@/lib/api/api-error";

const configuredBaseUrl = (process.env.NEXT_PUBLIC_API_URL ?? "/api/v1").replace(/\/$/, "");
const BASE_URL = configuredBaseUrl.endsWith("/api")
  ? `${configuredBaseUrl}/v1`
  : configuredBaseUrl;


export async function apiFetch<T>(
  url: string,
  options: RequestInit = {},
  retry = true
): Promise<T> {
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };

  const response = await fetch(`${BASE_URL}${url}`, {
    credentials: "include",
    ...options,
    headers,
  });

  if (
    response.status === 401 &&
    retry &&
    url !== endpoints.auth.refresh
  ) {
    return handleRefreshToken<T>(url, options);
  }

  const data = await parseJsonResponse(response);
  if (!response.ok) {
    throw new ApiError(response.status, extractErrorMessage(data), data);
  }

  return data as T;
}

let refreshPromise: Promise<void> | null = null;

async function refreshOnce(): Promise<void> {
  if (!refreshPromise) {
    refreshPromise = (async () => {
      const refreshResponse = await fetch(`${BASE_URL}${endpoints.auth.refresh}`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
      });

      const refreshData = await parseJsonResponse(refreshResponse);
      if (!refreshResponse.ok) {
        throw new ApiError(refreshResponse.status, extractErrorMessage(refreshData), refreshData);
      }
    })().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

async function handleRefreshToken<T>(
  url: string,
  options: RequestInit
): Promise<T> {
  await refreshOnce();
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
