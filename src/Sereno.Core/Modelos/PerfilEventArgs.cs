using System;

namespace Sereno.Core.Modelos
{
    /// <summary>Datos de los eventos que las ventanas le envían a la aplicación.</summary>
    public sealed class PerfilEventArgs : EventArgs
    {
        public PerfilEventArgs(Perfil perfil) => Perfil = perfil;

        public Perfil Perfil { get; }
    }
}
