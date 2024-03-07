import React, { useEffect } from "react";
import styles from "../styles/navbar.module.css";
import { Link, useLocation, useNavigate } from "@remix-run/react";
import ChevronLeft from "./icons/chevron_left";
import Button from "./button";

const NavBar = (props: any) => {
  const location = useLocation();
  const nav = useNavigate();

  return (
    <div id="navbar" className={styles.navbar}>
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          justifyContent: "space-between",
          height: "100%",
          paddingLeft: "24px",
          paddingRight: "24px",
        }}
      >
        <ul className={styles.listcontainer}>
          <div
            style={{
              display: "flex",
              gap: "12px",
            }}
          >
            <Button
              title="Go back"
              style={{
                width: "100%",
                display: "flex",
                backgroundColor: "transparent",
                alignItems: "center",
              }}
              onClick={() => nav("/")}
            >
              <ChevronLeft></ChevronLeft>
              <h1 style={{ color: "white" }} className="headerText">
                Centrum
              </h1>
            </Button>
          </div>
          <li className={styles.item}>
            <Link
              className={`${styles.button} ${
                location.pathname == "/dashboard" ? styles.active : ""
              }`}
              to="dashboard"
            >
              Dashboard
            </Link>
          </li>
          <li className={styles.item}>
            <Link
              className={`${styles.button} ${
                location.pathname == "/users" ? styles.active : ""
              }`}
              to="users"
            >
              Users
            </Link>
          </li>
          <li className={styles.item}>
            <Link
              className={`${styles.button} ${
                location.pathname == "/filemanager" ? styles.active : ""
              }`}
              to="filemanager"
            >
              File Manager
            </Link>
          </li>
        </ul>
        <div style={{ paddingBottom: "15%", width: "100%" }}>
          <a
            className={`${styles.button} ${
              location.pathname == "/login" ? styles.active : ""
            }`}
            href=""
          >
            Logout
          </a>
        </div>
      </div>
    </div>
  );
};

export default NavBar;
