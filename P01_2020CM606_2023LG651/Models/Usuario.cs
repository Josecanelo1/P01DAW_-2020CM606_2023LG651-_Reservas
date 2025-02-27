using System;
using System.ComponentModel.DataAnnotations;

namespace P01_2020CM606_2023LG651.Models;

public class Usuario
{
    [Key]
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Correo { get; set; }
    public string Telefono { get; set; }
    public string Contrasena { get; set; }
    public string Rol { get; set; }
}
