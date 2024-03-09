import { useContext, useEffect, useState } from "react";
import { WebSocketContext } from "~/context/socket";
import { GlobalContext } from "~/context/global_state";

import styles from "../styles/file.module.css";
import Button from "./button";

export default function OpenFile({
  content,
}: {
  content: {
    fileContent: { txt: string; path: string; name: string };
    setFileContent: any;
  };
}) {
  const contextValue = useContext(WebSocketContext);
  const accountValue = useContext(GlobalContext);

  const [visible, toggle] = useState(false);
  const [text, changeText] = useState(content.fileContent.txt);

  useEffect(() => {
    if (content.fileContent.txt != undefined) {
      changeText(content.fileContent.txt);
      toggle(true);
    } else {
      toggle(false);
    }
  }, [content]);

  return visible ? (
    <div className={styles.wrapper}>
      <div className={styles.container}>
        <div className={styles.header}>
          <p className="headerText">{content.fileContent.name}</p>
          <Button
            className={styles.button2}
            onClick={() => {
              content.setFileContent({
                txt: undefined,
                path: "",
                name: "",
              });
            }}
          >
            <p style={{ fontSize: "48px", padding: 0, margin: 0 }}>&times;</p>
          </Button>
        </div>
        <textarea
          className={styles.textInput}
          value={text}
          onChange={(e) => changeText(e.target.value)}
        />
        <Button
          className={styles.button}
          onClick={() => {
            contextValue?.socket.emit(
              "save_file",
              {
                instanceName: accountValue.instanceLastOn,
                relativePath: content.fileContent.path,
                content: text,
              },
              (response) => {
                alert(response.message);
                content.setFileContent({
                  txt: undefined,
                  path: "",
                  name: "",
                });
              }
            );
          }}
        >
          Save
        </Button>
      </div>
    </div>
  ) : (
    <></>
  );
}
