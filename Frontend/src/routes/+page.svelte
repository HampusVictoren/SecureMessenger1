<script lang="ts">
  import { onMount } from "svelte";
  import { getMe } from "$lib/auth";
  import NewChatDialog from "$lib/components/dialogs/NewChatDialog.svelte";

  let showNewChat = false;
  let displayName: string | null = null;

  onMount(async () => {
    const me = await getMe(); // Me | null
    // me?.name is fine; if you prefer preferred_username claim, switch to getUserInfo() and read it there.
    displayName = me?.name ?? null;
  });

  function openNewChat() {
    showNewChat = true;
  }
</script>

<section class="empty-state">
  <div class="card hero">
    <div class="icon">
      <svg width="32" height="32" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M21 15a4 4 0 01-4 4H7l-4 4V7a4 4 0 014-4h10a4 4 0 014 4v8z" stroke="var(--accent-600)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </div>
    <h1>Welcome{#if displayName}, {displayName}{/if}</h1>
    <p>Start a new conversation or select an existing chat from the sidebar.</p>
    <div class="actions">
      <button type="button" class="button primary" on:click={openNewChat}>
        Start new chat
      </button>
    </div>
  </div>
</section>

{#if showNewChat}
  <NewChatDialog bind:open={showNewChat} />
{/if}

<style>
.empty-state {
  flex: 1; display: grid; place-items: center; padding: 20px;
}
.hero {
  padding: 24px; max-width: 560px; text-align: center;
}
.icon {
  width: 64px; height: 64px; margin: 0 auto 12px auto;
  display: grid; place-items: center;
  border-radius: 50%;
  background: rgba(27, 94, 32, 0.15);
  outline: 1px solid var(--accent-700);
}
h1 { margin: 8px 0; }
p { color: var(--muted); }
.actions { margin-top: 16px; }
.button { display: inline-flex; align-items: center; gap: 8px; padding: 10px 14px; border-radius: 10px; border: 1px solid var(--border); }
.button.primary { background: linear-gradient(180deg, var(--accent-600), var(--accent)); color: white; border-color: transparent; }
</style>