using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Sereno.Core.Acceso;
using Sereno.Core.Modelos;
using Sereno.Core.Timing;

namespace Sereno.Platform.Storage
{
    /// <summary>
    /// Guarda y lee los perfiles locales. Estructura en disco:
    ///
    ///   %APPDATA%\Sereno\
    ///     config.json
    ///     Perfiles\
    ///       lucia-3f9a1c\
    ///         perfil.json
    ///         clave-recuperacion.txt
    ///
    /// Cada persona tiene su propia carpeta; ahí se guarda también su clave de recuperación.
    /// </summary>
    public sealed class AlmacenPerfiles
    {
        public const string ArchivoPerfil = "perfil.json";
        public const string ArchivoClave = "clave-recuperacion.txt";
        private const string NombreCompartido = "Compartido";

        private static readonly JsonSerializerOptions OpcionesJson = new() { WriteIndented = true };

        private readonly string _carpetaPerfiles;
        private readonly string _archivoConfig;
        private readonly object _candado = new();
        private readonly List<Perfil> _perfiles = new();
        private Configuracion _config = new();

        public AlmacenPerfiles(string carpetaBase)
        {
            _carpetaPerfiles = Path.Combine(carpetaBase, "Perfiles");
            _archivoConfig = Path.Combine(carpetaBase, "config.json");
        }

        /// <summary>Perfiles con nombre, sin incluir el compartido.</summary>
        public IReadOnlyList<Perfil> Perfiles
        {
            get
            {
                lock (_candado)
                    return _perfiles.Where(p => !p.EsCompartido)
                                    .OrderBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase)
                                    .ToList();
            }
        }

        /// <summary>Lee todas las carpetas de perfil. Se llama una vez, desde el splash.</summary>
        public void Cargar()
        {
            Directory.CreateDirectory(_carpetaPerfiles);
            var leidos = new List<Perfil>();

            foreach (string carpeta in Directory.EnumerateDirectories(_carpetaPerfiles))
            {
                string archivo = Path.Combine(carpeta, ArchivoPerfil);
                if (!File.Exists(archivo)) continue;

                try
                {
                    var perfil = JsonSerializer.Deserialize<Perfil>(File.ReadAllText(archivo, Encoding.UTF8));
                    if (perfil is null || string.IsNullOrWhiteSpace(perfil.Nombre)) continue;
                    perfil.Carpeta = carpeta;
                    leidos.Add(perfil);
                }
                catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
                {
                    // Un perfil dañado no debe impedir que el resto de las personas entre.
                }
            }

            Configuracion config = new();
            try
            {
                if (File.Exists(_archivoConfig))
                    config = JsonSerializer.Deserialize<Configuracion>(File.ReadAllText(_archivoConfig, Encoding.UTF8)) ?? new();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                config = new();
            }

            lock (_candado)
            {
                _perfiles.Clear();
                _perfiles.AddRange(leidos);
                _config = config;
            }
        }

        public Perfil? UltimoPerfil()
        {
            lock (_candado)
                return _perfiles.FirstOrDefault(p => !p.EsCompartido && p.Id == _config.UltimoPerfilId);
        }

        public bool ExisteNombre(string nombre)
        {
            lock (_candado)
                return _perfiles.Any(p => !p.EsCompartido &&
                    string.Equals(p.Nombre, nombre.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>Crea el perfil y devuelve la clave de recuperación sin formato (16 caracteres).</summary>
        public (Perfil Perfil, string Clave) Crear(string nombre, string contrasena)
        {
            string clave = ClaveRecuperacion.Generar();
            var (hashContrasena, salContrasena) = Hasher.Crear(contrasena);
            var (hashClave, salClave) = Hasher.Crear(clave);

            var perfil = new Perfil
            {
                Nombre = nombre.Trim(),
                HashContrasena = hashContrasena,
                SalContrasena = salContrasena,
                HashClave = hashClave,
                SalClave = salClave,
                Iteraciones = Hasher.Iteraciones,
            };
            perfil.Carpeta = Path.Combine(_carpetaPerfiles, NombreDeCarpeta(perfil));

            Guardar(perfil);
            lock (_candado) _perfiles.Add(perfil);
            return (perfil, clave);
        }

        public bool VerificarContrasena(Perfil perfil, string contrasena) =>
            Hasher.Verificar(contrasena, perfil.HashContrasena, perfil.SalContrasena, perfil.Iteraciones);

        public bool VerificarClave(Perfil perfil, string claveNormalizada) =>
            Hasher.Verificar(claveNormalizada, perfil.HashClave, perfil.SalClave, perfil.Iteraciones);

        /// <summary>Reemplaza la contraseña. La clave de recuperación se mantiene.</summary>
        public void CambiarContrasena(Perfil perfil, string nueva)
        {
            var (hash, sal) = Hasher.Crear(nueva);
            perfil.HashContrasena = hash;
            perfil.SalContrasena = sal;
            perfil.Iteraciones = Hasher.Iteraciones;
            Guardar(perfil);
        }

        /// <summary>Escribe la clave como .txt en la carpeta del perfil y devuelve la ruta.</summary>
        public string GuardarClaveEnArchivo(Perfil perfil, string clave)
        {
            Directory.CreateDirectory(perfil.Carpeta);
            string ruta = Path.Combine(perfil.Carpeta, ArchivoClave);
            string contenido =
                "Sereno - Clave de recuperación" + Environment.NewLine +
                $"Perfil: {perfil.Nombre}" + Environment.NewLine +
                $"Creada: {DateTime.Now:dd/MM/yyyy HH:mm}" + Environment.NewLine + Environment.NewLine +
                ClaveRecuperacion.Formatear(clave) + Environment.NewLine + Environment.NewLine +
                "Usala en \"Olvidé mi contraseña\" para definir una contraseña nueva." + Environment.NewLine;
            EscribirSeguro(ruta, contenido);
            return ruta;
        }

        public void Guardar(Perfil perfil)
        {
            Directory.CreateDirectory(perfil.Carpeta);
            EscribirSeguro(Path.Combine(perfil.Carpeta, ArchivoPerfil),
                JsonSerializer.Serialize(perfil, OpcionesJson));
        }

        public void RecordarUltimo(Perfil perfil)
        {
            if (perfil.EsCompartido) return;
            Configuracion copia;
            lock (_candado)
            {
                _config.UltimoPerfilId = perfil.Id;
                copia = CopiarConfiguracion();
            }
            try
            {
                EscribirConfiguracion(copia);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Recordar el último perfil es una comodidad: si falla, la app sigue funcionando.
            }
        }

        public (int MinutosBloque, int MinutosPausa) ObtenerDuraciones()
        {
            lock (_candado)
                return (_config.MinutosBloque, _config.MinutosPausa);
        }

        public void GuardarDuraciones(int minutosBloque, int minutosPausa)
        {
            if (!Duraciones.EsBloqueValido(minutosBloque))
                throw new ArgumentOutOfRangeException(nameof(minutosBloque));
            if (!Duraciones.EsPausaValida(minutosPausa))
                throw new ArgumentOutOfRangeException(nameof(minutosPausa));

            Configuracion copia;
            lock (_candado)
            {
                _config.MinutosBloque = minutosBloque;
                _config.MinutosPausa = minutosPausa;
                copia = CopiarConfiguracion();
            }
            EscribirConfiguracion(copia);
        }

        public PreferenciasAviso ObtenerPreferenciasAviso()
        {
            lock (_candado)
                return CopiarAviso(_config.Aviso);
        }

        public void GuardarPreferenciasAviso(PreferenciasAviso preferencias)
        {
            Configuracion copia;
            lock (_candado)
            {
                if (!PreferenciasAviso.EsSegundosValido(preferencias.SegundosVisible))
                    throw new ArgumentOutOfRangeException(nameof(preferencias), "El aviso debe durar entre 10 y 60 segundos.");
                _config.Aviso = CopiarAviso(preferencias);
                copia = CopiarConfiguracion();
            }
            EscribirConfiguracion(copia);
        }

        /// <summary>
        /// Texto legible con perfiles (sin hashes ni claves) y configuración. Es lo que la persona
        /// puede llevarse o revisar; no incluye nada que sirva para entrar a un perfil.
        /// </summary>
        public string ExportarTexto()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sereno · Exportación de datos");
            sb.AppendLine($"Generada: {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine();
            sb.AppendLine("Perfiles");
            lock (_candado)
            {
                foreach (Perfil p in _perfiles.Where(p => !p.EsCompartido))
                    sb.AppendLine($"  - {p.Nombre} (creado el {p.CreadoEn:dd/MM/yyyy}, abrir sin contraseña: {(p.AbrirSinContrasena ? "sí" : "no")})");
                sb.AppendLine();
                sb.AppendLine("Configuración");
                sb.AppendLine($"  - Bloque: {_config.MinutosBloque} minutos");
                sb.AppendLine($"  - Pausa: {_config.MinutosPausa} minutos");
                sb.AppendLine($"  - Canal del aviso: {_config.Aviso.Canal}");
            }
            return sb.ToString();
        }

        /// <summary>Borra toda la carpeta de Sereno. Es irreversible: quien llama debe confirmar antes.</summary>
        public void BorrarTodo()
        {
            string carpetaBase = Path.GetDirectoryName(_archivoConfig)!;
            if (Directory.Exists(carpetaBase))
                Directory.Delete(carpetaBase, recursive: true);
            lock (_candado)
            {
                _perfiles.Clear();
                _config = new Configuracion();
            }
        }

        public PreferenciasVisuales ObtenerPreferencias()
        {
            lock (_candado)
                return CopiarPreferencias(_config.Visuales);
        }

        public void GuardarPreferencias(PreferenciasVisuales preferencias)
        {
            if (!PreferenciasVisuales.EsEscalaValida(preferencias.Escala))
                throw new ArgumentOutOfRangeException(nameof(preferencias), "La escala debe ser 100, 125 o 150.");

            Configuracion copia;
            lock (_candado)
            {
                _config.Visuales = CopiarPreferencias(preferencias);
                copia = CopiarConfiguracion();
            }
            EscribirConfiguracion(copia);
        }

        private Configuracion CopiarConfiguracion() => new()
        {
            UltimoPerfilId = _config.UltimoPerfilId,
            MinutosBloque = _config.MinutosBloque,
            MinutosPausa = _config.MinutosPausa,
            Visuales = CopiarPreferencias(_config.Visuales),
            Aviso = CopiarAviso(_config.Aviso),
        };

        private static PreferenciasAviso CopiarAviso(PreferenciasAviso origen) => new()
        {
            Canal = origen.Canal,
            BajoEstimulo = origen.BajoEstimulo,
            SegundosVisible = origen.SegundosVisible,
        };

        private static PreferenciasVisuales CopiarPreferencias(PreferenciasVisuales origen) => new()
        {
            FuenteLectura = origen.FuenteLectura,
            AltoContraste = origen.AltoContraste,
            EspaciadoAmplio = origen.EspaciadoAmplio,
            Escala = origen.Escala,
        };

        private void EscribirConfiguracion(Configuracion config)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_archivoConfig)!);
            EscribirSeguro(_archivoConfig, JsonSerializer.Serialize(config, OpcionesJson));
        }

        /// <summary>Perfil sin contraseña para "Usar Sereno sin perfil". Se crea la primera vez.</summary>
        public Perfil ObtenerCompartido()
        {
            lock (_candado)
            {
                var existente = _perfiles.FirstOrDefault(p => p.EsCompartido);
                if (existente is not null) return existente;
            }

            var compartido = new Perfil { Nombre = NombreCompartido, EsCompartido = true };
            compartido.Carpeta = Path.Combine(_carpetaPerfiles, "_compartido");
            Guardar(compartido);
            lock (_candado) _perfiles.Add(compartido);
            return compartido;
        }

        /// <summary>Cambia el nombre del perfil. No toca la contraseña ni la clave de recuperación.</summary>
        public void RenombrarPerfil(Perfil perfil, string nuevoNombre)
        {
            string nombre = nuevoNombre.Trim();
            if (nombre.Length == 0)
                throw new ArgumentException("El nombre no puede estar vacío.", nameof(nuevoNombre));

            lock (_candado)
            {
                bool duplicado = _perfiles.Any(p => !p.EsCompartido && p.Id != perfil.Id &&
                    string.Equals(p.Nombre, nombre, StringComparison.CurrentCultureIgnoreCase));
                if (duplicado)
                    throw new InvalidOperationException("Ya hay un perfil con ese nombre en esta computadora.");
                perfil.Nombre = nombre;
            }
            Guardar(perfil);
        }

        /// <summary>Borra la carpeta completa del perfil (perfil.json y la clave de recuperación). Es irreversible.</summary>
        public void EliminarPerfil(Perfil perfil)
        {
            if (perfil.EsCompartido)
                throw new InvalidOperationException("El perfil compartido no se puede eliminar.");

            if (Directory.Exists(perfil.Carpeta))
                Directory.Delete(perfil.Carpeta, recursive: true);

            lock (_candado)
                _perfiles.RemoveAll(p => p.Id == perfil.Id);
        }

        private static string NombreDeCarpeta(Perfil perfil)
        {
            var sb = new StringBuilder();
            foreach (char c in perfil.Nombre.ToLowerInvariant())
            {
                if (sb.Length >= 30) break;
                sb.Append(char.IsLetterOrDigit(c) ? c : '-');
            }
            string baseNombre = sb.ToString().Trim('-');
            if (baseNombre.Length == 0) baseNombre = "perfil";
            return $"{baseNombre}-{perfil.Id[..6]}";
        }

        /// <summary>Escribe en un temporal y luego reemplaza, para no dejar archivos a medio escribir.</summary>
        private static void EscribirSeguro(string ruta, string contenido)
        {
            string temporal = ruta + ".tmp";
            File.WriteAllText(temporal, contenido, new UTF8Encoding(false));
            File.Move(temporal, ruta, overwrite: true);
        }
    }
}
