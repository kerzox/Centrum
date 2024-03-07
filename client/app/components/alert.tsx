import React, { useEffect, useState } from "react";
import Button from "./button";

const hidden: React.CSSProperties = {
  display: "none",
};

const not_hidden: React.CSSProperties = {
  display: "block",
};

const Alert = ({
  alert,
  className,
  childRef,
}: {
  alert: any;
  className: string;
  childRef: any;
}) => {
  const [visible, setVisible] = useState(alert.title != undefined);

  const display = () => {
    console.log(alert);
    alert?.clear();
  };

  useEffect(() => {
    setVisible(alert.title != undefined);
  }, [alert]);

  return (
    <div
      ref={childRef}
      style={!visible ? hidden : not_hidden}
      className={`alert ${alert.type} ${className} ${
        visible ? "slide-in" : ""
      }`}
      onClick={display}
    >
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          padding: "12px",
          gap: "12px",
          width: "100%",
        }}
      >
        <h1 style={{ padding: 0, margin: 0 }}>{alert.title}</h1>
        <span style={{ overflow: "auto" }}>
          {alert.body != undefined ? (
            alert.body.split("\n").map((t: any, i: number) => (
              <p style={{ margin: 0 }} key={t + ":" + i}>
                {t}
              </p>
            ))
          ) : (
            <></>
          )}
        </span>
      </div>
      {alert.function != undefined ? (
        <div
          style={{
            display: "flex",
            gap: "6px",
            justifyContent: "space-evenly",
            marginTop: "auto",
          }}
        >
          <Button
            onClick={alert.function.accept}
            className="button-clean"
            style={{ flex: 1, width: "100%" }}
          >
            Accept
          </Button>{" "}
          <Button
            onClick={alert.function.deny}
            className="button-danger"
            style={{ flex: 1, width: "100%" }}
          >
            Deny
          </Button>
        </div>
      ) : (
        <></>
      )}
    </div>
  );
};

export default Alert;
