import CustomModal from "@/components/utils/Modal";
import { TextField, Button } from "@mui/material";
import { UserContext } from "@/components/Context/UserContext";
import { useState, useContext } from "react";
import axios from "axios ";
import allUrls from "@/services/backend_url";
import { useAlert } from "react-alert";
import CustomButton from "@/components/utils/CustomButtom";
const CargarMarcaComercial = ({ cargarMarcasComerciales }) => {
  const [open, setOpen] = useState(false);
  const alert = useAlert();
  const { userSesion } = useContext(UserContext);
  const [descripcion, setDescripcion] = useState("");
  const handleChangeDescripcion = (evt) => {
    setDescripcion(evt.target.value);
  };
  const handleSubmit = (evt) => {
    evt.preventDefault();
    if (descripcion.length < 1) return alert.error("Complete el campo");
    try {
      axios
        .post(allUrls.marcasComercialesCrear, {
          EmailOperadorNacional: userSesion.email,
          DescripcionMarcaComercial: descripcion,
        })
        .then((response) => {
          if (response.data.estadoTransaccion === "Aceptada") {
            alert.success("se creo con exito");
            cargarMarcasComerciales();
          } else {
            alert.error(response.data.errores);
          }
        })
        .catch((error) => {
          alert.error(`Ocurrio un error del lado del servidor ${error}`);
        })
        .finally(() => setOpen(false));
    } catch (error) {
      alert.error(`Ocurrio un error del lado del servidor ${error}`);
    }
  };

  return (
    <>
      <CustomModal color={"info"} title="Cargar marca" open={open} setOpen={setOpen}>
        <h4>Cargar marca comercial:</h4>
        <form onSubmit={handleSubmit}>
          <TextField
            sx={{ display: "block" }}
            fullWidth
            id="nombre-field"
            label="Nombre marca comercial"
            variant="filled"
            type="text"
            required
            onChange={handleChangeDescripcion}
          />
          <div style={{ marginTop: 6 }}>
            <CustomButton
              type={"submit"}
              sx={{ marginRight: 1 }}
              variant={"contained"}
              color={"error"}
              onClick={() => setOpen(false)}
            >
              Cancelar
            </CustomButton>
            <CustomButton type={"submit"} variant={"contained"} color={"success"}>
              Cargar
            </CustomButton>
          </div>
        </form>
      </CustomModal>
    </>
  );
};

export default CargarMarcaComercial;
