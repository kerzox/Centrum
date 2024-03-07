import styles from "../styles/input.module.css";

const Input = (props: any) => {
  const onChange = (e: any) => {
    props.onChange(e.target.value);
  };

  return (
    <input style={props.style} type={props.type} placeholder={props.placeholder} 
    className={`${styles.input} ${props.className}`} onChange={onChange} value={props.value}>
      {props.children}
    </input>
  );
};

export default Input;
