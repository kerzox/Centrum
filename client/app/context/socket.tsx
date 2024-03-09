import { createContext } from "react";
import { eventHandler } from "./socket_events";
import { Account } from "~/root";

class ClientSocket {
  socket: WebSocket;

  constructor(url: string) {
    this.socket = new WebSocket("ws://localhost:3000");
  }

  emit = (key: string, data?: any, response?: (data: any) => void) => {
    if (this.socket == null) return;
    if (
      this.socket.readyState == WebSocket.CLOSED ||
      this.socket.readyState == WebSocket.CLOSING ||
      this.socket.readyState == WebSocket.CONNECTING
    )
      return;

    if (typeof window !== "undefined") {
      const session = sessionStorage.getItem("user");
      if (session != undefined) {
        const acc = JSON.parse(session) as Account;

        if (response != undefined) {
          data.callback = 1;
        }

        data.authorisation = acc.token;

        this.socket.send(
          JSON.stringify({
            eventKey: key,
            data: data == undefined ? key : data,
          })
        );

        if (response != undefined) {
          eventHandler.once(key, response);
        }

        return;
      }
    }

    if (response != undefined) {
      data.callback = 1;
    }

    this.socket.send(
      JSON.stringify({ eventKey: key, data: data == undefined ? key : data })
    );

    if (response != undefined) {
      eventHandler.once(key, response);
    }
  };

  emitAndListenToSpecialKey = (
    serverKey: string,
    responseKey: string,
    data?: any,
    response?: (data: any) => void
  ) => {
    this.socket.send(
      JSON.stringify({
        eventKey: serverKey,
        data: data == undefined ? serverKey : data,
      })
    );
    if (response != undefined) {
      eventHandler.once(responseKey, response);
    }
  };

  close = () => {
    this.socket.close();
  };
}

export const connect = () => {
  let socket = new ClientSocket("ws://127.0.0.1:3000");
  return socket;
};

export interface UserContext {
  socket: ClientSocket;
}

export let WebSocketContext = createContext<UserContext | undefined>(undefined);
