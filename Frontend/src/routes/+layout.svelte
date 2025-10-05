<!-- <script lang="ts">
  import { onMount } from 'svelte';
  // import { initCsrf } from '$lib/initCsrf';
  import { getMe, login, signout } from '$lib/auth';

  let me: Awaited<ReturnType<typeof getMe>> = null;
  let loading = true;

  async function refresh() {
    me = await getMe();
  }

  onMount(async () => {
    //await initCsrf();
    await refresh();
    loading = false;
  });

  async function doLogin() {
    login();
  }



  function doSignout() {
    signout();
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
    <button on:click={doSignout}>Sign out (full)</button>
  {:else}
    <p>Not signed in.</p>
    <button on:click={doLogin}>Login</button>
  {/if}
{/if}

<slot /> -->
<script lang="ts">
  import TopBar from "$lib/components/TopBar.svelte";
  import Sidebar from "$lib/components/Sidebar.svelte";
  import "$lib/styles/theme.css";
</script>

<TopBar />

<main class="shell">
  <Sidebar />
  <section class="content">
    <slot />
  </section>
</main>

<style>
.shell {
  display: grid;
  grid-template-columns: 320px 1fr;
  min-height: calc(100vh - 66px);
}
.content {
  min-width: 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
  background: linear-gradient(180deg, var(--bg), var(--surface));
}
@media (max-width: 900px) {
  .shell {
    grid-template-columns: 1fr;
  }
  .content {
    order: 2;
  }
}
</style>