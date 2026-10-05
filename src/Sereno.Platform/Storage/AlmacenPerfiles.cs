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

        public PreferenciasVisuales ObtenerPreferencias()
        {
            lock (_candado)
                return CopiarPreferencias(_config.Visuales);
        }

        public void GuardarPreferencias(PreferenciasVisuales preferencias)
        {
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
        };

        private static PreferenciasVisuales CopiarPreferencias(PreferenciasVisuales origen) => new()
        {
            FuenteLectura = origen.FuenteLectura,
            AltoContraste = origen.AltoContraste,
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
