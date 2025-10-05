<script lang="ts">
  import { onMount } from "svelte";
  import { primeCsrf, postBff, getBff } from "./lib/bff";
  import LogoutButton from './features/auth/LogoutButton.svelte';

  onMount(() => {
    // Sets the __Host-Antiforgery cookie and caches a token
    primeCsrf().catch(console.error);
  });

  async function logout() {
    await postBff("/bff/logout");
  }

  async function loadUser() {
    const user = await getBff("/bff/user");
    console.log(user);
  }
</script>

<button on:click={loadUser}>Load user</button>
<button on:click={logout}>Logout</button>
<LogoutButton />