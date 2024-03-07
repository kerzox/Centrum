import Input from "~/components/input";
import styles from "../../styles/login.module.css";
import Button from "~/components/button";
import { useContext, useState } from "react";
import { WebSocketContext } from "~/context/socket";
import { AccountContext } from "~/context/account";

export default function Login() {
  const contextValue = useContext(WebSocketContext);
  const accountValue = useContext(AccountContext);

  const [username, changeUsername] = useState("");
  const [password, changePassword] = useState("");

  return (
    <div style={{ width: "100%", padding: "24px", color: "white" }}>
      <div className={"center_container"}>
        <div className={styles.login}>
          <div className={styles.header} style={{ borderBottom: "5px white solid" }}>
            <h1>Login</h1>
          </div>
          <div className={styles.main}>
            <div className={styles.col} style={{ gap: "12px" }}>
              <Input className={styles.input} onChange={changeUsername} placeholder={"Username"}></Input>
              <Input className={styles.input} onChange={changePassword} placeholder={"Password"}></Input>
            </div>
            <Button className={styles.button} onClick={() => contextValue?.socket.emit("login", { username: username, password: password })}>
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
