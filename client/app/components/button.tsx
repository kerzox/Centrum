import React from "react";
import styles from "../styles/button.module.css";

const Button = (props: any) => {
  return (
    <button
      onContextMenu={props.onContextMenu}
      onKeyDown={props.onKeyDown}
      style={props.style}
      className={`${styles.button} ${props.className}`}
      onClick={props.onClick}
    >
      {props.children}
    </button>
  );
};

export default Button;
