<script lang="ts">
  // Svelte 5: replace createEventDispatcher with a function prop
  export let onSend: ((payload: { text: string }) => void) | null = null;

  let text = "";

  function onKeydown(e: KeyboardEvent) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      send();
    }
  }

  function send() {
    const trimmed = text.trim();
    if (!trimmed) return;
    onSend?.({ text: trimmed });
    text = "";
  }
</script>

<div class="composer">
  <textarea rows="1" bind:value={text} on:keydown={onKeydown} placeholder="Type a message. Press Enter to send, Shift+Enter for a new line."></textarea>
  <button class="primary" on:click={send} aria-label="Send message">
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d="M5 12l14-7-7 14-2-5-5-2z" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>
    <span style="margin-left: 8px;">Send</span>
  </button>
</div>

<style>
.composer {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 10px;
  padding: 12px;
  border-top: 1px solid var(--border);
  background: linear-gradient(180deg, var(--surface), var(--bg));
}
textarea { resize: none; min-height: 42px; }
</style>