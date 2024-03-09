interface EventCallback {
  (data: any): void;
}

class EventEmitter {
  private multi_events: { [key: string]: EventCallback[] } = {};

  private events: { [key: string]: EventCallback } = {};
  private onceListeners: { [key: string]: EventCallback } = {};

  on(eventName: string, callback: EventCallback): void {
    this.events[eventName] = callback;
  }

  multi(eventName: string, callback: EventCallback): void {
    if (!this.multi_events[eventName]) {
      this.multi_events[eventName] = [];
    }
    this.multi_events[eventName].push(callback);
  }

  once(eventName: string, callback: EventCallback): void {
    this.onceListeners[eventName] = callback;
  }

  leave(eventName: string) {
    if (this.events[eventName]) {
      delete this.events[eventName];
    }

    if (this.onceListeners[eventName]) {
      delete this.onceListeners[eventName];
    }
  }

  emit(eventName: string, data: any): void {
    if (this.multi_events[eventName] != null) {
      this.multi_events[eventName].forEach((call) => call(data));
    }
    if (this.events[eventName] != null) {
      this.events[eventName](data);
    }
    if (this.onceListeners[eventName] != null) {
      this.onceListeners[eventName](data);
      delete this.onceListeners[eventName];
    }
  }
}

class SocketEvent extends EventEmitter {
  constructor() {
    super();
  }
}

export const eventHandler = new SocketEvent();
