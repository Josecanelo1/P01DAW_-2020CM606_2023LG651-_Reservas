using System;
using System.ComponentModel.DataAnnotations;

namespace P01_2020CM606_2023LG651.Models;

public class EspacioParque
{
    [Key]
    public int Id { get; set; }
    public int SucursalId { get; set; }
    public int Numero { get; set; }
    public string Ubicacion { get; set; }
    public decimal CostoPorHora { get; set; }
    public string Estado { get; set; } // Disponible / Ocupado

}
