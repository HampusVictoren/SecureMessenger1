import { getJson } from '$lib/api';

export type UserClaim = { type: string; value: string };
export type Me = { name: string | null; claims: UserClaim[] } | null;

export async function getMe(): Promise<Me> {
  return await getJson<Me>('/bff/user');
}

export async function getUserInfo(): Promise<Record<string, unknown> | null> {
  return await getJson<Record<string, unknown>>('/bff/userinfo');
}

export function login(): void {
  const returnUrl = window.location.href;
  window.location.href = '/bff/login?returnUrl=' + encodeURIComponent(returnUrl);
}

// export async function logoutAjax(): Promise<boolean> {
//   const resp = await postJson<unknown>('/bff/logout');
//   return resp !== null;
// }

export function signout(): void {
  window.location.href = '/bff/signout';
}