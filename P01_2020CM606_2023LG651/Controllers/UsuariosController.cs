using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using P01_2020CM606_2023LG651.Models;

namespace P01_2020CM606_2023LG651.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly ApplicationDbContext _contexto;

        public UsuariosController(ApplicationDbContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Endpoint que retorna la lista de usuarios
        /// </summary>
        [HttpGet]
        [Route("GetAllUsuarios")]
        public IActionResult Get()
        {
            var usuarios = (from u in _contexto.Usuarios
                            select u).ToList();
            return Ok(usuarios);
        }

        /// <summary>
        /// Endpoint que retorna un usuario por su id
        /// </summary>
        [HttpGet]
        [Route("GetUsuarioById/{id}")]
        public IActionResult GetUsuarioById(int id)
        {
            var usuario = (from u in _contexto.Usuarios
                           where u.Id == id
                           select u).FirstOrDefault();

            if (usuario == null) return NotFound();
            return Ok(usuario);
        }

        /// <summary>
        /// Endpoint para registrar un nuevo usuario
        /// </summary>
        [HttpPost]
        [Route("Register")]
        public IActionResult Register([FromBody] Usuario usuario)
        {
            if (usuario == null)
                return BadRequest("Datos de usuario inválidos");

            // Verificar si el correo ya existe
            var usuarioExistente = _contexto.Usuarios.FirstOrDefault(u => u.Correo == usuario.Correo);
            if (usuarioExistente != null)
                return BadRequest("El correo electrónico ya está registrado");

            _contexto.Usuarios.Add(usuario);
            _contexto.SaveChanges();
            return Ok("Usuario registrado exitosamente");
        }

        /// <summary>
        /// Endpoint para validar credenciales de usuario
        /// </summary>
        [HttpPost]
        [Route("Login")]
        public IActionResult Login([FromBody] LoginModel credentials)
        {
            var usuario = _contexto.Usuarios
                .FirstOrDefault(u => u.Correo == credentials.Correo &&
                                   u.Contrasena == credentials.Contrasena);

            if (usuario == null)
                return Unauthorized("Credenciales inválidas");

            return Ok(new { mensaje = "Login exitoso", usuario });
        }

        /// <summary>
        /// Endpoint para actualizar datos de usuario
        /// </summary>
        [HttpPut]
        [Route("UpdateUsuario")]
        public IActionResult UpdateUsuario([FromBody] Usuario usuario)
        {
            var usuarioExistente = _contexto.Usuarios.Find(usuario.Id);
            if (usuarioExistente == null) return NotFound();

            _contexto.Entry(usuarioExistente).CurrentValues.SetValues(usuario);
            _contexto.SaveChanges();
            return Ok("Usuario actualizado exitosamente");
        }

        /// <summary>
        /// Endpoint para eliminar un usuario
        /// </summary>
        [HttpDelete]
        [Route("DeleteUsuario/{id}")]
        public IActionResult DeleteUsuario(int id)
        {
            var usuario = _contexto.Usuarios.Find(id);
            if (usuario == null) return NotFound();

            _contexto.Usuarios.Remove(usuario);
            _contexto.SaveChanges();
            return Ok("Usuario eliminado exitosamente");
        }
    }

}