// Minimal API client tailored for BFF usage with cookies and optional CSRF.
// Adjust endpoint paths to match your BFF routes.

const API_BASE = "/api";

function getCsrfTokenFromCookie(cookieName = "XSRF-TOKEN"): string | undefined {
  const cookies = typeof document !== "undefined" ? document.cookie : "";
  const token = cookies.split("; ").find((row) => row.startsWith(`${cookieName}=`));
  return token?.split("=")[1];
}

async function request<T>(
  path: string,
  init: RequestInit = {},
  opts?: { csrfHeader?: string; absolute?: boolean }
): Promise<T> {
  const url = opts?.absolute ? path : `${API_BASE}${path}`;
  const headers = new Headers(init.headers || {});
  headers.set("Accept", "application/json");

  // If sending JSON, ensure content-type
  if (init.body && !(init.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  // Optional: CSRF header if your BFF uses one
  const csrfHeaderName = opts?.csrfHeader || "X-CSRF-Token";
  const csrfToken = getCsrfTokenFromCookie();
  if (csrfToken) headers.set(csrfHeaderName, csrfToken);

  const res = await fetch(url, {
    ...init,
    headers,
    credentials: "include", // include cookies for BFF
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`HTTP ${res.status} ${res.statusText} for ${url}: ${text}`);
  }

  if (res.status === 204) return undefined as unknown as T;
  return res.json() as Promise<T>;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: "GET" }),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined }),
  del: <T>(path: string) => request<T>(path, { method: "DELETE" }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PUT", body: body ? JSON.stringify(body) : undefined }),

  // Helper for Server-Sent Events
  sse: (path: string, onMessage: (data: MessageEvent) => void, onError?: (e: Event) => void) => {
    const url = `${API_BASE}${path}`;
    const es = new EventSource(url, { withCredentials: true });
    es.onmessage = onMessage;
    if (onError) es.onerror = onError;
    return es;
  },

  // Helper for WebSockets (if your BFF supports it)
  ws: (path: string): WebSocket => {
    const scheme = location.protocol === "https:" ? "wss" : "ws";
    const url = `${scheme}://${location.host}${API_BASE}${path}`;
    return new WebSocket(url);
  },
};