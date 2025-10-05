<script lang="ts">
  import type { Message, User } from "$lib/types";
  import { auth } from "$lib/stores/auth";

  export let messages: Message[] = [];
  export let participants: User[] = [];

  const me$ = auth.user;

  function isMine(m: Message) {
    const me = $me$;
    return me?.id && m.senderId === me.id || m.senderId === "me"; // "me" used for optimistic
  }

  function initialsOf(userId: string) {
    const u = participants.find((p) => p.id === userId);
    const name = u?.displayName || u?.username || "?";
    return name.split(" ").map(s => s[0]).join("").slice(0,2).toUpperCase();
  }
</script>

<div class="messages scroll">
  {#each messages as m (m.id)}
    <div class="row {isMine(m) ? 'mine' : ''}">
      {#if !isMine(m)}
        <div class="avatar">{initialsOf(m.senderId)}</div>
      {/if}
      <div class="bubble card">
        <div class="content">{m.content}</div>
        <div class="meta">
          <span>{new Date(m.sentAt).toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'})}</span>
          {#if isMine(m)}
            <span class="status">
              {#if m.status === 'sending'}Sending…{:else if m.status === 'failed'}<span class="failed">Failed</span>{:else}{m.status || 'sent'}{/if}
            </span>
          {/if}
        </div>
      </div>
    </div>
  {/each}
</div>

<style>
.messages {
  flex: 1;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.row {
  display: flex;
  align-items: flex-end;
  gap: 10px;
  max-width: 80%;
}
.row.mine {
  margin-left: auto;
  justify-content: flex-end;
}
.avatar {
  width: 28px; height: 28px; border-radius: 50%;
  background: var(--surface-2); border: 1px solid var(--border);
  display: grid; place-items: center;
  font-size: 0.75rem; color: var(--muted);
}
.bubble {
  background: var(--surface-2);
  padding: 10px 12px;
  border-radius: 14px;
}
.row.mine .bubble {
  background: linear-gradient(180deg, rgba(27,94,32,0.3), rgba(27,94,32,0.2));
  outline: 1px solid var(--accent-700);
}
.content { white-space: pre-wrap; }
.meta {
  display: flex; gap: 10px; color: var(--muted); font-size: 0.75rem; margin-top: 6px;
}
.failed { color: var(--danger); }
</style>