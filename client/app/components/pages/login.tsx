import Input from "~/components/input";
import styles from "../../styles/login.module.css";
import Button from "~/components/button";
import { useContext, useRef, useState } from "react";
import { WebSocketContext } from "~/context/socket";
import { GlobalContext } from "~/context/global_state";
import Alert from "../alert";

function decodeJwt(token: string) {
  const parts = token.split(".");

  if (parts.length !== 3) {
    throw new Error("Invalid JWT format");
  }

  const [header, payload, signature] = parts;

  const decodedHeader = JSON.parse(atob(header));
  const decodedPayload = JSON.parse(atob(payload));

  return {
    header: decodedHeader,
    payload: decodedPayload,
    signature: signature,
  };
}

function setExpiryCallback(expiryTimestamp: number, callback: any) {
  const currentTimestamp = Math.floor(Date.now() / 1000); // get time in seconds
  const remainingTime = expiryTimestamp - currentTimestamp; // remaining time

  // If the token is already expired, execute the callback immediately
  if (remainingTime <= 0) {
    callback();
    return;
  }

  const timeoutId = setTimeout(callback, remainingTime * 1000);

  // Return a function to clear the timeout if needed
  return function flushExpiry() {
    clearTimeout(timeoutId);
  };
}

export default function Login(props: any) {
  const contextValue = useContext(WebSocketContext);
  const accountValue = useContext(GlobalContext);

  const [username, changeUsername] = useState("");
  const [password, changePassword] = useState("");

  const alertRef = useRef(null);
  const [alertData, setAlertData] = useState<any>({});

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

  return (
    <div style={{ width: "100%", padding: "24px", color: "white" }}>
      <div
        className={"center_container"}
        style={{ flexDirection: "column", gap: "24px" }}
      >
        <Alert
          childRef={alertRef}
          className={styles.alert}
          alert={alertData}
        ></Alert>
        <div className={styles.login}>
          <div
            className={styles.header}
            style={{ borderBottom: "5px white solid" }}
          >
            <h1>Login</h1>
          </div>
          <div className={styles.main}>
            <div className={styles.col} style={{ gap: "12px" }}>
              <Input
                className={styles.input}
                onChange={changeUsername}
                placeholder={"Username"}
              ></Input>
              <Input
                className={styles.input}
                onChange={changePassword}
                placeholder={"Password"}
              ></Input>
            </div>
            <Button
              className={styles.button}
              onClick={() =>
                contextValue?.socket.emit(
                  "login",
                  {
                    username: username,
                    password: password,
                  },
                  (res: {
                    status: number;
                    username: string;
                    token: string;
                  }) => {
                    if (res.status == 200) {
                      sendAlert("Alert", "Login successful", "bg-success");
                      setTimeout(() => {
                        sessionStorage.setItem(
                          "user",
                          JSON.stringify({
                            username: res.username,
                            token: res.token,
                          })
                        );

                        props.state(true);

                        const decodedToken = decodeJwt(res.token);
                        const onTokenExpire = () => {
                          console.log("expired");
                          contextValue?.socket.emit("reauthenticate", {});
                        };

                        setExpiryCallback(
                          decodedToken.payload.exp,
                          onTokenExpire
                        );
                      }, 1000);
                    }

                    if (res.status == 401) {
                      sendAlert(
                        "Alert",
                        "Incorrect username and or password",
                        "bg-error"
                      );
                    }
                  }
                )
              }
            >
              Login
            </Button>
          </div>
          <div className={styles.footer}>
            <div style={{ padding: "12px" }}>
              <h3>Centrum</h3>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
