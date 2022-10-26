import { useEffect, useState, useContext } from "react";
import { cargarVacunas } from "@/services/getVacunas";
import { useAlert } from "react-alert";
import allUrls from "@/services/backend_url";
import { FormControl, InputLabel, Select, MenuItem } from "@mui/material";
import SelectDosis from "./SelectDosis";
import CustomLoader from "../utils/CustomLoader";
import CustomButton from "../utils/CustomButtom";
import CustomModal from "../utils/Modal";
import { UserContext } from "../Context/UserContext";
import { Box } from "@mui/material";
import axios from "axios";
const FormularioVacunacion = ({ persona, email, setOpenPadre }) => {
  const alert = useAlert();
  const [vacunasCreadas, setVacunasCreadas] = useState([]);
  const [estaCargando, setEstaCargando] = useState(true);
  const [vacunaSeleccionada, setVacunaSeleccionada] = useState(0);
  const [dosisSeleccionada, setDosisSeleccionada] = useState(0);
  const [respuestaConsulta, setRespuestaConsulta] = useState({});
  const [open, setOpen] = useState(false);
  const [errores, setErrores] = useState([]);
  const { userSesion } = useContext(UserContext);

  useEffect(() => {
    cargarVacunas(setVacunasCreadas, allUrls.todasVacunas, email, alert, () => setEstaCargando(false));
  }, []);

  const handleChangeVacuna = (evt) => {
    setDosisSeleccionada(0);
    setVacunaSeleccionada(evt.target.value);
  };
  const dateParser = (str) => {
    return `${str[6]}${str[7]}${str[8]}${str[9]}-${str[3]}${str[4]}-${str[0]}${str[1]} T${str[11]}${str[12]}${str[13]}${str[14]}${str[15]}${str[16]}${str[17]}${str[18]}`;
  };

  const handleSubmit = (evt) => {
    evt.preventDefault();
    setEstaCargando(true);
    axios
      .post(allUrls.consultarVacunacion, {
        EmailVacunador: userSesion.email,
        Dni: persona.DNI,
        SexoBiologico: persona.genero,
        Nombre: persona.nombre,
        Apellido: persona.apellido,
        Embarazada: persona.embarazada,
        PersonalSalud: persona.personal_salud,
        FechaHoraNacimiento: dateParser(persona.fecha_hora_nacimiento),
        IdVacuna: vacunaSeleccionada.id,
        IdDosis: dosisSeleccionada.id,
        JurisdiccionResidencia: persona.jurisdiccion,
      })
      .then((response) => {
        if (response?.data?.estadoTransaccion === "Aceptada") {
          setRespuestaConsulta(response.data);
          if (response.data.alertasVacunacion !== null) {
            setErrores(response.data.alertasVacunacion);
          }
          setOpen(true);
        } else {
          alert.error(response.data.errores);
        }
      })
      .catch((error) => {
        console.log(error);
      })
      .finally(() => setEstaCargando(false));
  };

  const handleVacunacion = () => {
    try {
      setEstaCargando(true);
      axios
        .post(allUrls.crearVacunacion, {
          EmailVacunador: userSesion.email,
          Dni: persona.DNI,
          SexoBiologico: persona.genero,
          Nombre: persona.nombre,
          Apellido: persona.apellido,
          Embarazada: persona.embarazada,
          PersonalSalud: persona.personal_salud,
          FechaHoraNacimiento: dateParser(persona.fecha_hora_nacimiento),
          IdVacuna: vacunaSeleccionada.id,
          IdDosis: dosisSeleccionada.id,
          JurisdiccionResidencia: persona.jurisdiccion,
          idLote: respuestaConsulta.vacunaDesarrolladaAplicacion.idLote,
          IdVacunaDesarrollada: respuestaConsulta.vacunaDesarrolladaAplicacion.id,
          Alertas: respuestaConsulta.alertasVacunacion.toString(),
          IdRegla: respuestaConsulta.dosisCorrespondienteAplicacion.reglas[0].id,
        })
        .then((response) => {
          if (response?.data?.estadoTransaccion === "Aceptada") {
            alert.success("La vacuna se aplico exitosamente");
            alert.success(response?.data?.vacunaDesarrolladaAplicacion.descripcion);
          } else {
            alert.error(response?.data?.errores);
          }
        })
        .catch((error) => {
          console.log(error);
        })
        .finally(() => {
          setOpenPadre(false);
          setEstaCargando(false);
        });
    } catch (e) {
      alert.error(`${e}`);
      setEstaCargando(false);
      setOpenPadre(false);
    }
  };
  return (
    <>
      {estaCargando && <CustomLoader />}
      <form onSubmit={handleSubmit}>
        Dni: {persona.DNI} - Nombre: {persona.nombre}
        <FormControl sx={{ marginTop: 1 }} fullWidth>
          <InputLabel id="Tipo-de-vacuna">Vacunas:</InputLabel>
          <Select
            labelId="Tipo-de-vacuna"
            id="Tipo-de-vacuna"
            value={vacunaSeleccionada}
            label="Vacunas:"
            onChange={handleChangeVacuna}
            required
          >
            <MenuItem disabled value={0}>
              Selecciona una vacuna
            </MenuItem>
            {vacunasCreadas.map((element, index) => (
              <MenuItem key={index} value={element}>
                {element.descripcionTipoVacuna} - {element.descripcion}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        {vacunaSeleccionada != 0 && (
          <SelectDosis
            vacuna={vacunaSeleccionada}
            dosisSeleccionada={dosisSeleccionada}
            setDosisSeleccionada={setDosisSeleccionada}
          />
        )}
        <Box>
          <CustomButton
            sx={{ marginTop: 1 }}
            type={"submit"}
            variant={"contained"}
            color={"error"}
            onClick={() => setOpenPadre(false)}
          >
            Cancelar
          </CustomButton>
          {vacunaSeleccionada != 0 && dosisSeleccionada != 0 && (
            <CustomButton sx={{ marginTop: 1 }} type={"submit"} variant={"outlined"} color={"info"} textColor={"black"}>
              Consultar
            </CustomButton>
          )}
        </Box>
      </form>
      <CustomModal displayButton={false} open={open} setOpen={setOpen}>
        {errores.length > 0 ? (
          <>
            <h5>Se encontraron problemas:</h5>
            {errores.map((error, index) => (
              <p key={index}>{error}</p>
            ))}
          </>
        ) : (
          <h5>No hay problemas para aplicar la vacuna</h5>
        )}
        <CustomButton
          sx={{ marginTop: 1 }}
          variant={"outlined"}
          color={"error"}
          textColor={"black"}
          onClick={() => setOpen(false)}
        >
          Cancelar
        </CustomButton>
        <CustomButton
          sx={{ marginTop: 1 }}
          variant={"outlined"}
          color={"info"}
          textColor={"black"}
          onClick={handleVacunacion}
        >
          Vacunar
        </CustomButton>
      </CustomModal>
    </>
  );
};

export default FormularioVacunacion;
