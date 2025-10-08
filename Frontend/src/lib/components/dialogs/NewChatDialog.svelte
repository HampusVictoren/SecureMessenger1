<script lang="ts">
  import { chatStore } from "$lib/stores/chats";
  import { goto } from "$app/navigation";
  import { get } from "svelte/store";

  export let open: boolean = true;
  export let onClose: (() => void) | null = null;

  let username = "";
  let loading = false;
  let error: string | null = null;

  const uid = (typeof crypto !== "undefined" && "randomUUID" in crypto)
    ? crypto.randomUUID()
    : Math.random().toString(36).slice(2);
  const titleId = `newchat-title-${uid}`;
  const inputId = `newchat-username-${uid}`;
  const errorId = `newchat-error-${uid}`;

  function close() {
    open = false;
    onClose?.();
  }

  async function onSubmit(e: Event) {
    e.preventDefault();
    error = null;
    const u = username.trim();
    if (!u) {
      error = "Please enter a username.";
      return;
    }
    loading = true;
    try {
      await chatStore.createChatByUsername(u);
      const id = get(chatStore.activeChatId);
      if (id) {
        await goto(`/chat/${encodeURIComponent(id)}`);
      }
      close();
    } catch (e: any) {
      error = e?.message || "Failed to create chat.";
    } finally {
      loading = false;
    }
  }
</script>

{#if open}
  <div class="overlay">
    <div
      class="modal card shadow"
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      aria-describedby={error ? errorId : undefined}
    >
      <form on:submit={onSubmit}>
        <div class="header">
          <h2 id={titleId} class="title">Start new chat</h2>
          <button class="ghost" type="button" on:click={close} aria-label="Close dialog">Close</button>
        </div>

        <div class="body">
          <label for={inputId}>Username</label>
          <input
            id={inputId}
            name="username"
            placeholder="@username"
            bind:value={username}
            required
            autocomplete="off"
            aria-invalid={error ? "true" : "false"}
            aria-describedby={error ? errorId : undefined}
          />
          {#if error}
            <div id={errorId} class="error" role="alert">{error}</div>
          {/if}
        </div>

        <div class="footer">
          <button class="ghost" type="button" on:click={close}>Cancel</button>
          <button class="primary" type="submit" disabled={loading}>
            {loading ? "Starting…" : "Start chat"}
          </button>
        </div>
      </form>
    </div>
  </div>
{/if}

<style>
.overlay {
  position: fixed; inset: 0; display: grid; place-items: center;
  background: rgba(0,0,0,0.5);
}
.modal {
  width: min(520px, calc(100vw - 24px));
  padding: 16px;
}
.header, .footer {
  display: flex; align-items: center; justify-content: space-between;
}
.title { font-weight: 700; margin: 0; }
.body { display: grid; gap: 10px; padding: 12px 0; }
.error { color: var(--danger); font-size: 0.9rem; }
</style>