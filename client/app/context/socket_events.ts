interface EventCallback {
  (data: any): void;
}

class EventEmitter {
  private events: { [key: string]: EventCallback[] } = {};
  private uniqueEvents: {
    [key: string]: { id: string; evt: EventCallback }[];
  } = {};
  private onceListeners: { [key: string]: EventCallback[] } = {};

  on(eventName: string, callback: EventCallback): void {
    if (!this.events[eventName]) {
      this.events[eventName] = [];
    }

    this.events[eventName].push(callback);
  }

  unique(eventName: string, id: string, callback: EventCallback): void {
    if (!this.uniqueEvents[eventName]) {
      this.uniqueEvents[eventName] = [];
    }
    this.uniqueEvents[eventName] = this.uniqueEvents[eventName].filter(
      (evt) => evt.id !== id
    );
    this.uniqueEvents[eventName].push({ id: id, evt: callback });
  }

  once(eventName: string, callback: EventCallback): void {
    if (!this.onceListeners[eventName]) {
      this.onceListeners[eventName] = [];
    }

    this.onceListeners[eventName].push(callback);
  }

  leave(eventName: string) {
    if (this.uniqueEvents[eventName]) {
      delete this.uniqueEvents[eventName];
    }

    if (this.events[eventName]) {
      delete this.events[eventName];
    }

    if (this.onceListeners[eventName]) {
      delete this.onceListeners[eventName];
    }
  }

  wipeUniques(id: string) {
    for (const key in this.uniqueEvents) {
      if (this.uniqueEvents.hasOwnProperty(key)) {
        const value = this.uniqueEvents[key];
        this.uniqueEvents[key] = value.filter((e) => e.id !== id);
      }
    }
  }

  emit(eventName: string, data: any): void {
    if (this.events[eventName]) {
      this.events[eventName].forEach((callback) => {
        callback(data);
      });
    }
    if (this.uniqueEvents[eventName]) {
      this.uniqueEvents[eventName].forEach((callback) => {
        callback.evt(data);
      });
    }
    if (this.onceListeners[eventName]) {
      this.onceListeners[eventName].forEach((callback) => {
        callback(data);
      });
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
