<script lang="ts">
  import { chatStore } from "$lib/stores/chats";
  import type { Chat } from "$lib/types";
  import { derived } from "svelte/store";

  const { chats, chatsLoading, activeChatId, setActiveChat } = chatStore;

  function chatTitle(c: Chat): string {
    if (c.title) return c.title;
    // Derive title from participants (excluding self, if your BFF marks it)
    return c.participants.map((p) => p.displayName || p.username).join(", ");
  }

  const activeId = activeChatId;

  const sorted = derived(chats, ($chats) =>
    [...$chats].sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
  );

  function openChat(id: string) {
    setActiveChat(id);
  }
</script>

<aside class="sidebar">
  <div class="section-header">
    <div class="title">Chats</div>
    {#if $chatsLoading}<div class="badge">Loading…</div>{/if}
  </div>

  <div class="list scroll">
    {#each $sorted as c}
      <button class="row {c.id === $activeId ? 'active' : ''}" on:click={() => openChat(c.id)} aria-current={c.id === $activeId ? 'page' : undefined}>
        <img class="avatar" alt="avatar"
          src={c.participants[0]?.avatarUrl || `https://api.dicebear.com/9.x/identicon/svg?seed=${encodeURIComponent(c.id)}`} />
        <div class="col">
          <div class="top">
            <div class="name">{chatTitle(c)}</div>
            <div class="time">{new Date(c.updatedAt).toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'})}</div>
          </div>
          <div class="bottom">
            <div class="preview">{c.lastMessage?.content || "No messages yet"}</div>
            {#if c.unreadCount}
              <div class="badge">{c.unreadCount}</div>
            {/if}
          </div>
        </div>
      </button>
    {:else}
      <div class="empty">No chats yet. Start a new conversation.</div>
    {/each}
  </div>
</aside>

<style>
.sidebar {
  width: 320px;
  border-right: 1px solid var(--border);
  background: linear-gradient(180deg, var(--surface), var(--bg));
  display: flex;
  flex-direction: column;
  min-height: 0;
}
.section-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 12px;
  border-bottom: 1px solid var(--border);
}
.title { font-weight: 600; }
.list { padding: 6px; }
.row {
  width: 100%;
  display: grid;
  grid-template-columns: 44px 1fr;
  gap: 10px;
  padding: 10px;
  border-radius: 10px;
  background: transparent;
  border: none;
  text-align: left;
}
.row:hover { background: var(--surface-2); }
.row.active { background: rgba(27, 94, 32, 0.2); outline: 1px solid var(--accent-700); }
.avatar { width: 44px; height: 44px; border-radius: 50%; border: 1px solid var(--border); }
.col { display: flex; flex-direction: column; gap: 6px; }
.top { display: flex; align-items: baseline; justify-content: space-between; }
.name { font-weight: 600; }
.time { color: var(--muted); font-size: 0.8rem; }
.bottom { display: flex; align-items: center; gap: 8px; justify-content: space-between; }
.preview { color: var(--muted); font-size: 0.9rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.empty { padding: 16px; color: var(--muted); }
</style>