import { createContext } from "react";

// expand this

export class Account {
  username!: string;
  loggedin: boolean = false;
  instanceLastOn?: string;
  token!: string;

  constructor() {
    this.username = "";
  }
}

export let AccountContext = createContext<Account>(new Account());
