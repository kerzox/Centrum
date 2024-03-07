import React, { useEffect, useRef } from "react";
import styles from "../styles/progressbar.module.css";

const ProgressBar = (props: any) => {
  const progressRef = useRef<HTMLProgressElement>(null);

  return (
    <button onKeyDown={props.onKeyDown} style={props.style} className={`${styles.button} ${props.className}`} onClick={props.onClick}>
      {props.children}
      <progress max={100} className={styles.progress} value={props.value}></progress>
    </button>
  );
};

export default ProgressBar;
