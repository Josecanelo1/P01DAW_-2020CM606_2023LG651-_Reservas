using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P01_2020CM606_2023LG651.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace P01_2020CM606_2023LG651.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservacionesController : ControllerBase
    {
        private readonly ApplicationDbContext _contexto;

        public ReservacionesController(ApplicationDbContext contexto)
        {
            _contexto = contexto;
        }

                /// <summary>
        /// Endpoint para crear una nueva reserva de espacio
        /// </summary>
        [HttpPost]
        [Route("CrearReserva")]
        public async Task<IActionResult> CrearReserva([FromBody] Reserva nuevaReserva)
        {
            try
            {
                // Verificar que el usuario existe
                var usuario = await _contexto.Usuarios.FindAsync(nuevaReserva.UsuarioId);
                if (usuario == null)
                {
                    return NotFound($"No se encontró el usuario con ID {nuevaReserva.UsuarioId}");
                }
        
                // Verificar que el espacio existe
                var espacio = await _contexto.EspaciosParque
                    .Include(e => e.Sucursal)
                    .FirstOrDefaultAsync(e => e.Id == nuevaReserva.EspacioId);
        
                if (espacio == null)
                {
                    return NotFound($"No se encontró el espacio con ID {nuevaReserva.EspacioId}");
                }
        
                // Verificar que el espacio esté disponible
                if (espacio.Estado != "Disponible")
                {
                    return BadRequest($"El espacio {espacio.Numero} en {espacio.Sucursal.Nombre} no está disponible");
                }
        
                // Calcular hora de finalización para la nueva reserva
                var horaFin = nuevaReserva.HoraInicio.Add(TimeSpan.FromHours(nuevaReserva.CantidadHoras));
        
                // Primero obtenemos todas las reservas para ese espacio y fecha sin filtrar por hora
                var reservasExistentes = await _contexto.Reservas
                    .Where(r => r.EspacioId == nuevaReserva.EspacioId && 
                           r.Fecha.Date == nuevaReserva.Fecha.Date)
                    .ToListAsync();
        
                // Ahora verificamos el solapamiento de horas en memoria
                var reservaConflicto = reservasExistentes.FirstOrDefault(r => 
                    (r.HoraInicio <= nuevaReserva.HoraInicio && 
                     r.HoraInicio.Add(TimeSpan.FromHours(r.CantidadHoras)) > nuevaReserva.HoraInicio) ||
                    (r.HoraInicio < horaFin && 
                     r.HoraInicio >= nuevaReserva.HoraInicio));
        
                if (reservaConflicto != null)
                {
                    return BadRequest("El espacio ya está reservado en ese horario");
                }
        
                // Guardar la reserva
                await _contexto.Reservas.AddAsync(nuevaReserva);
                await _contexto.SaveChangesAsync();
        
                // Actualizar el estado del espacio temporalmente
                espacio.Estado = "Reservado";
                await _contexto.SaveChangesAsync();
        
                return Ok(new { mensaje = "Reserva creada exitosamente", reserva = nuevaReserva });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar la reserva: {ex.Message}");
            }
        }

                /// <summary>
        /// Endpoint para obtener todas las reservas activas de un usuario
        /// </summary>
        [HttpGet]
        [Route("ReservasActivasUsuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerReservasActivasUsuario(int usuarioId)
        {
            try
            {
                // Verificar que el usuario existe
                var usuario = await _contexto.Usuarios.FindAsync(usuarioId);
                if (usuario == null)
                {
                    return NotFound($"No se encontró el usuario con ID {usuarioId}");
                }
        
                // Obtener fecha y hora actual
                var ahora = DateTime.Now;
        
                // Primero obtenemos las reservas con carga anticipada y luego filtramos en memoria
                var todasReservas = await _contexto.Reservas
                    .Where(r => r.UsuarioId == usuarioId)
                    .Include(r => r.Espacio)
                    .ThenInclude(e => e.Sucursal)
                    .ToListAsync();
                
                // Ahora filtramos en memoria (client-side)
                var reservasActivas = todasReservas
                    .Where(r => r.Fecha > ahora.Date || 
                           (r.Fecha == ahora.Date && 
                            r.HoraInicio.Add(TimeSpan.FromHours(r.CantidadHoras)) > ahora.TimeOfDay))
                    .Select(r => new
                    {
                        r.Id,
                        r.Fecha,
                        r.HoraInicio,
                        r.CantidadHoras,
                        HoraFin = r.HoraInicio.Add(TimeSpan.FromHours(r.CantidadHoras)),
                        EspacioNumero = r.Espacio.Numero,
                        Ubicacion = r.Espacio.Ubicacion,
                        Sucursal = r.Espacio.Sucursal.Nombre,
                        CostoPorHora = r.Espacio.CostoPorHora,
                        CostoTotal = r.Espacio.CostoPorHora * r.CantidadHoras
                    })
                    .ToList();
        
                if (!reservasActivas.Any())
                {
                    return NotFound($"El usuario no tiene reservas activas");
                }
        
                return Ok(reservasActivas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al consultar reservas: {ex.Message}");
            }
        }

        /// <summary>
        /// Endpoint para cancelar una reserva
        /// </summary>
        [HttpDelete]
        [Route("CancelarReserva/{reservaId}/{usuarioId}")]
        public async Task<IActionResult> CancelarReserva(int reservaId, int usuarioId)
        {
            try
            {
                // Buscar la reserva
                var reserva = await _contexto.Reservas
                    .Include(r => r.Espacio)
                    .FirstOrDefaultAsync(r => r.Id == reservaId);

                if (reserva == null)
                {
                    return NotFound($"No se encontró la reserva con ID {reservaId}");
                }

                // Verificar que la reserva pertenece al usuario
                if (reserva.UsuarioId != usuarioId)
                {
                    return Unauthorized("No tiene permisos para cancelar esta reserva");
                }

                // Verificar que la reserva es para una fecha futura o el mismo día
                if (reserva.Fecha < DateTime.Now.Date)
                {
                    return BadRequest("No se puede cancelar una reserva de fecha anterior");
                }

                // Si es el mismo día, verificar que no haya pasado la hora
                if (reserva.Fecha == DateTime.Now.Date && reserva.HoraInicio < DateTime.Now.TimeOfDay)
                {
                    return BadRequest("No se puede cancelar una reserva cuando ya ha pasado la hora de inicio");
                }

                // Restaurar el estado del espacio a "Disponible"
                if (reserva.Espacio != null)
                {
                    reserva.Espacio.Estado = "Disponible";
                }

                // Eliminar la reserva
                _contexto.Reservas.Remove(reserva);
                await _contexto.SaveChangesAsync();

                return Ok(new { mensaje = "Reserva cancelada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al cancelar la reserva: {ex.Message}");
            }
        }

                /// <summary>
        /// Endpoint para obtener todos los espacios disponibles por sucursal, fecha y hora
        /// </summary>
        [HttpGet]
        [Route("EspaciosDisponibles")]
        public async Task<IActionResult> ObtenerEspaciosDisponibles(int sucursalId, DateTime fecha, TimeSpan horaInicio, int cantidadHoras)
        {
            try
            {
                // Verificar que la sucursal existe
                var sucursal = await _contexto.Sucursales.FindAsync(sucursalId);
                if (sucursal == null)
                {
                    return NotFound($"No se encontró la sucursal con ID {sucursalId}");
                }
        
                // Calcular hora de fin
                var horaFin = horaInicio.Add(TimeSpan.FromHours(cantidadHoras));
        
                // Primero obtenemos las reservas para la fecha y la sucursal específica
                var todasReservas = await _contexto.Reservas
                    .Where(r => r.Fecha.Date == fecha.Date)
                    .Include(r => r.Espacio)
                    .Where(r => r.Espacio.SucursalId == sucursalId)
                    .ToListAsync();
        
                // Filtramos en memoria para encontrar los espacios con conflictos horarios
                var espaciosOcupados = todasReservas
                    .Where(r => (r.HoraInicio <= horaInicio && 
                                 r.HoraInicio.Add(TimeSpan.FromHours(r.CantidadHoras)) > horaInicio) ||
                                (r.HoraInicio < horaFin && r.HoraInicio >= horaInicio))
                    .Select(r => r.EspacioId)
                    .Distinct()
                    .ToList();
        
                // Obtenemos todos los espacios de la sucursal que estén disponibles
                var espaciosDisponibles = await _contexto.EspaciosParque
                    .Where(e => e.SucursalId == sucursalId && 
                               e.Estado == "Disponible" &&
                               !espaciosOcupados.Contains(e.Id))
                    .ToListAsync();
        
                // Formateamos la respuesta
                var resultado = espaciosDisponibles.Select(e => new
                {
                    e.Id,
                    e.Numero,
                    e.Ubicacion,
                    e.CostoPorHora,
                    CostoTotal = e.CostoPorHora * cantidadHoras
                }).ToList();
        
                if (!resultado.Any())
                {
                    return NotFound($"No hay espacios disponibles en la sucursal para la fecha y hora seleccionadas");
                }
        
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al consultar espacios disponibles: {ex.Message}");
            }
        }
    }
}