import * as signalR from "@microsoft/signalr";

let connection: signalR.HubConnection | null = null;

export function getChatHub(): signalR.HubConnection {
  if (connection) return connection;

  connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", { withCredentials: true })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
}

export async function ensureConnected(): Promise<signalR.HubConnection> {
  const hub = getChatHub();
  if (hub.state === signalR.HubConnectionState.Disconnected) {
    await hub.start();
  }
  return hub;
}