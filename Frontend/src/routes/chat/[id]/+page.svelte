<script lang="ts">
  import { onDestroy, onMount } from "svelte";
  import MessageList from "$lib/components/MessageList.svelte";
  import MessageInput from "$lib/components/MessageInput.svelte";
  import { chatStore } from "$lib/stores/chats";

  export let params: { id: string };

  const {
    activeChat,
    messages,
    setActiveChat,
    connectSignalR,
    disconnectSignalR,
    sendMessage,
    loadMessages
  } = chatStore;

  onMount(async () => {
    setActiveChat(params.id);
    await loadMessages(params.id);
    await connectSignalR(params.id);
  });

  onDestroy(() => {
    disconnectSignalR(params.id);
  });

  async function handleSend(e: { text: string }) {
    await sendMessage(params.id, e.text);
  }
</script>

{#if $activeChat}
  <div class="chat">
    <header class="chat-header">
      <div class="title">
        {$activeChat.title || $activeChat.participants.map((p) => p.displayName || p.username).join(", ")}
      </div>
      <div class="sub">{$activeChat.participants.length} participants</div>
    </header>

    <MessageList messages={$messages} participants={$activeChat.participants} />

    <MessageInput onSend={handleSend} />
  </div>
{:else}
  <div class="loading">Loading chat…</div>
{/if}

<style>
.chat { display: flex; flex-direction: column; min-height: 0; flex: 1; }
.chat-header { padding: 12px 16px; border-bottom: 1px solid var(--border); background: linear-gradient(180deg, var(--surface), var(--bg)); }
.title { font-weight: 700; }
.sub { color: var(--muted); font-size: 0.9rem; }
.loading { padding: 16px; color: var(--muted); }
</style>