import Input from "~/components/input";
import styles from "../styles/instances.module.css";
import Button from "~/components/button";
import { useContext, useEffect, useRef, useState } from "react";
import { WebSocketContext } from "~/context/socket";
import { AccountContext } from "~/context/account";
import { json, useNavigate } from "@remix-run/react";
import { M } from "node_modules/vite/dist/node/types.d-jgA8ss1A";
import { eventHandler } from "~/context/socket_events";
import Trash from "~/components/icons/trash";
import ChevronLeft from "~/components/icons/chevron_left";
import Bolt from "~/components/icons/bolt";
import Alert from "~/components/alert";

interface InstanceLink {
  name: string;
}

interface Instance {
  instanceName: string;
  serverStatus: string;
}

export default function Instances() {
  const socketContext = useContext(WebSocketContext);
  const nav = useNavigate();
  const mainRef = useRef<HTMLDivElement>(null);
  const instanceRef = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const alertRef = useRef<HTMLDivElement>(null);

  const contextValue = useContext(WebSocketContext);
  const accountValue = useContext(AccountContext);

  const [machineMemory, setMachineMemory] = useState(1024);

  const [buttonVisible, toggleCreateButton] = useState(true);
  const [name, setInstanceName] = useState("Minecraft");
  const [players, setPlayerCount] = useState("10");
  const [port, setPort] = useState("25565");
  const [mem, changeMem] = useState(1024);
  const [jarfile, setJarFile] = useState("server.jar");

  const [alertData, setAlertData] = useState<any>({});

  const [list, setInstanceList] = useState<Instance[]>([]);

  const sendAlert = (
    title: string,
    body?: string,
    type?: string,
    func?: any
  ) => {
    setAlertData({
      title,
      body,
      type,
      clear: () => setAlertData({}),
    });
  };

  const openInstance = (item: Instance) => {
    const ret = confirm("Do you want to open this instance");
    if (ret == true) {
      contextValue?.socket.emit(
        "choose_instance",
        { instanceName: item.instanceName },
        (response) => {
          if (response?.ok) {
            accountValue.instanceLastOn = item.instanceName;
            nav("/dashboard");
          } else {
            alert(response.message);
          }
        }
      );
    }
  };

  const deleteInstance = (item: Instance) => {
    const ret = confirm("You're about to delete this instance are you sure?");
    if (ret == true) {
      contextValue?.socket.emit(
        "remove_instance",
        {
          instanceName: item.instanceName,
        },
        (data: any) => {
          if (data.error) {
            sendAlert("Alert", data.error, "bg-error");
          }
        }
      );
    }
  };

  const createInstance = () => {
    const parsedPlayers = parseInt(players);
    const parsedPort = parseInt(port);

    if (isNaN(parsedPlayers)) {
      sendAlert("Alert", `Players must be a number`, "bg-error");
      return;
    }

    if (isNaN(parsedPort)) {
      sendAlert("Alert", `Port must be a number`, "bg-error");
      return;
    }

    socketContext?.socket.emit(
      "create_instance",
      {
        instanceName: name,
        playerSlots: parsedPlayers,
        port: parsedPort,
        memory: mem,
      },
      (response: any) => {
        if (response.error) {
          sendAlert("Alert", response.error, "bg-error");
        } else {
          sendAlert("Alert", response.message, "bg-success");
        }
      }
    );
  };

  useEffect(() => {
    /*

    send authentication inside the data

    */

    contextValue?.socket.emit(
      "machine_information",
      {},
      ({ memory }: { memory: number }) => {
        setMachineMemory(memory);
      }
    );

    if (accountValue.instanceLastOn != undefined) {
      contextValue?.socket.emit("leave_instance", {
        instanceName: accountValue.instanceLastOn,
      });

      // remove ourselves from unique events if we had any
      eventHandler.wipeUniques(accountValue.instanceLastOn);
      console.log(eventHandler);
    }

    contextValue?.socket.emit("grab_instances", {});

    eventHandler.unique(
      "grab_instances",
      "instance_page",
      ({ instances }: { instances: any }) => {
        const parsed: Instance[] = JSON.parse(instances);
        setInstanceList(parsed);
      }
    );
  }, []);

  return (
    <div style={{ width: "100%", padding: "24px", color: "white" }}>
      <div className={`center_container col`}>
        <Alert
          childRef={alertRef}
          className={styles.alert}
          alert={alertData}
        ></Alert>
        <div ref={mainRef} className={styles.main}>
          <div className={styles.side} ref={listRef}>
            <div className={styles.side_main}>
              <div style={{ paddingLeft: "12px" }}>
                <p className="headerText">Servers</p>
              </div>
              {list.length === 0 ? (
                <p className={styles.innerText}>No servers found</p>
              ) : (
                list.map((item: Instance, i: number) => (
                  <div
                    key={item.instanceName + "div"}
                    className={styles.instanceDiv}
                    style={{
                      width: "100%",
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                    }}
                  >
                    <Button
                      onClick={() => openInstance(item)}
                      key={item.instanceName + ":button"}
                      className={styles.button}
                      style={{
                        padding: "5px 0 5px 12px",
                        width: "100%",
                        height: "100%",
                      }}
                    >
                      {item.instanceName}
                    </Button>
                    <Bolt
                      fill={
                        item.serverStatus === "running"
                          ? "rgb(255, 249, 83)"
                          : item.serverStatus === "initalizing"
                          ? "rgb(83, 109, 255)"
                          : "rgba(255, 83, 83, 1)"
                      }
                    ></Bolt>
                    <Button
                      onClick={() => deleteInstance(item)}
                      key={item.instanceName + ":remove_button"}
                      style={{ display: "flex", alignItems: "center" }}
                      className={styles.button}
                    >
                      <Trash />
                    </Button>
                  </div>
                ))
              )}
            </div>
            <div className={styles.footer}>
              {buttonVisible ? (
                <Button
                  onClick={() => {
                    toggleCreateButton(false);
                    mainRef.current?.classList.remove(styles.shrinkMain);
                    mainRef.current?.classList.add(styles.growMain);
                    instanceRef.current?.classList.add(styles.growInstance);
                    listRef.current?.classList.add(styles.changeSide);
                  }}
                  className={styles.newInstance}
                >
                  New Server
                </Button>
              ) : (
                <></>
              )}
            </div>
          </div>
          <div ref={instanceRef} className={styles.instances}>
            <div className={styles.instanceMain}>
              <Button
                onClick={() => {
                  toggleCreateButton(true);
                  mainRef.current?.classList.remove(styles.growMain);
                  instanceRef.current?.classList.remove(styles.growInstance);
                  listRef.current?.classList.remove(styles.changeSide);
                  mainRef.current?.classList.add(styles.shrinkMain);
                }}
                className={styles.backChevron}
              >
                <div
                  style={{
                    marginLeft: "-20px",
                    display: "flex",
                    alignItems: "center",
                  }}
                >
                  <ChevronLeft></ChevronLeft>
                  <p className={styles.innerText}>Return</p>
                </div>
              </Button>
              <p>Name of the Instance</p>
              <Input
                className={styles.input}
                value={name}
                onChange={setInstanceName}
                placeholder={"Minecraft"}
              ></Input>
              <p>How many players</p>
              <Input
                className={styles.input}
                value={players}
                onChange={(text: any) => {
                  if (text == "") {
                    setPlayerCount("");
                  } else if (!isNaN(Number(text))) {
                    setPlayerCount(text);
                  }
                }}
                placeholder={"10"}
              ></Input>
              <p>Port</p>
              <Input
                className={styles.input}
                value={port}
                onChange={(text: any) => {
                  if (text == "") {
                    setPort("");
                  } else if (!isNaN(Number(text))) {
                    setPort(text);
                  }
                }}
                placeholder={"25565"}
              ></Input>
              <p>Memory: {mem / 1024} gbs</p>
              <input
                type="range"
                style={{ width: "100%" }}
                onChange={(e: any) => {
                  changeMem(parseInt(e.target.value));
                }}
                title={mem + ""}
                min={1024}
                max={machineMemory}
                step={1024 / 8}
                value={mem}
              ></input>
              <p>Server jarfile</p>
              <Input
                className={styles.input}
                value={jarfile}
                onChange={setJarFile}
                placeholder={"server.jar"}
              ></Input>
            </div>
            <div style={{}}>
              <Button
                onClick={() => {
                  mainRef.current?.classList.add(styles.growMain);
                  instanceRef.current?.classList.add(styles.growInstance);
                  listRef.current?.classList.add(styles.changeSide);
                  createInstance();
                }}
                className={styles.newInstance}
              >
                Create
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
