import { useContext, useEffect, useState } from "react";
import { GlobalContext } from "~/context/global_state";
import { WebSocketContext } from "~/context/socket";
import { eventHandler } from "~/context/socket_events";

import styles from "../styles/users.module.css";
import Button from "~/components/button";
import Restart from "~/components/icons/restart";
import Expand from "~/components/icons/expand";
import server from "@geckos.io/server";

class Player {
  id: string;
  name: string;
  constructor(id: string, name: string) {
    this.id = id;
    this.name = name;
  }
}

interface PlayerList {
  max: number;
  online: number;
  sample: Player[];
}

interface Version {
  name: string;
  protocol: number;
}

interface SPLResponse {
  description: string;
  enforcesSecureChat: boolean;
  players: PlayerList;
  version: Version;
}

export default function Settings() {
  const contextValue = useContext(WebSocketContext);
  const accountContext = useContext(GlobalContext);
  const [serverPingResponse, setSPLResponse] = useState<SPLResponse>();
  const [players, setPlayers] = useState<any[]>();
  const [bannedPlayers, setBannedPlayers] = useState<any[]>();
  const [information, setPlayerInformation] = useState<any>();
  const [instanceInformation, setInstanceInformation] = useState<any>();
  const [bannedPlayerInformation, setBannedPlayerInformation] = useState<any>();

  const [isBannedTab, setBannedTab] = useState<boolean>(false);

  const createPlayerCards = async (players: any[]) => {
    const playerElements: any[] = [];
    if (players == undefined) return;

    for (let i = 0; i < players.length; i++) {
      const player = players[i];

      try {
        const response = await fetch(
          `https://mc-heads.net/avatar/${player.id}/55`
        );
        const data = await response;

        let coloured = isOpped(player.id)
          ? " " + styles.operatorColour
          : " bg-45";

        playerElements.push(
          <Button
            key={player.id + ":online"}
            className={styles.userCard + coloured}
            onClick={() => playerClicked(player)}
          >
            <div className={styles.userImage}>
              <img src={`${data.url}`} alt="N/A" />
            </div>
            <div className={styles.userMain}>
              <p style={{ padding: 0, margin: 0 }}>{player.name}</p>
              <p
                style={{
                  color: "gray",
                  padding: 0,
                  margin: 0,
                  fontSize: ".75em",
                }}
              >
                {player.id}
              </p>
            </div>
          </Button>
        );
      } catch (error) {
        console.error("Error:", error);
      }
    }

    setPlayers(playerElements);
  };

  const createBannedPlayerCards = async (players: any[]) => {
    const playerElements: any[] = [];
    if (players == undefined) return;

    for (let i = 0; i < players.length; i++) {
      const player = players[i];

      try {
        const response = await fetch(
          `https://mc-heads.net/avatar/${player.id}/55`
        );
        const data = await response;

        let coloured = isOpped(player.uuid)
          ? " " + styles.operatorColour
          : " bg-45";

        playerElements.push(
          <Button
            key={player.uuid + ":banned"}
            className={styles.userCard + coloured}
            onClick={() => playerClicked(player)}
          >
            <div className={styles.userImage}>
              <img src={`${data.url}`} alt="N/A" />
            </div>
            <div className={styles.userMain}>
              <p style={{ padding: 0, margin: 0 }}>{player.name}</p>
              <div
                style={{
                  color: "gray",
                  padding: 0,
                  margin: 0,
                  fontSize: ".85em",
                  display: "flex",
                  flexDirection: "column",
                }}
              >
                <p
                  style={{
                    color: "gray",
                    padding: 0,
                    margin: 0,
                    fontSize: ".75em",
                  }}
                >
                  {player.uuid}
                </p>
                <p
                  style={{
                    color: "gray",
                    padding: 0,
                    margin: 0,
                    fontSize: ".75em",
                  }}
                >
                  {player.reason}
                </p>
                <p
                  style={{
                    color: "gray",
                    padding: 0,
                    margin: 0,
                    fontSize: ".75em",
                  }}
                >
                  from: {player.source}
                </p>
              </div>
            </div>
          </Button>
        );
      } catch (error) {
        console.error("Error:", error);
      }
    }

    setBannedPlayers(playerElements);
  };

  const playerClicked = (player: Player) => {
    setPlayerInformation({
      player: player,
    });
  };

  useEffect(() => {
    if (accountContext.instanceLastOn != undefined) {
      contextValue?.socket.emit("get_spl", {
        instanceName: accountContext.instanceLastOn,
      });

      contextValue?.socket.emit("instance_information", {
        instanceName: accountContext.instanceLastOn,
        full_information: true,
      });

      eventHandler.on("get_spl", (output) => {
        console.log(output);
        if (output.error) {
          return;
        }
        try {
          const json: SPLResponse = JSON.parse(output);
          console.log(json);
          setSPLResponse(json);
        } catch (exp) {
          console.log(exp);
        }
      });

      eventHandler.on("instance_information", (data: any) => {
        console.log(data);
        setInstanceInformation(JSON.parse(data.ops));
        setBannedPlayerInformation(JSON.parse(data.bannedPlayers));
      });
    }
  }, []);

  useEffect(() => {
    console.log(instanceInformation);
    if (
      instanceInformation != undefined &&
      serverPingResponse != undefined &&
      bannedPlayerInformation != undefined
    ) {
      console.log(bannedPlayerInformation);
      createPlayerCards(serverPingResponse.players.sample);
      createBannedPlayerCards(bannedPlayerInformation);
    }
  }, [instanceInformation, serverPingResponse]);

  const isOpped = (uuid: string) => {
    const data = instanceInformation;
    for (let i = 0; i < data.length; i++) {
      if (data[i].uuid === uuid) {
        return true;
      }
    }
    return false;
  };

  function playerAction(command: string) {
    contextValue?.socket.emit(
      "instance_command",
      {
        instanceName: accountContext.instanceLastOn,
        cmd: `${command} ${information.player.name}`,
      },
      (response) => {
        setTimeout(() => {
          contextValue?.socket.emit("get_spl", {
            instanceName: accountContext.instanceLastOn,
          });
          contextValue?.socket.emit("instance_information", {
            instanceName: accountContext.instanceLastOn,
            full_information: true,
          });
        }, 500);
      }
    );
  }

  return (
    <div style={{ width: "100%", padding: "24px", color: "white" }}>
      <div className={styles.container}>
        <div className={styles.side}>
          <div className={styles.header}>
            <Button
              onClick={() => {
                setBannedTab((prev) => !prev);
                setPlayerInformation(undefined);
              }}
              style={{ padding: "25px" }}
              className={"bg-clear"}
            >
              <Expand width={28} height={28} className={"icon-btn"}></Expand>
            </Button>
            <p
              className="headerText"
              style={{ textAlign: "center", flex: "90%" }}
            >
              {!isBannedTab ? "Online Players" : "Banned Players"}
            </p>
            <Button
              style={{ padding: "25px" }}
              onClick={() => {
                contextValue?.socket.emit("get_spl", {
                  instanceName: accountContext.instanceLastOn,
                });
              }}
              className={"bg-clear"}
            >
              <Restart className={"icon-btn"}></Restart>
            </Button>
          </div>
          <div className={styles.playerList}>
            {!isBannedTab ? players : bannedPlayers}
          </div>
        </div>
        <div className={styles.main}>
          {information != undefined ? (
            !isBannedTab ? (
              <div
                style={{
                  display: "flex",
                  width: "100%",
                  flexDirection: "column",
                  gap: "12px",
                }}
              >
                <p className={"headerText"}>Player Actions</p>
                {isOpped(information.player.id) ? (
                  <Button onClick={() => playerAction("deop")}>
                    Take Operator Status
                  </Button>
                ) : (
                  <Button onClick={() => playerAction("op")}>
                    Give Operator Status
                  </Button>
                )}
                <Button onClick={() => playerAction("ban")}>Ban Player</Button>
              </div>
            ) : (
              <div
                style={{
                  display: "flex",
                  width: "100%",
                  flexDirection: "column",
                  gap: "12px",
                }}
              >
                <p className={"headerText"}>Player Actions</p>
                <Button onClick={() => playerAction("pardon")}>
                  Pardon Player
                </Button>
              </div>
            )
          ) : (
            <></>
          )}
        </div>
      </div>
    </div>
  );
}
