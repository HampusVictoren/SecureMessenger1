export type User = {
  id: string;
  username: string;
  displayName: string;
  avatarUrl?: string;
};

export type Chat = {
  id: string;
  title?: string;
  participants: Array<Pick<User, "id" | "username" | "displayName" | "avatarUrl">>;
  lastMessage?: Message;
  unreadCount?: number;
  updatedAt: string; // ISO date
};

export type Message = {
  id: string;
  chatId: string;
  senderId: string;
  content: string;
  sentAt: string; // ISO date
  status?: "sending" | "sent" | "delivered" | "read" | "failed";
};

export type CreateChatRequest = {
  username: string;
};

export type CreateChatResponse = {
  chat: Chat;
};

export type SendMessageRequest = {
  content: string;
};

export type SendMessageResponse = {
  message: Message;
};