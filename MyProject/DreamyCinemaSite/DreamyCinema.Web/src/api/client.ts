import type { SessionInfo } from "@/types";

let csrfToken = "";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number
  ) {
    super(message);
  }
}

export function setCsrfToken(token: string) {
  csrfToken = token;
}

export async function loadSession(): Promise<SessionInfo> {
  const session = await apiRequest<SessionInfo>("/api/auth/session", { skipAuthRedirect: true });
  setCsrfToken(session.requestToken);
  return session;
}

type RequestOptions = RequestInit & { skipAuthRedirect?: boolean };

export async function apiRequest<T = void>(url: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers);
  const method = (options.method ?? "GET").toUpperCase();
  if (["POST", "PUT", "PATCH", "DELETE"].includes(method) && csrfToken) {
    headers.set("X-CSRF-TOKEN", csrfToken);
  }

  const response = await fetch(url, {
    ...options,
    headers,
    credentials: "same-origin"
  });

  if (response.status === 401 && !options.skipAuthRedirect) {
    window.location.assign("/login");
    throw new ApiError("登录已失效，请重新登录。", response.status);
  }

  if (!response.ok) {
    let message = `请求失败 (${response.status})`;
    const contentType = response.headers.get("content-type") ?? "";
    if (contentType.includes("application/json")) {
      const payload = (await response.json()) as { message?: string };
      message = payload.message || message;
    } else {
      const text = await response.text();
      if (text.trim()) message = text;
    }
    throw new ApiError(message, response.status);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
