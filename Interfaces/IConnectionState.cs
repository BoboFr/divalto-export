using System;

namespace Divalto.Interfaces
{
    /// <summary>
    /// Interface pour gérer l'état de la connexion à la base de données
    /// </summary>
    public interface IConnectionState
    {
        /// <summary>
        /// Indique si la connexion est actuellement ouverte
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Message d'état ou d'erreur de la connexion
        /// </summary>
        string StatusMessage { get; }

        /// <summary>
        /// Nom du serveur auquel on est connecté
        /// </summary>
        string? ServerName { get; }

        /// <summary>
        /// Nom de la base de données connectée
        /// </summary>
        string? DatabaseName { get; }

        /// <summary>
        /// Événement levé lorsque l'état de la connexion change
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    }

    /// <summary>
    /// Arguments pour l'événement de changement d'état de connexion
    /// </summary>
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string? ServerName { get; set; }
        public string? DatabaseName { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }
}
