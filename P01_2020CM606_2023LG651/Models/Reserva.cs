using System;
using System.ComponentModel.DataAnnotations;

namespace P01_2020CM606_2023LG651.Models;

public class Reserva
{
    [Key]
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int EspacioId { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public int CantidadHoras { get; set; }
}
