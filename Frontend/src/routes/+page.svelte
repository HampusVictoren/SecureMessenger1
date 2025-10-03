<script lang="ts">
  import { onMount } from 'svelte';
  import { getUserInfo } from '$lib/auth';

  let userinfo: Record<string, unknown> | null = null;

  onMount(async () => {
    userinfo = await getUserInfo();
  });
</script>

<h2>UserInfo (via BFF)</h2>
{#if userinfo}
  <pre>{JSON.stringify(userinfo, null, 2)}</pre>
{:else}
  <p>No userinfo (not signed in or insufficient scope).</p>
{/if}