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
import { GlobalState, GlobalContext } from "./context/global_state";
import Login from "./components/pages/login";
import Alert from "./components/alert";

export interface Account {
  username: string;
  token: string;
}

export default function App() {
  const location = useLocation();
  const [ctx, setSocket] = useState<UserContext>();
  const [accountCtx, setAccountCtx] = useState<GlobalState>(new GlobalState());
  const [loggedIn, setLoggedIn] = useState(false);

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
      setAccountCtx(new GlobalState());

      const session = sessionStorage.getItem("user");
      if (session != null) {
        const acc = JSON.parse(session) as Account;
        socket.emit("reauthenticate", {});
      }
    };

    socket.socket.onmessage = (e) => {
      const parsed = JSON.parse(e.data);
      console.log(parsed);
      eventHandler.emit(parsed.eventKey, parsed.data);
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

    eventHandler.on(
      "reauthenticate",
      (response: { status: number; error: string }) => {
        if (response.status != 200) {
          if (response.status == 401) {
            alert("Your session has expired please login again");
          }
          if (response.status == 403) {
            alert(
              "You're not authorised for this action\nIf you should be if may because of a malformed token try logging in again"
            );
          }
          setTimeout(() => {
            sessionStorage.removeItem("user");
            setLoggedIn(false);
          }, 1000);
          return;
        }
        setLoggedIn(true);
      }
    );

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

    if (ctx?.socket.socket.readyState === 1 && loggedIn) {
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

    return (
      <Login
        state={(param: boolean | ((prevState: boolean) => boolean)) =>
          setLoggedIn(param)
        }
      />
    );
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
          <GlobalContext.Provider value={accountCtx}>
            <WebSocketContext.Provider value={ctx}>
              {layout()}
            </WebSocketContext.Provider>
          </GlobalContext.Provider>
        </div>
        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  );
}
