import { derived, get, writable } from "svelte/store";
import type { Chat, Message, CreateChatRequest, CreateChatResponse, SendMessageRequest, SendMessageResponse } from "$lib/types";
import { api } from "$lib/api/client";

type MessagesState = {
  loading: boolean;
  items: Message[];
  error?: string;
  // optional real-time connection handle
  es?: EventSource;
  ws?: WebSocket;
};

const chats = writable<Chat[]>([]);
const activeChatId = writable<string | null>(null);
const chatsLoading = writable<boolean>(false);
const messagesByChat = writable<Record<string, MessagesState>>({});

async function loadChats() {
  chatsLoading.set(true);
  try {
    const data = await api.get<Chat[]>("/chats");
    // sort by updatedAt desc
    data.sort((a, b) => (new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()));
    chats.set(data);
  } finally {
    chatsLoading.set(false);
  }
}

function ensureMessagesState(chatId: string) {
  const current = get(messagesByChat);
  if (!current[chatId]) {
    current[chatId] = { loading: false, items: [] };
    messagesByChat.set({ ...current });
  }
}

async function loadMessages(chatId: string) {
  ensureMessagesState(chatId);
  const current = get(messagesByChat);
  current[chatId].loading = true;
  messagesByChat.set({ ...current });
  try {
    const msgs = await api.get<Message[]>(`/chats/${encodeURIComponent(chatId)}/messages`);
    current[chatId].items = msgs.sort((a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
  } catch (e: any) {
    current[chatId].error = e?.message || "Failed to load messages";
  } finally {
    current[chatId].loading = false;
    messagesByChat.set({ ...current });
  }
}

function connectRealtime(chatId: string, transport: "sse" | "ws" = "sse") {
  ensureMessagesState(chatId);
  const current = get(messagesByChat);
  const state = current[chatId];

  // Clean up old connection
  state.es?.close?.();
  state.ws?.close?.();

  if (transport === "sse") {
    const es = api.sse(`/chats/${encodeURIComponent(chatId)}/stream`, (evt) => {
      try {
        const data = JSON.parse(evt.data) as Message;
        appendOrUpdateMessage(chatId, data);
      } catch {
        // ignore malformed
      }
    });
    state.es = es;
  } else {
    const ws = api.ws(`/chats/${encodeURIComponent(chatId)}/ws`);
    ws.onmessage = (ev) => {
      try {
        const data = JSON.parse(ev.data) as Message;
        appendOrUpdateMessage(chatId, data);
      } catch {
        // ignore malformed
      }
    };
    state.ws = ws;
  }

  messagesByChat.set({ ...current });
}

function disconnectRealtime(chatId: string) {
  const current = get(messagesByChat);
  const state = current[chatId];
  if (!state) return;
  state.es?.close?.();
  state.ws?.close?.();
  delete state.es;
  delete state.ws;
  messagesByChat.set({ ...current });
}

function appendOrUpdateMessage(chatId: string, msg: Message) {
  const current = get(messagesByChat);
  const state = current[chatId];
  if (!state) return;
  const idx = state.items.findIndex((m) => m.id === msg.id);
  if (idx >= 0) {
    state.items[idx] = { ...state.items[idx], ...msg };
  } else {
    state.items = [...state.items, msg].sort((a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
  }
  messagesByChat.set({ ...current });
}

async function sendMessage(chatId: string, content: string) {
  const tempId = `temp-${crypto.randomUUID?.() || Math.random()}`;
  const optimistic: Message = {
    id: tempId,
    chatId,
    senderId: "me", // your BFF should replace this; used only for UI alignment before server reply
    content,
    sentAt: new Date().toISOString(),
    status: "sending",
  };
  appendOrUpdateMessage(chatId, optimistic);

  try {
    const sent = await api.post<SendMessageResponse>(`/chats/${encodeURIComponent(chatId)}/messages`, {
      content,
    } as SendMessageRequest);

    // Replace optimistic message
    const current = get(messagesByChat);
    const state = current[chatId];
    const idx = state.items.findIndex((m) => m.id === tempId);
    if (idx >= 0) {
      state.items[idx] = sent.message;
      messagesByChat.set({ ...current });
    } else {
      appendOrUpdateMessage(chatId, sent.message);
    }
  } catch (e) {
    // mark optimistic as failed
    const current = get(messagesByChat);
    const state = current[chatId];
    const idx = state.items.findIndex((m) => m.id === tempId);
    if (idx >= 0) {
      state.items[idx] = { ...state.items[idx], status: "failed" };
      messagesByChat.set({ ...current });
    }
    throw e;
  }
}

async function createChatByUsername(username: string) {
  const res = await api.post<CreateChatResponse>("/chats", { username } as CreateChatRequest);
  const list = get(chats);
  const exists = list.find((c) => c.id === res.chat.id);
  if (!exists) {
    chats.set([res.chat, ...list]);
  }
  activeChatId.set(res.chat.id);
  await loadMessages(res.chat.id);
  connectRealtime(res.chat.id);
}

function setActiveChat(id: string | null) {
  activeChatId.set(id);
  if (id) {
    loadMessages(id);
    connectRealtime(id);
  }
}

const activeChat = derived([chats, activeChatId], ([$chats, $id]) => $chats.find((c) => c.id === $id) || null);
const messages = derived([messagesByChat, activeChatId], ([$map, $id]) => ($id ? $map[$id]?.items || [] : []));

export const chatStore = {
  chats,
  chatsLoading,
  activeChatId,
  activeChat,
  messages,
  loadChats,
  loadMessages,
  setActiveChat,
  sendMessage,
  createChatByUsername,
  connectRealtime,
  disconnectRealtime,
};