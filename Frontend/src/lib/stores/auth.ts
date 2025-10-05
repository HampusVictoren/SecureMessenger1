import { writable, derived } from "svelte/store";
import type { Me } from "$lib/auth";
import { getMe, login as bffLogin, signout as bffSignout } from "$lib/auth";

type SessionUser = {
  id?: string;
  username?: string;
  displayName?: string | null;
  avatarUrl?: string;
  raw: Me;
};

type AuthState = {
  user: SessionUser | null;
  loading: boolean;
  error?: string;
};

const initial: AuthState = { user: null, loading: true };

function claim(me: Me, type: string): string | undefined {
  if (!me) return undefined;
  return me.claims?.find((c) => c.type === type)?.value;
}

function toSessionUser(me: Me): SessionUser | null {
  if (!me) return null;
  const id = claim(me, "sub") || undefined;
  const preferred = claim(me, "preferred_username");
  const displayName = me.name || preferred || null;
  const username = preferred || me.name || undefined;
  const avatarUrl = claim(me, "picture") || undefined;
  return { id, username, displayName, avatarUrl, raw: me };
}

function createAuthStore() {
  const store = writable<AuthState>(initial);

  async function bootstrap() {
    try {
      const me = await getMe();
      store.set({ user: toSessionUser(me), loading: false });
    } catch (e: any) {
      store.set({ user: null, loading: false, error: e?.message || "Not authenticated" });
    }
  }

  function login() {
    bffLogin();
  }

  function logout() {
    // For BFF signout, a hard redirect is expected
    bffSignout();
  }

  // initialize
  bootstrap();

  const user = derived(store, (s) => s.user);
  const loading = derived(store, (s) => s.loading);

  return { subscribe: store.subscribe, user, loading, login, logout, refresh: bootstrap };
}

export const auth = createAuthStore();