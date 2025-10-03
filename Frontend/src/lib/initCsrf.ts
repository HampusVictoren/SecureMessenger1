// import { setCsrfToken, apiFetch } from '$lib/api';

// export async function initCsrf(): Promise<void> {
//   const resp = await apiFetch('/bff/csrf');
//   if (!resp.ok) return;
//   const data = (await resp.json()) as { token: string };
//   if (data?.token) setCsrfToken(data.token);
// }