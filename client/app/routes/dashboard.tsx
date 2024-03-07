import type { MetaFunction } from "@remix-run/node";
import styles from "../styles/_index.module.css";
import Console from "~/components/console";
import { memo, useContext, useEffect, useState } from "react";
import { WebSocketContext, connect } from "~/context/socket";
import Button from "~/components/button";
import Play from "~/components/icons/play";
import Stop from "~/components/icons/stop";
import Restart from "~/components/icons/restart";
import { eventHandler } from "~/context/socket_events";
import ProgressBar from "~/components/progressBar";
import { AccountContext } from "~/context/account";
import { Link, useNavigate } from "@remix-run/react";

export const meta: MetaFunction = () => {
  return [
    { title: "Centrum" },
    { name: "description", content: "Welcome to Remix!" },
  ];
};

export default function Dashboard() {
  const contextValue = useContext(WebSocketContext);
  const accountContext = useContext(AccountContext);
  const nav = useNavigate();

  const [connected, setConnected] = useState(false);
  const [instanceRunning, setInstanceRunning] = useState(false);

  const [ramUsage, setRamUsage] = useState(0);

  useEffect(() => {
    // eventHandler.on("instance_status", (data: { state: string }) => {
    //   console.log(data);
    //   if (data.state === "running") {
    //     setInstanceRunning(true);
    //   } else if (data.state === "stopped") {
    //     setInstanceRunning(false);
    //   }
    // });
    // eventHandler.on("instance_data", (data: { memory: number }) => {
    //   let toMB = data.memory;
    //   console.log(toMB);
    // });
  }, []);

  useEffect(() => {
    if (accountContext.instanceLastOn == undefined) {
      nav("/");
    }

    contextValue?.socket.emit("instance_information", {
      instanceName: accountContext.instanceLastOn,
    });

    eventHandler.unique(
      "instance_information",
      `${accountContext.instanceLastOn}`,
      (data: { state: string; ops: any }) => {
        console.log(data);
        if (data.state === "running") {
          setInstanceRunning(true);
        } else if (data.state === "stopped") {
          setInstanceRunning(false);
        }
      }
    );
  }, []);

  return (
    <div style={{ width: "100%", padding: "24px", color: "white" }}>
      {accountContext.instanceLastOn == undefined ? (
        <div></div>
      ) : (
        <div className={"container"}>
          <div className={"header bg-45"} style={{ borderRadius: "12px" }}>
            <div className={"rownoheight"} style={{}}>
              {instanceRunning ? (
                <>
                  <Button
                    style={{ width: "150px" }}
                    className={"btn-red"}
                    onClick={() => {
                      contextValue?.socket.emit("instance_state", {
                        instanceName: accountContext.instanceLastOn,
                        state: "stopped",
                      });
                    }}
                  >
                    <Stop></Stop>
                  </Button>
                </>
              ) : (
                <>
                  <Button
                    style={{ width: "150px" }}
                    className={"btn-green"}
                    onClick={() => {
                      eventHandler.emit("clean_console", "");
                      contextValue?.socket.emit("instance_state", {
                        instanceName: accountContext.instanceLastOn,
                        state: "running",
                      });
                    }}
                  >
                    <Play></Play>
                  </Button>
                </>
              )}
            </div>
          </div>
          <div
            className={"row"}
            style={{ overflow: "hidden", maxHeight: "100%" }}
          >
            {/* <div className={"box"} style={{ flex: "30%", gap: "24px", display: "flex", flexDirection: "column" }}>
              <p className={"headerText"}>Information</p>
              <div className={"box-inner"} style={{ marginTop: "-24px" }}>
                <ProgressBar value={ramUsage}>
                  <p>Cpu Usage</p>
                </ProgressBar>
                <ProgressBar value={ramUsage}>
                  <p>Ram Usage</p>
                </ProgressBar>
              </div>
              <div className={"box-inner"}>
                <p className={"headerText"}>Server Details</p>
                <p>Name</p>
                <p>Status</p>
                <p>Player Count</p>
                <p>Last Start</p>
              </div>
            </div> */}
            <Console instanceName={accountContext.instanceLastOn} />
          </div>
        </div>
      )}
    </div>
  );
}
