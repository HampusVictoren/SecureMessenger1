// let csrfToken: string | null = null;

// export function setCsrfToken(token: string) {
//   csrfToken = token;
// }

// // Methods that don't require CSRF header
// const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS', 'TRACE']);

// // Avoid infinite loops: don't redirect to login from auth-related pages
// function isAuthRoute(pathname: string): boolean {
//   return pathname.startsWith('/bff')
//       || pathname.startsWith('/connect')
//       || pathname.startsWith('/Identity');
// }

// /**
//  * Same-origin fetch wrapper for the BFF.
//  * - Sends credentials (cookies)
//  * - Adds X-Requested-With so backend returns 401 (not 302) for AJAX under /bff
//  * - Adds X-CSRF for unsafe methods if token is present
//  * - Redirects top-level to /bff/login on 401, preserving returnUrl
//  */
// export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
//   // Use relative paths when calling from the SPA served by the backend
//   const url = path.startsWith('http') ? path : path;

//   const method = (init.method ?? 'GET').toUpperCase();
//   const headers = new Headers(init.headers as HeadersInit);

//   headers.set('Accept', 'application/json');
//   // Mark as AJAX so the server returns 401 instead of a redirect for /bff requests
//   headers.set('X-Requested-With', 'XMLHttpRequest');

//   if (!SAFE_METHODS.has(method) && csrfToken) {
//     headers.set('X-CSRF', csrfToken);
//   }

//   const resp = await fetch(url, {
//     ...init,
//     method,
//     headers,
//     credentials: 'include'
//   });

//   // Redirect to BFF login on 401 (browser context only)
//   if (resp.status === 401 && typeof window !== 'undefined') {
//     const { pathname, href } = window.location;
//     if (!isAuthRoute(pathname)) {
//       window.location.href = '/bff/login?returnUrl=' + encodeURIComponent(href);
//     }
//     throw new Error('Unauthorized');
//   }

//   return resp;
// }

// export async function getJson<T>(path: string): Promise<T | null> {
//   const resp = await apiFetch(path);
//   if (!resp.ok) return null;
//   return (await resp.json()) as T;
// }

// export async function postJson<T>(path: string, body?: unknown): Promise<T | null> {
//   const resp = await apiFetch(path, {
//     method: 'POST',
//     headers: { 'Content-Type': 'application/json' },
//     body: body === undefined ? undefined : JSON.stringify(body)
//   });
//   if (!resp.ok) return null;
//   const txt = await resp.text();
//   return txt ? (JSON.parse(txt) as T) : (null as T | null);
// }
// Avoid infinite loops: don't redirect to login from auth-related pages
function isAuthRoute(pathname: string): boolean {
  return pathname.startsWith('/bff')
      || pathname.startsWith('/connect')
      || pathname.startsWith('/Identity');
}

/**
 * Same-origin fetch wrapper for the BFF.
 * - Sends credentials (cookies)
 * - Adds X-Requested-With so backend returns 401 (not 302) for AJAX under /bff
 * - Redirects top-level to /bff/login on 401, preserving returnUrl
 */
export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const url = path.startsWith('http') ? path : path;

  const method = (init.method ?? 'GET').toUpperCase();
  const headers = new Headers(init.headers as HeadersInit);

  headers.set('Accept', 'application/json');
  headers.set('X-Requested-With', 'XMLHttpRequest');

  const resp = await fetch(url, {
    ...init,
    method,
    headers,
    credentials: 'include'
  });

  // Redirect to BFF login on 401 (browser context only)
  if (resp.status === 401 && typeof window !== 'undefined') {
    const { pathname, href } = window.location;
    if (!isAuthRoute(pathname)) {
      window.location.href = '/bff/login?returnUrl=' + encodeURIComponent(href);
    }
    throw new Error('Unauthorized');
  }

  return resp;
}

export async function getJson<T>(path: string): Promise<T | null> {
  const resp = await apiFetch(path);
  if (!resp.ok) return null;
  return (await resp.json()) as T;
}

export async function postJson<T>(path: string, body?: unknown): Promise<T | null> {
  const resp = await apiFetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body)
  });
  if (!resp.ok) return null;
  const txt = await resp.text();
  return txt ? (JSON.parse(txt) as T) : (null as T | null);
}