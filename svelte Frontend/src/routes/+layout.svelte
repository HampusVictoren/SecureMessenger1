<script lang="ts">
  import { onMount } from 'svelte';
  import { initCsrf } from '$app/initCsrf';
  import { getMe, login, logoutAjax, signout } from '$lib/auth';

  let me: Awaited<ReturnType<typeof getMe>> = null;
  let loading = true;

  async function refresh() {
    me = await getMe();
  }

  onMount(async () => {
    await initCsrf();
    await refresh();
    loading = false;
  });

  async function doLogin() {
    login(); // navigates to backend to start OIDC flow
  }

  async function doLogoutAjax() {
    const ok = await logoutAjax();
    if (ok) await refresh();
  }

  function doSignout() {
    signout(); // full RP-initiated signout (navigation)
  }
</script>

<svelte:head>
  <title>BFF Auth Test</title>
</svelte:head>

{#if loading}
  <p>Loading…</p>
{:else}
  {#if me}
    <p>Signed in as: <strong>{me.name}</strong></p>
    <button on:click={doLogoutAjax}>Logout (AJAX)</button>
    <button on:click={doSignout}>Sign out (full)</button>
  {:else}
    <p>Not signed in.</p>
    <button on:click={doLogin}>Login</button>
  {/if}
{/if}

<slot />