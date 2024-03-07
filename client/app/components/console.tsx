import React, { useContext, useEffect, useRef, useState } from "react";
import { Link, useLocation } from "@remix-run/react";

import styles from "../styles/console.module.css";
import Input from "./input";
import { WebSocketContext } from "~/context/socket";
import { eventHandler } from "~/context/socket_events";

const Console = (props: any) => {
  const location = useLocation();
  const [commandInput, setCommandInput] = useState<string>("");
  const [log, updateConsoleLog] = useState<string[]>([]);
  const consoleRef = useRef<HTMLDivElement>(null);

  const contextValue = useContext(WebSocketContext);

  const sendCommandToServer = (event: any) => {
    event.preventDefault();
    if (contextValue == undefined) return;
    if (contextValue.socket.socket.readyState == 0) return;
    contextValue.socket.emit("instance_command", {
      instanceName: props.instanceName,
      cmd: commandInput,
    });
    setCommandInput("");
  };

  // useEffect(() => {}, []);

  useEffect(() => {
    contextValue?.socket.emitAndListenToSpecialKey(
      "console_latest",
      `console_${props.instanceName}_latest`,
      { instanceName: props.instanceName },
      (response) => {
        updateConsoleLog([]);
        updateConsoleLog(response.log);
      }
    );

    eventHandler.unique(
      `console_${props.instanceName}`,
      `${props.instanceName}`,
      (data: { output: string }) => {
        updateConsoleLog((prevText: any) => [...prevText, data.output]);
      }
    );

    // internal event
    eventHandler.unique("clean_console", `${props.instanceName}`, () =>
      updateConsoleLog([])
    );
  }, []);

  useEffect(() => {
    // Scroll to the bottom of the div when the component mounts or when the content changes
    if (consoleRef.current) {
      consoleRef.current.scrollTo({
        top: consoleRef.current.scrollHeight,
        behavior: "smooth",
      });
    }
  }, [log]);

  const colourCoded = (t: any) => {
    if (t != null) {
      if (t.match(/\[.*?\/ERROR\]/)) {
        return styles.error;
      }
      if (t.match(/\[.*?\/WARN\]/)) {
        return styles.warning;
      }
      if (t.match(/\[.*?\/FATAL\]/)) {
        return styles.fatal;
      }
    }
    return "";
  };

  return (
    <div className={styles.container}>
      <div ref={consoleRef} className={styles.log}>
        {log.map((t: any, i: number) => (
          <p
            key={i}
            style={{ padding: 0, margin: 0, fontFamily: "Consolas" }}
            className={colourCoded(t)}
          >
            {t}
          </p>
        ))}
      </div>
      <div className={styles.footer}>
        <form onSubmit={sendCommandToServer}>
          <Input
            onChange={setCommandInput}
            value={commandInput}
            style={{
              fontFamily: "Consolas",
              fontSize: "12px",
              color: "white",
              border: "none",
              backgroundColor: "transparent",
            }}
            placeholder="Enter Command"
          ></Input>
        </form>
      </div>
    </div>
  );
};

export default Console;
