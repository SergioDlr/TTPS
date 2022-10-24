import { Container, Button, Box } from "@mui/material";
import CustomModal from "@/components/utils/Modal";
import { useState, useEffect, useContext } from "react";
import VacunaDesarrolladaTabla from "./VacunaDesarrolladaTabla";
import RegistrarVacunaDesarrollada from "./RegistrarVacunaDesarrollada/RegistrarVacunaDesarrollada";
import allUrls from "@/services/backend_url";
import { UserContext } from "@/components/Context/UserContext";
import { useAlert } from "react-alert";
import CustomLoader from "@/components/utils/CustomLoader";
import axios from "axios";
import CustomButton from "@/components/utils/CustomButtom";
const VacunasDesarrolladasContainer = () => {
  const [open, setOpen] = useState(false);
  const [vacunasDesarrolladas, setVacunasDesarrolladas] = useState([]);
  const [estaCargando, setEstaCargando] = useState(true);
  const { userSesion } = useContext(UserContext);
  const alert = useAlert();
  useEffect(() => {
    cargarVacunasDesarrolladas();
  }, []);

  const cargarVacunasDesarrolladas = () => {
    try {
      axios
        .get(`${allUrls.todasVacunasDesarrolladas}?emailOperadorNacional=${userSesion.email}`)
        .then((response) => {
          setVacunasDesarrolladas(response?.data?.listaVacunasDesarrolladasDTO);
        })
        .catch((error) => {
          alert.error(`Ocurrio un error ${error}`);
        })
        .finally(() => setEstaCargando(false));
    } catch (error) {
      alert.error(`Ocurrio un error del lado del servidor: ${error}`);
    }
  };
  return (
    <Container>
      {!estaCargando ? (
        <>
          <Box sx={{ marginTop: 2, marginBottom: 2 }}>
            <CustomButton sx={{ marginRight: 1 }} color={"info"} textColor={"#2E7994"} variant={"outlined"}>
              Comprar
            </CustomButton>
            <CustomButton sx={{ marginRight: 1 }} textColor={"#2E7994"} color={"info"} variant={"outlined"}>
              Distribuir
            </CustomButton>
            <CustomModal title="Cargar vacuna desarrollada" color={"info"} open={open} setOpen={setOpen}>
              <RegistrarVacunaDesarrollada setOpen={setOpen} cargarVacunasDesarrolladas={cargarVacunasDesarrolladas} />
            </CustomModal>
          </Box>
          <VacunaDesarrolladaTabla vacunasDesarrolladas={vacunasDesarrolladas} />
        </>
      ) : (
        <CustomLoader />
      )}
    </Container>
  );
};

export default VacunasDesarrolladasContainer;