import { createContext } from "react";

// expand this

export class GlobalState {
  instanceLastOn?: string;
}

export let GlobalContext = createContext<GlobalState>(new GlobalState());
