using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P01_2020CM606_2023LG651.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace P01_2020CM606_2023LG651.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EspaciosParqueController : ControllerBase
    {
        private readonly ApplicationDbContext _contexto;

        public EspaciosParqueController(ApplicationDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Endpoint que retorna todas las sucursales
        /// </summary>
        [HttpGet]
        [Route("GetAllSucursales")]
        public async Task<IActionResult> GetAllSucursales()
        {
            var sucursales = await _contexto.Sucursales.ToListAsync();
            return Ok(sucursales);
        }

        /// <summary>
        /// Endpoint que retorna una sucursal por su id
        /// </summary>
        [HttpGet]
        [Route("GetSucursalById/{id}")]
        public async Task<IActionResult> GetSucursalById(int id)
        {
            var sucursal = await _contexto.Sucursales.FindAsync(id);
            if (sucursal == null) return NotFound($"No se encontró la sucursal con ID: {id}");

            return Ok(sucursal);
        }

        /// <summary>
        /// Endpoint que crea una nueva sucursal
        /// </summary>
        [HttpPost]
        [Route("CreateSucursal")]
        public async Task<IActionResult> CreateSucursal([FromBody] Sucursal sucursal)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _contexto.Sucursales.AddAsync(sucursal);
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSucursalById), new { id = sucursal.Id }, sucursal);
        }

        /// <summary>
        /// Endpoint que actualiza una sucursal existente
        /// </summary>
        [HttpPut]
        [Route("UpdateSucursal/{id}")]
        public async Task<IActionResult> UpdateSucursal(int id, [FromBody] Sucursal sucursal)
        {
            if (id != sucursal.Id)
                return BadRequest("El ID de la sucursal no coincide");

            _contexto.Entry(sucursal).State = EntityState.Modified;

            try
            {
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SucursalExists(id))
                    return NotFound($"No se encontró la sucursal con ID: {id}");
                else
                    throw;
            }

            return Ok("Sucursal actualizada correctamente");
        }

        /// <summary>
        /// Endpoint que elimina una sucursal por su id
        /// </summary>
        [HttpDelete]
        [Route("DeleteSucursal/{id}")]
        public async Task<IActionResult> DeleteSucursal(int id)
        {
            var sucursal = await _contexto.Sucursales.FindAsync(id);
            if (sucursal == null)
                return NotFound($"No se encontró la sucursal con ID: {id}");

            // Verificar si hay espacios de parqueo asociados
            var espaciosAsociados = await _contexto.EspaciosParque.AnyAsync(e => e.SucursalId == id);
            if (espaciosAsociados)
                return BadRequest("No se puede eliminar la sucursal porque tiene espacios de parqueo asociados");

            _contexto.Sucursales.Remove(sucursal);
            await _contexto.SaveChangesAsync();

            return Ok("Sucursal eliminada correctamente");
        }

        private bool SucursalExists(int id)
        {
            return _contexto.Sucursales.Any(e => e.Id == id);
        }

        /// <summary>
        /// Endpoint que retorna todos los espacios de parqueo
        /// </summary>
        [HttpGet]
        [Route("GetAllEspacios")]
        public async Task<IActionResult> GetAllEspacios()
        {
            var espacios = await _contexto.EspaciosParque
                .Include(e => e.Sucursal)
                .ToListAsync();

            return Ok(espacios);
        }

        /// <summary>
        /// Endpoint que retorna un espacio de parqueo por su id
        /// </summary>
        [HttpGet]
        [Route("GetEspacioById/{id}")]
        public async Task<IActionResult> GetEspacioById(int id)
        {
            var espacio = await _contexto.EspaciosParque
                .Include(e => e.Sucursal)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (espacio == null)
                return NotFound($"No se encontró el espacio de parqueo con ID: {id}");

            return Ok(espacio);
        }

        /// <summary>
        /// Endpoint que crea un nuevo espacio de parqueo por sucursal
        /// </summary>
        [HttpPost]
        [Route("CreateEspacio")]
        public async Task<IActionResult> CreateEspacio([FromBody] EspacioParque espacio)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verificar si la sucursal existe
            var sucursal = await _contexto.Sucursales.FindAsync(espacio.SucursalId);
            if (sucursal == null)
                return NotFound($"No se encontró la sucursal con ID: {espacio.SucursalId}");

            // Verificar que el número de espacio no esté duplicado en la misma sucursal
            var existeEspacio = await _contexto.EspaciosParque
                .AnyAsync(e => e.SucursalId == espacio.SucursalId && e.Numero == espacio.Numero);

            if (existeEspacio)
                return BadRequest($"Ya existe un espacio con el número {espacio.Numero} en esta sucursal");

            // Por defecto, crear el espacio como disponible si no viene especificado
            if (string.IsNullOrEmpty(espacio.Estado))
                espacio.Estado = "Disponible";

            await _contexto.EspaciosParque.AddAsync(espacio);
            await _contexto.SaveChangesAsync();

            // Actualizar el número de espacios en la sucursal
            sucursal.NumeroEspacios++;
            await _contexto.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEspacioById), new { id = espacio.Id }, espacio);
        }

        /// <summary>
        /// Endpoint que actualiza un espacio de parqueo existente
        /// </summary>
        [HttpPut]
        [Route("UpdateEspacio/{id}")]
        public async Task<IActionResult> UpdateEspacio(int id, [FromBody] EspacioParque espacio)
        {
            if (id != espacio.Id)
                return BadRequest("El ID del espacio no coincide");

            // Verificar si el espacio existe
            var espacioExistente = await _contexto.EspaciosParque.FindAsync(id);
            if (espacioExistente == null)
                return NotFound($"No se encontró el espacio de parqueo con ID: {id}");

            // Si se cambia la sucursal, actualizar contadores
            if (espacioExistente.SucursalId != espacio.SucursalId)
            {
                var sucursalAnterior = await _contexto.Sucursales.FindAsync(espacioExistente.SucursalId);
                var nuevaSucursal = await _contexto.Sucursales.FindAsync(espacio.SucursalId);

                if (nuevaSucursal == null)
                    return NotFound($"No se encontró la nueva sucursal con ID: {espacio.SucursalId}");

                if (sucursalAnterior != null)
                {
                    sucursalAnterior.NumeroEspacios--;
                    nuevaSucursal.NumeroEspacios++;
                }
            }

            _contexto.Entry(espacioExistente).State = EntityState.Detached;
            _contexto.Entry(espacio).State = EntityState.Modified;

            try
            {
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EspacioExists(id))
                    return NotFound($"No se encontró el espacio de parqueo con ID: {id}");
                else
                    throw;
            }

            return Ok("Espacio de parqueo actualizado correctamente");
        }

        /// <summary>
        /// Endpoint que elimina un espacio de parqueo por su id
        /// </summary>
        [HttpDelete]
        [Route("DeleteEspacio/{id}")]
        public async Task<IActionResult> DeleteEspacio(int id)
        {
            var espacio = await _contexto.EspaciosParque.FindAsync(id);
            if (espacio == null)
                return NotFound($"No se encontró el espacio de parqueo con ID: {id}");

            // Verificar si hay reservas asociadas
            var reservasAsociadas = await _contexto.Reservas.AnyAsync(r => r.EspacioId == id);
            if (reservasAsociadas)
                return BadRequest("No se puede eliminar el espacio porque tiene reservas asociadas");

            // Actualizar el contador de espacios en la sucursal
            var sucursal = await _contexto.Sucursales.FindAsync(espacio.SucursalId);
            if (sucursal != null)
            {
                sucursal.NumeroEspacios--;
            }

            _contexto.EspaciosParque.Remove(espacio);
            await _contexto.SaveChangesAsync();

            return Ok("Espacio de parqueo eliminado correctamente");
        }

        /// <summary>
        /// Endpoint que retorna todos los espacios disponibles para reservar por día
        /// </summary>
        [HttpGet]
        [Route("GetEspaciosDisponiblesPorDia")]
        public async Task<IActionResult> GetEspaciosDisponiblesPorDia([FromQuery] DateTime fecha)
        {
            // Obtener todos los espacios disponibles
            var espaciosDisponibles = await _contexto.EspaciosParque
                .Where(e => e.Estado == "Disponible")
                .Include(e => e.Sucursal)
                .ToListAsync();

            // Obtener las reservas para ese día
            var reservasDelDia = await _contexto.Reservas
                .Where(r => r.Fecha.Date == fecha.Date)
                .ToListAsync();

            // Filtrar espacios que no están reservados para ese día
            var espaciosNoReservados = espaciosDisponibles
                .Where(e => !reservasDelDia.Any(r => r.EspacioId == e.Id))
                .ToList();

            return Ok(espaciosNoReservados);
        }

        /// <summary>
        /// Endpoint que retorna todos los espacios de parqueo por sucursal
        /// </summary>
        [HttpGet]
        [Route("GetEspaciosPorSucursal/{sucursalId}")]
        public async Task<IActionResult> GetEspaciosPorSucursal(int sucursalId)
        {
            var sucursal = await _contexto.Sucursales.FindAsync(sucursalId);
            if (sucursal == null)
                return NotFound($"No se encontró la sucursal con ID: {sucursalId}");

            var espacios = await _contexto.EspaciosParque
                .Where(e => e.SucursalId == sucursalId)
                .ToListAsync();

            return Ok(espacios);
        }

        private bool EspacioExists(int id)
        {
            return _contexto.EspaciosParque.Any(e => e.Id == id);
        }

        /// <summary>
        /// Endpoint que retorna todos los espacios reservados por día de todas las sucursales
        /// </summary>
        [HttpGet]
        [Route("GetEspaciosReservadosPorDia")]
        public async Task<IActionResult> GetEspaciosReservadosPorDia([FromQuery] DateTime fecha)
        {
            var espaciosReservados = await _contexto.Reservas
                .Where(r => r.Fecha.Date == fecha.Date)
                .Join(_contexto.EspaciosParque,
                      r => r.EspacioId,
                      e => e.Id,
                      (r, e) => new
                      {
                          ReservaId = r.Id,
                          Fecha = r.Fecha,
                          HoraInicio = r.HoraInicio,
                          CantidadHoras = r.CantidadHoras,
                          EspacioId = e.Id,
                          NumeroEspacio = e.Numero,
                          SucursalId = e.SucursalId,
                          Ubicacion = e.Ubicacion,
                          CostoPorHora = e.CostoPorHora
                      })
                .Join(_contexto.Sucursales,
                      e => e.SucursalId,
                      s => s.Id,
                      (e, s) => new
                      {
                          ReservaId = e.ReservaId,
                          Fecha = e.Fecha,
                          HoraInicio = e.HoraInicio,
                          CantidadHoras = e.CantidadHoras,
                          EspacioId = e.EspacioId,
                          NumeroEspacio = e.NumeroEspacio,
                          Ubicacion = e.Ubicacion,
                          CostoPorHora = e.CostoPorHora,
                          SucursalId = s.Id,
                          NombreSucursal = s.Nombre,
                          DireccionSucursal = s.Direccion
                      })
                .ToListAsync();

            if (!espaciosReservados.Any())
                return NotFound($"No hay espacios reservados para la fecha {fecha.ToShortDateString()}");

            return Ok(espaciosReservados);
        }

        /// <summary>
        /// Endpoint que retorna todos los espacios reservados entre dos fechas de una sucursal específica
        /// </summary>
        [HttpGet]
        [Route("GetEspaciosReservadosEntreFechasPorSucursal")]
        public async Task<IActionResult> GetEspaciosReservadosEntreFechasPorSucursal(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] int sucursalId)
        {
            // Verificar que la sucursal exista
            var sucursal = await _contexto.Sucursales.FindAsync(sucursalId);
            if (sucursal == null)
                return NotFound($"No se encontró la sucursal con ID: {sucursalId}");

            // Obtener los espacios de la sucursal
            var espaciosSucursal = await _contexto.EspaciosParque
                .Where(e => e.SucursalId == sucursalId)
                .Select(e => e.Id)
                .ToListAsync();

            if (!espaciosSucursal.Any())
                return NotFound($"La sucursal con ID {sucursalId} no tiene espacios registrados");

            // Obtener las reservas de esos espacios entre las fechas
            var reservas = await _contexto.Reservas
                .Where(r => r.Fecha.Date >= fechaInicio.Date &&
                            r.Fecha.Date <= fechaFin.Date &&
                            espaciosSucursal.Contains(r.EspacioId))
                .Join(_contexto.EspaciosParque,
                      r => r.EspacioId,
                      e => e.Id,
                      (r, e) => new
                      {
                          ReservaId = r.Id,
                          Fecha = r.Fecha,
                          HoraInicio = r.HoraInicio,
                          CantidadHoras = r.CantidadHoras,
                          EspacioId = e.Id,
                          NumeroEspacio = e.Numero,
                          Ubicacion = e.Ubicacion,
                          CostoPorHora = e.CostoPorHora,
                          SucursalId = e.SucursalId
                      })
                .ToListAsync();

            if (!reservas.Any())
                return NotFound($"No hay reservas para la sucursal {sucursal.Nombre} entre {fechaInicio.ToShortDateString()} y {fechaFin.ToShortDateString()}");

            return Ok(new
            {
                Sucursal = sucursal.Nombre,
                Periodo = $"{fechaInicio.ToShortDateString()} - {fechaFin.ToShortDateString()}",
                Reservas = reservas
            });
        }

    }
}