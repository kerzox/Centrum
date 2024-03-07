import { useContext, useEffect, useRef, useState } from "react";
import styles from "../styles/filemanager.module.css";
import Button from "~/components/button";
import { WebSocketContext } from "~/context/socket";
import { Account, AccountContext } from "~/context/account";
import { json } from "@remix-run/node";
import OpenFile from "~/components/file";
import ContextMenu from "~/components/contextMenu";
import Trash from "~/components/icons/trash";
import { useNavigate } from "@remix-run/react";

const supportedFiles: string[] = ["txt", "properties", "json", "log"];

class File {
  name: string;
  date?: Date;
  size?: string;
  content?: string;
  constructor(name: string, date?: Date, size?: string, content?: string) {
    this.name = name;
    this.content = content;
    this.date = date;
    this.size = size;
  }
}

class Directory {
  parent?: Directory;
  id: number;
  dirname: string;
  children: Directory[] = [];
  hidden: boolean = true;
  depth: number;
  path!: string;

  constructor(dirname: string, depth: number, id: number) {
    this.dirname = dirname;
    this.depth = depth;
    this.id = id;
  }
}

export default function FileManager() {
  const nav = useNavigate();
  const socketContext = useContext(WebSocketContext);
  const accountContext = useContext(AccountContext);
  const [directories, setDirectories] = useState<Directory>(
    new Directory("", 0, 0)
  );

  const wrapperRef = useRef(null);
  const explorerRef = useRef(null);
  const fileListRef = useRef(null);

  const [clipboard, setClipboard] = useState<any>(undefined);

  const [files, setFiles] = useState<File[]>([]);
  const [activeButton, setButton] = useState(-1);
  const [relativePath, setRelativePath] = useState("");
  const [fileContent, setFileContent] = useState({
    txt: undefined,
    path: "",
    name: "",
  });

  const [contextMenuState, setContextInformation] = useState<any>();
  const [contextItems, setContextItems] = useState<any>();

  useEffect(() => {
    if (accountContext.instanceLastOn != undefined) {
      createFileExplorer();
    }
  }, []);

  const createFileExplorer = (): Directory => {
    socketContext?.socket.emit(
      "file_manager",
      {
        instanceName: accountContext.instanceLastOn,
        relativePath: "",
      },
      (response) => {
        let ID = 0;

        const func = (path: string, json: JsonDir, increment: number) => {
          ID++;
          let parent = new Directory(json.name, increment, ID);
          parent.path = path === "" ? json.name : path + "/" + json.name;
          if (increment == 0) {
            parent.hidden = false;
          }
          increment = increment + 1;

          json.children.forEach((child: JsonDir) => {
            let dir = func(parent.path, child, increment);
            dir.parent = parent;
            parent.children.push(dir);
          });
          return parent;
        };

        interface JsonDir {
          name: string;
          children: any[];
        }

        if (accountContext.instanceLastOn != undefined) {
          const parsed: JsonDir = JSON.parse(response.content);

          let increment = 0;
          let root = func("", parsed, increment);

          setDirectories(root);
          return root;
        }
      }
    );
    return null;
  };

  const active = (directory: Directory) => {
    let classes = styles.btn;
    if (directory.path.includes(relativePath)) {
      classes += " " + styles.inSelection + " ";
    }

    if (activeButton === directory.id) {
      classes += styles.active;
    }

    return classes;
  };

  const getDirectories = (starting: Directory, find?: Directory) => {
    const visited = new Set();
    const queue = [starting];
    const result = [];

    visited.add(starting);

    while (queue.length !== 0) {
      const currentNode = queue.shift();
      if (currentNode?.id === find?.id) return [currentNode];
      result.push(currentNode);

      if (currentNode?.children != null) {
        for (const child of currentNode?.children) {
          if (!visited.has(child)) {
            visited.add(child);
            queue.push(child);
          }
        }
      }
    }
    return result;
  };

  const hideAllDirectories = (starting: Directory) => {
    starting.hidden = true;
    starting.children.forEach((child) => {
      hideAllDirectories(child);
    });
  };

  const expandToParent = (starting: Directory) => {
    starting.hidden = false;
    starting.children.forEach((child) => {
      child.hidden = false;
    });
    if (starting.parent != undefined) {
      expandToParent(starting.parent);
    }
  };

  const DirectoryComponent = ({
    directory,
    spacing,
  }: {
    directory: Directory;
    spacing: number;
  }) => {
    return (
      <div>
        {directory.hidden ? (
          <></>
        ) : (
          <>
            <div key={directory.id + ":wrapper"}>
              <Button
                key={directory.id + ":button"}
                onContextMenu={(e: any) =>
                  openFileContextMenuOnDirectory(directory, e)
                }
                className={active(directory)}
                onClick={(e: any) => {
                  setButton(directory.id);
                  setRelativePath(directory.path);
                  hideAllDirectories(directories);
                  expandToParent(directory);

                  setFiles([]);
                  if (accountContext.instanceLastOn != undefined) {
                    socketContext?.socket.emitAndListenToSpecialKey(
                      "file_manager_files",
                      `file_manager_files_${accountContext.instanceLastOn}`,
                      {
                        instanceName: accountContext.instanceLastOn,
                        relativePath: directory.path
                          .replace(accountContext.instanceLastOn, "")
                          .substring(1),
                      },
                      (response) => {
                        let files = [];
                        for (let i = 0; i < response.content.length; i++) {
                          files.push(new File(response.content[i]));
                        }
                        setFiles(files);
                      }
                    );
                  }
                }}
              >
                <p
                  key={directory.id + ":text"}
                  style={{
                    width: "100%",
                    textAlign: "left",
                    marginLeft: `${spacing}px`,
                  }}
                >
                  {directory.dirname}
                </p>
              </Button>
              {directory.children && (
                <div className={styles.child} key={directory.id + ":child"}>
                  {directory.children.map((child) => (
                    <DirectoryComponent
                      key={directory.id + child.dirname}
                      directory={child}
                      spacing={spacing + 15}
                    />
                  ))}
                </div>
              )}
            </div>
          </>
        )}
      </div>
    );
  };

  // const openFileContextMenuExplorer = (name: string, e: any) => {
  //   e.preventDefault();
  //   setContextInformation({
  //     position: {
  //       x: e.clientX,
  //       y: e.clientY,
  //     },
  //     div: e.target,
  //   });

  //   setContextItems({
  //     header: (
  //       <>
  //         <Button className={styles.buttonFont} style={{ flex: "75%" }}>
  //           {name}
  //         </Button>
  //       </>
  //     ),
  //     main: (
  //       <>
  //         <Button
  //           className={styles.buttonFont}
  //           onClick={() => openPrompt("create", "directory", "", "What do you want it to be called")}
  //           style={{ flex: "25%", justifyContent: "left" }}
  //         >
  //           Create Directory
  //         </Button>
  //       </>
  //     ),
  //   });
  // };

  const openFileContextMenuOnDirectory = (dir: Directory, e: any) => {
    e.preventDefault();
    setContextInformation({
      position: {
        x: e.clientX,
        y: e.clientY,
      },
      div: e.target,
    });

    setContextItems({
      header: (
        <>
          <Button className={styles.buttonFont} style={{ flex: "75%" }}>
            {dir.dirname}
          </Button>
        </>
      ),
      main: (
        <>
          <Button
            onClick={() =>
              openPrompt(
                "rename",
                "directory",
                dir.path
                  .replace(accountContext.instanceLastOn, "")
                  .substring(1),
                "Rename to",
                dir
              )
            }
            className={styles.buttonFont}
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Rename
          </Button>
          <Button
            onClick={() => copyDirectory(dir)}
            className={styles.buttonFont}
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Copy
          </Button>
          <Button
            onClick={() => paste("directory")}
            className={styles.buttonFont}
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Paste
          </Button>
          <Button
            className={styles.buttonFont}
            onClick={() =>
              openPrompt(
                "create",
                "directory",
                dir.path
                  .replace(accountContext.instanceLastOn, "")
                  .substring(1),
                "What do you want it to be called",
                dir
              )
            }
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Create Directory
          </Button>
          <Button
            className={styles.buttonFont}
            onClick={() =>
              sendConfirm(
                "delete",
                "directory",
                dir.path
                  .replace(accountContext.instanceLastOn, "")
                  .substring(1),
                "Are you sure you want to delete this directory"
              )
            }
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Delete Directory
          </Button>
        </>
      ),
    });
  };

  const openFileContextMenuOnFileExplorer = (name: string, e: any) => {
    if (activeButton == -1) return;
    e.preventDefault();
    setContextInformation({
      position: {
        x: e.clientX,
        y: e.clientY,
      },
      div: e.target,
    });

    setContextItems({
      header: (
        <>
          <Button className={styles.buttonFont} style={{ flex: "75%" }}>
            {name}
          </Button>
        </>
      ),
      main: (
        <>
          <Button
            onClick={() =>
              openPrompt(
                "create",
                "file",
                relativePath
                  .replace(accountContext.instanceLastOn, "")
                  .substring(1),
                "What do you want it to be called"
              )
            }
            className={styles.buttonFont}
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Create File
          </Button>
        </>
      ),
    });
  };

  const openFileContextMenuOnFile = (file: File, e: any) => {
    e.preventDefault();
    setContextInformation({
      position: {
        x: e.clientX,
        y: e.clientY,
      },
      div: e.target,
    });

    setContextItems({
      header: (
        <>
          <Button className={styles.buttonFont} style={{ flex: "75%" }}>
            {file.name}
          </Button>
        </>
      ),
      main: (
        <>
          <Button
            onClick={() => {
              openFile(file);
            }}
            className={styles.buttonFont}
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Open
          </Button>
          <Button
            onClick={() => {
              openPrompt(
                "rename",
                "file",
                relativePath
                  .replace(accountContext.instanceLastOn, "")
                  .substring(1) + file.name,
                "What do you want it to be called"
              );
            }}
            className={styles.buttonFont}
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Rename
          </Button>
          <Button
            className={styles.buttonFont}
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Copy
          </Button>
          <Button
            className={styles.buttonFont}
            style={{ flex: "25%", justifyContent: "left" }}
          >
            Paste
          </Button>
        </>
      ),
    });
  };

  const sendConfirm = (
    interaction: string,
    type: string,
    relativePath: string,
    title: string
  ) => {
    const input = confirm(title);

    if (input) {
      socketContext?.socket.emit(
        "file_interaction",
        {
          instanceName: accountContext.instanceLastOn,
          relativePath: relativePath,
          type: type,
          interaction,
          content: "",
        },
        (response) => {
          if (response.error) {
            alert(response.error);
            return;
          }

          createFileExplorer();
          // setTimeout(() => , 1000);
        }
      );
    }

    setContextInformation(undefined);
  };

  const openPrompt = (
    interaction: string,
    type: string,
    relativePath: string,
    title: string,
    object?: any
  ) => {
    const input = window.prompt(title);

    if (input != undefined) {
      socketContext?.socket.emit(
        "file_interaction",
        {
          instanceName: accountContext.instanceLastOn,
          relativePath: relativePath,
          type: type,
          interaction,
          content: input,
        },
        (response) => {
          if (response.error) {
            alert(response.error);
            return;
          }

          if (type === "directory") {
            createFileExplorer();
          }
        }
      );
    }

    setContextInformation(undefined);
  };

  const copyDirectory = (dir: Directory) => {
    /**
     * So navigator clipboard doesn't work with firefox if you want to read it which is a bummer so react state it is!!
     */

    setClipboard({
      path: dir.path.replace(accountContext.instanceLastOn, "").substring(1),
      name: dir.dirname,
    });
  };

  const paste = (type: string) => {
    console.log(clipboard);

    if (clipboard == undefined) return;
    socketContext?.socket.emit(
      "file_interaction",
      {
        instanceName: accountContext.instanceLastOn,
        relativePath: relativePath
          .replace(accountContext.instanceLastOn, "")
          .substring(1),
        type: type,
        interaction: "paste",
        content: JSON.stringify(clipboard),
      },
      (response) => {
        if (response.error) {
          alert(response.error);
          return;
        }
        createFileExplorer();
      }
    );
  };

  const openFile = (file: File) => {
    const extension = file.name.split(".");
    if (accountContext.instanceLastOn == undefined) return;
    const path = relativePath
      .replace(accountContext.instanceLastOn, "")
      .substring(1);

    if (supportedFiles.includes(extension[1])) {
      socketContext?.socket.emitAndListenToSpecialKey(
        "get_file",
        `get_file_${accountContext.instanceLastOn}`,
        {
          instanceName: accountContext.instanceLastOn,
          relativePath: path === "" ? file.name : path + "/" + file.name,
        },
        (response) => {
          setFileContent({
            txt: response.content,
            path: path === "" ? file.name : path + "/" + file.name,
            name: file.name,
          });
        }
      );
    } else {
      alert("Unable to open file");
    }
  };

  return (
    <div
      ref={wrapperRef}
      style={{ width: "100%", padding: "24px", color: "white" }}
    >
      <ContextMenu
        contextItems={contextItems}
        contextState={contextMenuState}
        clear={() => {
          setContextInformation(undefined);
        }}
      ></ContextMenu>
      <OpenFile content={{ fileContent, setFileContent }} />
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: "24px",
          height: "100%",
        }}
      >
        <div className={styles.pathText}>
          <p className={`headerText`}>{relativePath}</p>
        </div>
        <div className={styles.container}>
          <div
            ref={explorerRef}
            className={styles.side}
            // onContextMenu={(e) => {
            //   if (explorerRef.current === e.target) {
            //     console.log(e);
            //     openFileContextMenuExplorer("File Explorer", e);
            //   }
            // }}
          >
            <p className="headerText" style={{ textAlign: "center" }}>
              File Explorer
            </p>
            <div className={styles.root}>
              <DirectoryComponent directory={directories} spacing={15} />
            </div>
          </div>
          <div
            ref={fileListRef}
            className={styles.main}
            onContextMenu={(e) => {
              if (fileListRef.current === e.target) {
                console.log(e);
                openFileContextMenuOnFileExplorer("File Explorer", e);
              }
            }}
          >
            <table className={styles.table}>
              <thead>
                <tr className={styles.tableheader}>
                  <th>Name</th>
                  <th>Date</th>
                  <th>Size</th>
                </tr>
              </thead>
              <tbody style={{ overflow: "auto" }}>
                {files.map((file, i) => (
                  <tr
                    className={styles.tableRow}
                    key={`${file}:${i}`}
                    onContextMenu={(e) => openFileContextMenuOnFile(file, e)}
                    onClick={() => {
                      openFile(file);
                    }}
                  >
                    <td>{file.name}</td>
                    <td>{file.date?.toLocaleString()}</td>
                    <td>{file.size}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
