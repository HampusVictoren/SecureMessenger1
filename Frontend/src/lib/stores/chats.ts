import { writable, derived, get } from "svelte/store";
import type {
  Chat,
  Message,
  CreateChatRequest,
  CreateChatResponse,
  SendMessageRequest,
  SendMessageResponse
} from "$lib/types";
import { getJson, postJson } from "$lib/api";
import { ensureConnected } from "$lib/realtime/signalr";

type MessagesState = {
  loading: boolean;
  items: Message[];
  error?: string;
  joined?: boolean;
};

const chats = writable<Chat[]>([]);
const activeChatId = writable<string | null>(null);
const chatsLoading = writable<boolean>(false);
const messagesByChat = writable<Record<string, MessagesState>>({});

async function loadChats() {
  chatsLoading.set(true);
  try {
    const data = await getJson<Chat[]>("/api/chats");
    if (data) {
      data.sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime());
      chats.set(data);
    }
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
    const msgs = await getJson<Message[]>(`/api/chats/${encodeURIComponent(chatId)}/messages`);
    if (msgs) {
      current[chatId].items = msgs.sort(
        (a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime()
      );
    }
  } catch (e: any) {
    current[chatId].error = e?.message || "Failed to load messages";
  } finally {
    current[chatId].loading = false;
    messagesByChat.set({ ...current });
  }
}

function appendOrUpdateMessage(chatId: string, msg: Message) {
  const current = get(messagesByChat);
  const state = current[chatId];
  if (!state) return;
  const idx = state.items.findIndex((m) => m.id === msg.id);
  if (idx >= 0) {
    state.items[idx] = { ...state.items[idx], ...msg };
  } else {
    state.items = [...state.items, msg].sort(
      (a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime()
    );
  }
  messagesByChat.set({ ...current });
}

async function connectSignalR(chatId: string) {
  ensureMessagesState(chatId);
  const hub = await ensureConnected();

  // Subscribe once globally
  if (!(hub as any).__subscribed) {
    hub.on("ReceiveMessage", (msg: Message) => {
      appendOrUpdateMessage(msg.chatId, msg);
    });
    (hub as any).__subscribed = true;
  }

  await hub.invoke("JoinChat", chatId);
  const current = get(messagesByChat);
  current[chatId].joined = true;
  messagesByChat.set({ ...current });
}

async function disconnectSignalR(chatId: string) {
  const hub = await ensureConnected();
  await hub.invoke("LeaveChat", chatId);
  const current = get(messagesByChat);
  if (current[chatId]) current[chatId].joined = false;
  messagesByChat.set({ ...current });
}

async function sendMessage(chatId: string, content: string) {
  // optimistic UI
  const tempId = `temp-${crypto.randomUUID?.() || Math.random()}`;
  const optimistic: Message = {
    id: tempId,
    chatId,
    senderId: "me",
    content,
    sentAt: new Date().toISOString(),
    status: "sending",
  };
  appendOrUpdateMessage(chatId, optimistic);

  try {
    const hub = await ensureConnected();
    await hub.invoke("SendMessage", chatId, content);
    // The real message will arrive via ReceiveMessage. Mark optimistic as sent in the meantime.
    const map = get(messagesByChat);
    const state = map[chatId];
    const idx = state.items.findIndex((m) => m.id === tempId);
    if (idx >= 0) {
      state.items[idx] = { ...state.items[idx], status: "sent" };
      messagesByChat.set({ ...map });
    }
  } catch (e) {
    const map = get(messagesByChat);
    const state = map[chatId];
    const idx = state.items.findIndex((m) => m.id === tempId);
    if (idx >= 0) {
      state.items[idx] = { ...state.items[idx], status: "failed" };
      messagesByChat.set({ ...map });
    }
    throw e;
  }
}

async function createChatByUsername(username: string): Promise<string | null> {
  const res = await postJson<{ chat: Chat }>("/api/chats", { username } as CreateChatRequest);
  if (!res) return null;
  const chat = res.chat;
  const list = get(chats);
  if (!list.find((c) => c.id === chat.id)) {
    chats.set([chat, ...list]);
  }
  activeChatId.set(chat.id);
  await loadMessages(chat.id);
  await connectSignalR(chat.id);
  return chat.id;
}

function setActiveChat(id: string | null) {
  activeChatId.set(id);
  if (id) {
    loadMessages(id);
    connectSignalR(id);
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
  connectSignalR,
  disconnectSignalR,
};