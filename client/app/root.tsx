import {
  Links,
  LiveReload,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
  useLocation,
  useNavigate,
} from "@remix-run/react";
import NavBar from "./components/navbar";
import "./global.css";
import { useEffect, useState } from "react";
import { UserContext, WebSocketContext, connect } from "./context/socket";
import { eventHandler } from "./context/socket_events";
import { Account, AccountContext } from "./context/account";
import Login from "./components/pages/login";
import Alert from "./components/alert";

export default function App() {
  const location = useLocation();
  const [ctx, setSocket] = useState<UserContext>();
  const [accountCtx, setAccountCtx] = useState<Account>(new Account());

  const nav = useNavigate();

  const tryConnection = () => {
    const socket = connect();
    socket.socket.onclose = (e) => {
      if (e.wasClean) {
        console.log(
          `WebSocket closed cleanly, code=${e.code}, reason=${e.reason}`
        );
      } else {
        setTimeout(() => {
          console.log("Retrying WebSocket connection...");
          tryConnection();
        }, 5000);
      }
    };

    socket.socket.onerror = (e) => {};

    socket.socket.onopen = (e) => {
      setSocket({
        socket: socket,
      });
      console.log("connection established");
      eventHandler.emit("connection", socket);
      nav("/");
      setAccountCtx(new Account());
    };

    socket.socket.onmessage = (e) => {
      // get the event key and emit to event handler
      const split = e.data.split("::");

      eventHandler.emit(split[0], JSON.parse(split[1]));
    };

    setSocket({
      socket: socket,
    });
  };

  useEffect(() => {
    tryConnection();

    // subscribe to close event
    eventHandler.on("close", (data: boolean) => {
      ctx?.socket.close();
      console.log("closed");
      window.location.href = "/";
    });

    eventHandler.on("login", (data: { status: number; token: string }) => {
      if (data.status == 200) {
        console.log(data);
        let acc = new Account();
        acc.username = "Admin";
        acc.loggedin = true;
        acc.token = data.token;
        setAccountCtx(acc);
      }
    });

    eventHandler.on(
      "redirect",
      ({ url, message }: { url: string; message: string }) => {
        console.log(url, message);
        window.location.href = "";
      }
    );
  }, []);

  const layout = () => {
    /*
       If we are logged in return the navbar and outlet
       unless we are on the instance page we remove the navbar
     */
    if (ctx?.socket.socket.readyState === 1 && accountCtx.loggedin) {
      return location.pathname !== "/" ? (
        <>
          <NavBar />
          <Outlet />
        </>
      ) : (
        <Outlet />
      );
    }

    /*
      otherwise return our login page
    */

    return <Login />;
  };

  return (
    <html lang="en">
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <Meta />
        <link
          href="https://fonts.googleapis.com/css2?family=Montserrat:ital,wght@0,100..900;1,100..900&display=swap"
          rel="stylesheet"
        ></link>
        <Links />
      </head>

      <body>
        <div
          style={{
            width: "100%",
            height: "100%",
            display: "flex",
            flexDirection: "row",
          }}
        >
          <AccountContext.Provider value={accountCtx}>
            <WebSocketContext.Provider value={ctx}>
              {layout()}
            </WebSocketContext.Provider>
          </AccountContext.Provider>
        </div>
        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  );
}
