<script lang="ts">
  import { auth } from "$lib/stores/auth";
  import NewChatDialog from "$lib/components/dialogs/NewChatDialog.svelte";
  import { chatStore } from "$lib/stores/chats";
  import { onMount } from "svelte";

  let showNewChat = false;

  const user$ = auth.user;

  onMount(() => {
    chatStore.loadChats();
  });

  function toggleNewChat() {
    showNewChat = !showNewChat;
  }

  // Ensure we always pass a string to encodeURIComponent
  $: userNameSeed = $user$ ? ($user$.displayName || $user$.username || "User") : "User";
  $: avatarSrc = $user$?.avatarUrl || `https://api.dicebear.com/9.x/initials/svg?seed=${encodeURIComponent(userNameSeed)}`;
</script>

<header class="topbar">
  <div class="left">
    <div class="logo">SecureMessenger</div>
  </div>
  <div class="center">
    <div class="search card">
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M21 21l-4.35-4.35m1.35-4.65a7 7 0 11-14 0 7 7 0 0114 0z" stroke="var(--muted)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      <input placeholder="Search chats or people (Ctrl+/)" />
    </div>
  </div>
  <div class="right">
    <button class="primary" on:click={toggleNewChat}>
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true" style="margin-right:8px">
        <path d="M12 5v14m7-7H5" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      New chat
    </button>
    {#if $user$}
      <div class="user">
        <img class="avatar" src={avatarSrc} alt="avatar" />
        <div class="meta">
          <div class="name">{$user$.displayName || $user$.username || "User"}</div>
          <button class="ghost small" on:click={auth.logout}>Log out</button>
        </div>
      </div>
    {:else}
      <button class="ghost" on:click={auth.login}>Log in</button>
    {/if}
  </div>

  {#if showNewChat}
    <NewChatDialog bind:open={showNewChat} />
  {/if}
</header>

<style>
.topbar {
  display: grid;
  grid-template-columns: 1fr 2fr 1fr;
  gap: 12px;
  align-items: center;
  padding: 12px 16px;
  border-bottom: 1px solid var(--border);
  background: linear-gradient(0deg, var(--bg), var(--surface));
}
.logo {
  font-weight: 700;
  letter-spacing: 0.3px;
  color: white;
}
.left { display: flex; align-items: center; }
.center { display: flex; justify-content: center; }
.search {
  display: flex; align-items: center; gap: 10px;
  padding: 8px 12px;
  width: min(600px, 100%);
  background: var(--surface-2);
}
.search input {
  flex: 1; border: none; background: transparent; color: var(--text);
}
.right { display: flex; justify-content: flex-end; align-items: center; gap: 12px; }
.user { display: flex; align-items: center; gap: 10px; }
.avatar {
  width: 36px; height: 36px; border-radius: 50%;
  border: 1px solid var(--border);
}
.meta { display: flex; flex-direction: column; gap: 4px; }
.meta .name { font-size: 0.95rem; }
.small { padding: 4px 8px; font-size: 0.8rem; }
</style>