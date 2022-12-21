﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacunacionApi.DTO;
using VacunacionApi.Models;
using VacunacionApi.ModelsDataWareHouse;
using VacunacionApi.Services;

namespace VacunacionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VacunasController : ControllerBase
    {
        private readonly VacunasContext _context;
        private readonly DataWareHouseContext _context2;

        public VacunasController(VacunasContext context, DataWareHouseContext context2)
        {
            _context = context;
            _context2 = context2;
        }

        // GET: api/Vacunas/EjecutarCron?emailOperadorNacional=maria@gmail.com
        [HttpGet]
        [Route("EjecutarCron")]
        public async Task<ActionResult<ResponseCronDTO>> EjecutarCron(string emailOperadorNacional)
        {
            List<CompraCronDTO> comprasRecibidasHoy = new List<CompraCronDTO>();
            List<LoteCronDTO> lotesVencidosHoy = new List<LoteCronDTO>();
            ResponseCronDTO responseCronDTO = null;

            try
            {
                Usuario usu = UsuarioService.GetUsuario(_context, emailOperadorNacional);

                if (usu != null && RolService.TieneRol(_context, usu, "Operador Nacional"))
                {
                    EstadoCompra estadoCompraRecibida = await _context.EstadoCompra.Where(e => e.Descripcion == "Recibida").FirstOrDefaultAsync();
                    EstadoCompra estadoCompraNoRecibida = await _context.EstadoCompra.Where(e => e.Descripcion == "No Recibida").FirstOrDefaultAsync();
                    DateTime fechaHoy = DateTime.Now;
                    int anioHoy = fechaHoy.Year;
                    int mesHoy = fechaHoy.Month;
                    int diaHoy = fechaHoy.Day;

                    //Recepciones
                    List<Compra> comprasNoRecibidas = await _context.Compra
                       .Where(l => l.IdLoteNavigation.Disponible == false
                           && l.Distribuidas == 0
                           && l.IdEstadoCompra == estadoCompraNoRecibida.Id
                           && l.IdLoteNavigation.FechaVencimiento > DateTime.Now)
                       .OrderBy(l => l.IdLoteNavigation.FechaVencimiento)
                       .ToListAsync();

                    foreach (Compra compra in comprasNoRecibidas)
                    {
                        int anioFechaEntregaCompra = compra.FechaEntrega.Year;
                        int mesFechaEntregaCompra = compra.FechaEntrega.Month;
                        int diaFechaEntregaCompra = compra.FechaEntrega.Day;

                        if (anioHoy == anioFechaEntregaCompra && mesHoy == mesFechaEntregaCompra && diaHoy == diaFechaEntregaCompra)
                        {
                            Lote loteCompra = await _context.Lote.Where(l => l.Id == compra.IdLote).FirstOrDefaultAsync();

                            compra.IdEstadoCompra = estadoCompraRecibida.Id;
                            _context.Entry(compra).State = EntityState.Modified;
                            loteCompra.Disponible = true;
                            _context.Entry(loteCompra).State = EntityState.Modified;
                            await _context.SaveChangesAsync();

                            CompraCronDTO compraCronDTO = new CompraCronDTO(compra.Id, loteCompra.Id, compra.FechaCompra,
                                compra.FechaEntrega, compra.CantidadVacunas, compra.IdEstadoCompra, estadoCompraRecibida.Descripcion);

                            comprasRecibidasHoy.Add(compraCronDTO);
                        }
                    }

                    //Vencimientos
                    List<Lote> lotes = await _context.Lote.ToListAsync();

                    foreach (Lote lote in lotes)
                    {
                        int anioFechaVencimientoLote = lote.FechaVencimiento.Year;
                        int mesFechaVencimientoLote = lote.FechaVencimiento.Month;
                        int diaFechaVencimientoLote = lote.FechaVencimiento.Day;

                        if (anioHoy == anioFechaVencimientoLote && mesHoy == mesFechaVencimientoLote && diaHoy == diaFechaVencimientoLote)
                        {
                            Compra compra = await _context.Compra.Where(c => c.IdLote == lote.Id).FirstOrDefaultAsync();

                            lote.Disponible = false;
                            _context.Entry(lote).State = EntityState.Modified;

                            List<Distribucion> distribucionesLote = await _context.Distribucion
                                .Where(d => d.IdLote == lote.Id).ToListAsync();

                            foreach (Distribucion distribucion in distribucionesLote)
                            {
                                distribucion.Vencidas = distribucion.CantidadVacunas - distribucion.Aplicadas;
                                _context.Entry(distribucion).State = EntityState.Modified;
                            }

                            compra.Vencidas = compra.CantidadVacunas - compra.Distribuidas;
                            _context.Entry(compra).State = EntityState.Modified;

                            await _context.SaveChangesAsync();

                            VacunaDesarrollada vd = await _context.VacunaDesarrollada.Where(v => v.Id == lote.IdVacunaDesarrollada).FirstOrDefaultAsync();
                            Vacuna vac = await _context.Vacuna.Where(v => v.Id == vd.IdVacuna).FirstOrDefaultAsync();

                            LoteCronDTO loteCronDTO = new LoteCronDTO(lote.Id, lote.FechaVencimiento, vac.Id, vac.Descripcion, compra.CantidadVacunas);

                            lotesVencidosHoy.Add(loteCronDTO);
                        }
                    }
                }

                responseCronDTO = new ResponseCronDTO(comprasRecibidasHoy, lotesVencidosHoy);
            }
            catch
            {

            }

            return Ok(responseCronDTO);
        }

        // GET: api/Vacunas/GetDescripcionesVacunasCalendario
        [HttpGet]
        [Route("GetDescripcionesVacunasCalendario")]
        public ActionResult<List<DescripcionVacunaCalendarioAnualDTO>> GetDescripcionesVacunasCalendario()
        {
            List<DescripcionVacunaCalendarioAnualDTO> descripcionesVacunasCalendario = new List<DescripcionVacunaCalendarioAnualDTO>();

            try
            {
                descripcionesVacunasCalendario = VacunaService.GetDescripcionesVacunasCalendario();
            }
            catch
            {

            }

            return descripcionesVacunasCalendario;
        }

        // GET: api/Vacunas/GetDescripcionesVacunasAnuales
        [HttpGet]
        [Route("GetDescripcionesVacunasAnuales")]
        public ActionResult<List<DescripcionVacunaCalendarioAnualDTO>> GetDescripcionesVacunasAnuales()
        {
            List<DescripcionVacunaCalendarioAnualDTO> descripcionesVacunasAnuales = new List<DescripcionVacunaCalendarioAnualDTO>();

            try
            {
                descripcionesVacunasAnuales = VacunaService.GetVacunasAnuales();
            }
            catch
            {

            }

            return descripcionesVacunasAnuales;
        }

        // GET: api/Vacunas/GetAll?emailOperadorNacional=juan@gmail.com&idTipoVacuna=2
        [HttpGet]
        [Route("GetAll")]
        public async Task<ActionResult<ResponseListaVacunasDTO>> GetAll(string emailOperadorNacional = null, int idTipoVacuna = 0)
        {
            try
            {
                ResponseListaVacunasDTO responseListaVacunasDTO = null;
                List<string> errores = new List<string>();
                List<VacunaDTO> listaVacunasDTO = new List<VacunaDTO>();
                List<Vacuna> vacunas = new List<Vacuna>();

                if (idTipoVacuna != 0)
                {
                    List<List<string>> listaVerificacionTipoVacuna = TipoVacunaService.VerificarTipoVacuna(_context, errores, idTipoVacuna);
                    errores = listaVerificacionTipoVacuna[0];
                }
                if (emailOperadorNacional == null)
                {
                    errores.Add(string.Format("El email operador nacional es obligatorio"));
                }
                else
                {
                    errores = RolService.VerificarCredencialesUsuario(_context, emailOperadorNacional, errores, "Operador Nacional");
                }

                if (errores.Count > 0)
                {
                    responseListaVacunasDTO = new ResponseListaVacunasDTO("Rechazada", true, errores, emailOperadorNacional, listaVacunasDTO);
                }
                else
                {
                    if (idTipoVacuna == 0)
                    {
                        vacunas = await _context.Vacuna.ToListAsync();
                    }
                    else
                    {
                        vacunas = await _context.Vacuna.Where(vac => vac.IdTipoVacuna == idTipoVacuna).ToListAsync();
                    }

                    foreach (Vacuna vac in vacunas)
                    {
                        TipoVacuna tipoVacuna = TipoVacunaService.GetTipoVacuna(_context, vac.IdTipoVacuna);
                        Pandemia pandemia = PandemiaService.GetPandemia(_context, vac.IdPandemia);
                        List<EntidadVacunaDosis> listaEntidadVacunaDosis = await _context.EntidadVacunaDosis.Where(evd => evd.IdVacuna == vac.Id).ToListAsync();
                        List<DosisDTO> dosisDTO = new List<DosisDTO>();
                        
                        foreach (EntidadVacunaDosis evd in listaEntidadVacunaDosis)
                        {
                            Dosis dosis = await _context.Dosis.Where(d => d.Id == evd.IdDosis).FirstOrDefaultAsync();
                            List<EntidadDosisRegla> listaEntidadDosisRegla = await _context.EntidadDosisRegla.Where(edr => edr.IdDosis == evd.IdDosis).ToListAsync();
                            List<ReglaDTO> reglasDTO = new List<ReglaDTO>();
                            foreach (EntidadDosisRegla edr in listaEntidadDosisRegla)
                            {
                                Regla reg = await _context.Regla.Where(r => r.Id == edr.IdRegla).FirstOrDefaultAsync();
                                if (reg != null)
                                {
                                    ReglaDTO reglaDTO = new ReglaDTO(reg.Id, reg.Descripcion, reg.MesesVacunacion, 
                                        reg.LapsoMinimoDias, reg.LapsoMaximoDias, reg.Otros, reg.Embarazada, reg.PersonalSalud);
                                    
                                    reglasDTO.Add(reglaDTO);
                                }
                            }

                            DosisDTO d = new DosisDTO(dosis.Id, evd.Orden.Value, dosis.Descripcion, reglasDTO);
                            dosisDTO.Add(d);
                        }

                        VacunaDTO vacunaDTO = new VacunaDTO(vac.Id, vac.Descripcion, vac.IdTipoVacuna, tipoVacuna.Descripcion,
                            vac.IdPandemia, pandemia.Descripcion, vac.CantidadDosis, dosisDTO);

                        listaVacunasDTO.Add(vacunaDTO);
                    }

                    responseListaVacunasDTO = new ResponseListaVacunasDTO("Aceptada", false, errores, emailOperadorNacional, listaVacunasDTO);
                }

                return Ok(responseListaVacunasDTO);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // GET: api/Vacunas/GetHistoricos?emailOperadorNacional=juan@gmail.com&idVacunaDesarrollada&idJurisdiccion=7&fechaDesde=10/07/2022&fechaHasta=18/12/2022
        [HttpGet]
        [Route("GetHistoricos")]
        public async Task<ActionResult<ResponseHistoricoDTO>> GetHistoricos(string emailOperadorNacional = null, int idVacunaDesarrollada = 0, int idJurisdiccion = 0, string fechaDesde = null, string fechaHasta = null)
        {
            try
            {
                ResponseHistoricoDTO responseHistoricoDTO = null;
                List<string> errores = new List<string>();
                string[] vacunas = null;
                int diaDesde = Convert.ToDateTime(fechaDesde).Day;
                int mesDesde = Convert.ToDateTime(fechaDesde).Month;
                int anioDesde = Convert.ToDateTime(fechaDesde).Year;
                int diaHasta = Convert.ToDateTime(fechaHasta).Day;
                int mesHasta = Convert.ToDateTime(fechaHasta).Month;
                int anioHasta = Convert.ToDateTime(fechaHasta).Year;
                VacunaDesarrollada vd = null;
                Jurisdiccion juris = null;

                if (emailOperadorNacional == null || idVacunaDesarrollada == 0 || fechaDesde == null || fechaHasta == null || idJurisdiccion == 0)
                    errores.Add("Faltan parámetros de consulta. Verifique por favor");
                else
                {
                    errores = RolService.VerificarCredencialesUsuario(_context, emailOperadorNacional, errores, "Operador Nacional");
                    RolService.VerificarCredencialesUsuario(_context, emailOperadorNacional, errores, "Operador Nacional");
                    vd = VacunaDesarrolladaService.GetVacunaDesarrollada(_context, idVacunaDesarrollada);
                    juris = JurisdiccionService.GetJurisdiccion(_context, idJurisdiccion);

                    if (vd == null || juris == null)
                        errores.Add("Faltan parámetros de consulta. Verifique por favor");
                }    
                
                if (errores.Count > 0)
                    responseHistoricoDTO = new ResponseHistoricoDTO("Rechazada", true, errores, null, idVacunaDesarrollada, 0, 0, new List<DetalleMesAnioDTO>(), emailOperadorNacional);
                else
                {
                    List<DetalleMesAnioDTO> detalles = new List<DetalleMesAnioDTO>();
                    List<int> idsLotes = new List<int>();
                    int totalAplicadas = 0;
                    int saldo = 0;
                    List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada
                        .Where(vac => vac.FechaVacunacion.Day >= diaDesde && vac.FechaVacunacion.Month >= mesDesde && vac.FechaVacunacion.Year >= anioDesde
                            && vac.FechaVacunacion.Day >= diaHasta && vac.FechaVacunacion.Month >= mesHasta && vac.FechaVacunacion.Year <= anioHasta
                            && vac.IdJurisdiccion == idJurisdiccion).ToListAsync();
                    List<VacunaAplicada> resultado = new List<VacunaAplicada>();

                    foreach (VacunaAplicada vac in vacunasAplicadas)
                    {
                        Lote lote = LoteService.GetLote(_context, vac.IdLote);
                        
                        if(lote != null)
                        {
                            if (lote.IdVacunaDesarrollada == idVacunaDesarrollada && lote.Disponible)
                            {
                                resultado.Add(vac);
                                if (!idsLotes.Contains(lote.Id))
                                    idsLotes.Add(lote.Id);
                            }
                        }
                    }
                                   
                    foreach (VacunaAplicada vac in resultado)
                    {
                        bool existe = false;

                        foreach (DetalleMesAnioDTO det in detalles)
                        {
                            if (det.NumeroMes == vac.FechaVacunacion.Month && det.NumeroAnio == vac.FechaVacunacion.Year)
                            {
                                existe = true;
                                det.Aplicadas++;
                                totalAplicadas++;
                                break;
                            }
                        }

                        if (!existe)
                        {
                            DetalleMesAnioDTO d = new DetalleMesAnioDTO(vac.FechaVacunacion.Month, vac.FechaVacunacion.Year, 1);
                            totalAplicadas++;
                        }
                    }

                    foreach (int idLote in idsLotes)
                    {
                        List<Distribucion> distribuciones = await _context.Distribucion.Where(d => d.IdJurisdiccion == idJurisdiccion && d.IdLote == idLote).ToListAsync();
                        foreach (Distribucion dis in distribuciones)
                        {
                            saldo += dis.CantidadVacunas - dis.Vencidas - dis.Aplicadas;
                        }
                    }

                    responseHistoricoDTO = new ResponseHistoricoDTO("Aceptada", false, errores, juris.Descripcion, idVacunaDesarrollada, totalAplicadas, saldo, detalles, emailOperadorNacional);
                }

                return Ok(responseHistoricoDTO);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // GET: api/Vacunas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vacuna>> GetVacuna(int id)
        {
            var vacuna = await _context.Vacuna.FindAsync(id);

            if (vacuna == null)
            {
                return NotFound();
            }

            return vacuna;
        }

        // PUT: api/Vacunas/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVacuna(int id, Vacuna vacuna)
        {
            if (id != vacuna.Id)
            {
                return BadRequest();
            }

            _context.Entry(vacuna).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VacunaService.VacunaExists(_context, id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Vacunas/CrearVacunasCalendario
        [HttpPost]
        [Route("CrearVacunasCalendario")]
        public async Task<ActionResult<bool>> CrearVacunasCalendario()
        {
            try
            {
                List<VacunaCalendarioAnualPandemiaDTO> vacunasCalendario = VacunaService.GetVacunasCalendario();
                Pandemia noPandemia = PandemiaService.GetPandemiaByDescripcion(_context, "No Pandemia");
                TipoVacuna tipoVacuna = TipoVacunaService.GetTipoVacunaByDescripcion(_context, "Vacuna de Calendario");
               
                foreach (VacunaCalendarioAnualPandemiaDTO vac in vacunasCalendario)
                {
                    Vacuna vacuna = new Vacuna();
                    vacuna.Descripcion = vac.Descripcion;
                    vacuna.IdPandemia = noPandemia.Id;
                    vacuna.IdTipoVacuna = tipoVacuna.Id;
                    vacuna.CantidadDosis = 0;
                    _context.Vacuna.Add(vacuna);
                    await _context.SaveChangesAsync();

                    vac.Id = vacuna.Id;
                    VacunaCalendarioAnualPandemiaDTO vacCal = VacunaService.RespaldarDosisReglasByVacuna(_context, vac);
                    if (vacCal != null)
                        vacuna.CantidadDosis = vacCal.Dosis.Count;

                    _context.Entry(vacuna).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // POST: api/Vacunas/CrearVacuna
        [HttpPost]
        [Route("CrearVacuna")]
        public async Task<ActionResult<ResponseVacunaDTO>> CrearVacuna([FromBody] RequestVacunaDTO model)
        {
            try
            {
                ResponseVacunaDTO responseVacunaDTO = new ResponseVacunaDTO();
                List<string> errores = new List<string>();
                List<List<string>> listaVerificacionTipoVacuna = TipoVacunaService.VerificarTipoVacuna(_context, errores, model.IdTipoVacuna);
                List<VacunaCalendarioAnualPandemiaDTO> vacunasCalendario = new List<VacunaCalendarioAnualPandemiaDTO>();
                errores = listaVerificacionTipoVacuna[0];

                if (model.IdPandemia > 1)
                {
                    List<List<string>> listaVerificacionPandemia = PandemiaService.VerificarPandemia(_context, errores, model.IdPandemia);
                    errores = listaVerificacionPandemia[0];

                    if ((listaVerificacionTipoVacuna[1][0] == "Vacuna de Calendario" || listaVerificacionTipoVacuna[1][0] == "Vacuna Anual") && (listaVerificacionPandemia[1][0] != null))
                    {
                        errores.Add(string.Format("La {0} no puede ser asociada a la pandemia {1}", listaVerificacionTipoVacuna[1][0], listaVerificacionPandemia[1][0]));
                    }
                    if (listaVerificacionTipoVacuna[1][0] == "Vacuna Anual")
                    {
                        List<DescripcionVacunaCalendarioAnualDTO> vacunasAnuales = VacunaService.GetVacunasAnuales();
                        DescripcionVacunaCalendarioAnualDTO descripcion = vacunasAnuales.Where(x => x.Descripcion == model.Descripcion).FirstOrDefault();

                        if (descripcion == null)
                            errores.Add(string.Format("La descripción {0} no corresponde a una vacuna anual", model.Descripcion));
                    }
                    if (listaVerificacionPandemia[1][0] != model.Descripcion)
                    {
                        errores.Add(string.Format("La descripción de vacuna {0} no coincide con la pandemia {1}", model.Descripcion, listaVerificacionPandemia[1][0]));
                    }
                }
                else
                {
                    Pandemia noPandemia = await _context.Pandemia.Where(p => p.Descripcion == "No Pandemia").FirstOrDefaultAsync();
                    model.IdPandemia = noPandemia.Id;

                    if (listaVerificacionTipoVacuna[1][0] == "Vacuna de Pandemia")
                        errores.Add("El tipo de vacuna no corresponde");
                }

                errores = RolService.VerificarCredencialesUsuario(_context, model.EmailOperadorNacional, errores, "Operador Nacional");

                Vacuna vacunaExistente = VacunaService.GetVacunaByDescripcion(_context, model.Descripcion);
                if (vacunaExistente != null)
                    errores.Add(string.Format("La descripción de vacuna {0} está registrada en el sistema", model.Descripcion));

                if (errores.Count > 0)
                    responseVacunaDTO = new ResponseVacunaDTO("Rechazada", true, errores, model.EmailOperadorNacional, new VacunaDTO(0, model.Descripcion, model.IdTipoVacuna, null, model.IdPandemia, null, 0, null));
                else
                {
                    Vacuna vacuna = new Vacuna();
                    vacuna.Descripcion = model.Descripcion;
                    vacuna.IdPandemia = model.IdPandemia;
                    vacuna.IdTipoVacuna = model.IdTipoVacuna;
                    vacuna.CantidadDosis = 0;

                    _context.Vacuna.Add(vacuna);
                    await _context.SaveChangesAsync();

                    if (listaVerificacionTipoVacuna[1][0] == "Vacuna Anual")
                    {
                        List<ReglaDTO> listaReglas = new List<ReglaDTO>();
                        ReglaDTO reglaDTO = new ReglaDTO(0, model.Descripcion + " - Aplicar en meses de vacunación anual", "4,5,6,7,8,9", 0, 0, null, true, true);
                        listaReglas.Add(reglaDTO);

                        List<DosisDTO> listaDosis = new List<DosisDTO>();
                        DosisDTO dosisDTO = new DosisDTO(0, 0, "Dosis Anual", listaReglas);
                        listaDosis.Add(dosisDTO);

                        VacunaCalendarioAnualPandemiaDTO vacunaAnual = new VacunaCalendarioAnualPandemiaDTO(vacuna.Id, model.Descripcion, listaDosis);
                        VacunaCalendarioAnualPandemiaDTO vacAnu = VacunaService.RespaldarDosisReglasByVacuna(_context, vacunaAnual);
                        if (vacAnu != null)
                        {
                            vacuna.CantidadDosis = vacAnu.Dosis.Count;
                            responseVacunaDTO = new ResponseVacunaDTO("Aceptada", false, errores, model.EmailOperadorNacional,
                                new VacunaDTO(vacAnu.Id, vacAnu.Descripcion, model.IdTipoVacuna, listaVerificacionTipoVacuna[1][0], model.IdPandemia, null, vacAnu.Dosis.Count, vacAnu.Dosis));
                        }
                        else
                        {
                            errores.Add("Error de conexión con la base de datos");
                            responseVacunaDTO = new ResponseVacunaDTO("Rechazada", true, errores, model.EmailOperadorNacional, new VacunaDTO(0, model.Descripcion, model.IdTipoVacuna, null, model.IdPandemia, null, 0, null));
                        }
                    }
                    if (listaVerificacionTipoVacuna[1][0] == "Vacuna de Pandemia")
                    {
                        Pandemia pandemia = await _context.Pandemia.Where(p => p.Descripcion == model.Descripcion).FirstOrDefaultAsync();
                        if (pandemia != null)
                        {
                            int cantidadDosis = PandemiaService.CalcularCantidadDosisPandemia(pandemia);
                            List<DosisDTO> listaDosis = new List<DosisDTO>();
                            List<ReglaDTO> listaReglas = new List<ReglaDTO>();
                            ReglaDTO reglaDTO = new ReglaDTO(0, string.Format("{0} - Aplicar dosis cada {1} días", model.Descripcion, pandemia.IntervaloMinimoDias), null, pandemia.IntervaloMinimoDias, 0, "Anterior", true, true);
                            listaReglas.Add(reglaDTO);
                            
                            for (int i=0; i < cantidadDosis; i++)
                            {
                                DosisDTO dosisDTO = new DosisDTO(0, i, "Dosis " + (i + 1), listaReglas);
                                listaDosis.Add(dosisDTO);
                            }

                            VacunaCalendarioAnualPandemiaDTO vacunaPandemia = new VacunaCalendarioAnualPandemiaDTO(vacuna.Id, model.Descripcion, listaDosis);
                            VacunaCalendarioAnualPandemiaDTO vacPan = VacunaService.RespaldarDosisReglasByVacuna(_context, vacunaPandemia);
                            if (vacPan != null)
                            {
                                vacuna.CantidadDosis = vacPan.Dosis.Count;
                                responseVacunaDTO = new ResponseVacunaDTO("Aceptada", false, errores, model.EmailOperadorNacional,
                                    new VacunaDTO(vacPan.Id, vacPan.Descripcion, model.IdTipoVacuna, listaVerificacionTipoVacuna[1][0], model.IdPandemia, pandemia.Descripcion, vacPan.Dosis.Count, vacPan.Dosis));
                            }
                            else
                            {
                                errores.Add("Error de conexión con la base de datos");
                                responseVacunaDTO = new ResponseVacunaDTO("Rechazada", true, errores, model.EmailOperadorNacional, new VacunaDTO(0, model.Descripcion, model.IdTipoVacuna, null, model.IdPandemia, null, 0, null));
                            }
                        }
                    }

                    _context.Entry(vacuna).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }

                return Ok(responseVacunaDTO);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // DELETE: api/Vacunas/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Vacuna>> DeleteVacuna(int id)
        {
            var vacuna = await _context.Vacuna.FindAsync(id);
            if (vacuna == null)
            {
                return NotFound();
            }

            _context.Vacuna.Remove(vacuna);
            await _context.SaveChangesAsync();

            return vacuna;
        }
    }
}
