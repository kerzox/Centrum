import { useEffect, useRef, useState } from "react";
import styles from "../styles/contextmenu.module.css";
import Button from "./button";
import Trash from "./icons/trash";

const ContextMenu = (props: any) => {
  const mouseDown = (e: any) => {
    const div = document.getElementById("contextMenuId");

    if (props.contextState == undefined || div == undefined) return;

    const dimensions = div.getBoundingClientRect();
    const inBounds =
      e.clientX > dimensions.x &&
      e.clientX < dimensions.x + dimensions.width &&
      e.clientY > dimensions.y &&
      e.clientY < dimensions.y + dimensions.height;

    if (inBounds) {
      return;
    }
    props.clear();
    window.removeEventListener("mousedown", mouseDown);
  };

  useEffect(() => {
    if (props.contextState != undefined) {
      window.addEventListener("mousedown", mouseDown);
    }
  }, [props.contextState]);

  if (props.contextItems == undefined) return <></>;

  return props.contextState != undefined ? (
    <div
      id="contextMenuId"
      style={{
        left: props.contextState.position.x,
        top: props.contextState.position.y,
      }}
      className={styles.contextMenuContainer}
    >
      <div className={styles.header}>{props.contextItems.header}</div>
      <hr
        style={{
          width: "100%",
          margin: 0,
          marginTop: "4px",
          marginBottom: "4px",
        }}
      ></hr>
      <div className={styles.main} style={{ padding: 0 }}>
        {props.contextItems.main}
      </div>
    </div>
  ) : (
    <></>
  );
};

export default ContextMenu;

/*

        */
