using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;  // Utiliser System.Data.SqlClient au lieu de Microsoft.Data.SqlClient (plus stable)
using Divalto.Interfaces;
using Divalto.Models;

namespace Divalto.Services
{
    /// <summary>
    /// Service pour gérer la connexion à un serveur MSSQL
    /// </summary>
    public class MssqlConnection : IConnectionState, IDisposable
    {
        private SqlConnection? _connection;
        private string _serverName;
        private string _databaseName;
        private string _statusMessage;
        private bool _isConnected;
        private readonly string _connectionString;

        public bool IsConnected => _isConnected;
        public string StatusMessage => _statusMessage;
        public string? ServerName => _isConnected ? _serverName : null;
        public string? DatabaseName => _isConnected ? _databaseName : null;

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        /// <summary>
        /// Initialise une nouvelle instance de MssqlConnection
        /// </summary>
        /// <param name="serverName">Nom du serveur MSSQL (ex: srv-divalto-bdd.incb.local)</param>
        /// <param name="databaseName">Nom de la base de données (ex: DIVATURBOSELF)</param>
        /// <param name="userId">Identifiant utilisateur (optionnel, utilise l'authentification Windows si non fourni)</param>
        /// <param name="password">Mot de passe (optionnel)</param>
        public MssqlConnection(string serverName, string databaseName, string? userId = null, string? password = null)
        {
            _serverName = serverName;
            _databaseName = databaseName;
            _statusMessage = "Déconnecté";
            _isConnected = false;

            // Construire la chaîne de connexion manuellement
            // Approche simple et robuste pour éviter les problèmes avec SqlConnectionStringBuilder
            try
            {
                Logger.Instance.LogInfo($"Construction de la chaîne de connexion pour {serverName}/{databaseName}");

                if (string.IsNullOrEmpty(userId))
                {
                    // Authentification Windows
                    _connectionString = $"Data Source={serverName};Initial Catalog={databaseName};Integrated Security=true;Encrypt=false;Connection Timeout=15;";
                    Logger.Instance.LogInfo("Mode authentification : Windows");
                }
                else
                {
                    // Authentification SQL
                    _connectionString = $"Data Source={serverName};Initial Catalog={databaseName};User ID={userId};Password={password};Encrypt=false;Connection Timeout=15;";
                    Logger.Instance.LogInfo("Mode authentification : SQL Server");
                }

                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new InvalidOperationException("La chaîne de connexion n'a pas pu être créée");
                }

                Logger.Instance.LogSuccess($"Chaîne de connexion créée avec succès : Data Source={serverName};Initial Catalog={databaseName};...");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Erreur lors de la construction de la chaîne de connexion : {ex.Message}", ex);
                _connectionString = string.Empty;
            }
        }

        /// <summary>
        /// Se connecte au serveur MSSQL
        /// </summary>
        /// <returns>True si la connexion réussit, False sinon</returns>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_isConnected)
                {
                    _statusMessage = "Déjà connecté";
                    Logger.Instance.LogWarning(_statusMessage);
                    return true;
                }

                Logger.Instance.LogInfo($"Tentative de connexion au serveur {_serverName} / {_databaseName}");
                Logger.Instance.LogDebug($"Chaîne de connexion utilisée : {_connectionString}");

                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new InvalidOperationException("La chaîne de connexion est vide ou null");
                }

                _connection = new SqlConnection(_connectionString);
                Logger.Instance.LogDebug("Instance SqlConnection créée");

                if (_connection == null)
                {
                    throw new InvalidOperationException("Impossible de créer l'instance SqlConnection");
                }

                Logger.Instance.LogDebug("Appel de OpenAsync...");
                await _connection.OpenAsync();
                Logger.Instance.LogDebug("Connexion ouverte avec succès");

                _isConnected = true;
                _statusMessage = $"Connecté à {_serverName} / {_databaseName}";

                Logger.Instance.LogSuccess(_statusMessage);

                OnConnectionStateChanged(new ConnectionStateChangedEventArgs
                {
                    IsConnected = true,
                    ServerName = _serverName,
                    DatabaseName = _databaseName,
                    StatusMessage = _statusMessage
                });

                return true;
            }
            catch (SqlException ex)
            {
                _isConnected = false;
                _statusMessage = $"Erreur de connexion : {ex.Message}";

                Logger.Instance.LogError(_statusMessage, ex);

                OnConnectionStateChanged(new ConnectionStateChangedEventArgs
                {
                    IsConnected = false,
                    ServerName = null,
                    DatabaseName = null,
                    StatusMessage = _statusMessage
                });

                return false;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _statusMessage = $"Erreur inattendue : {ex.Message}";

                Logger.Instance.LogError(_statusMessage, ex);

                OnConnectionStateChanged(new ConnectionStateChangedEventArgs
                {
                    IsConnected = false,
                    ServerName = null,
                    DatabaseName = null,
                    StatusMessage = _statusMessage
                });

                return false;
            }
        }

        /// <summary>
        /// Se connecte au serveur MSSQL de manière synchrone
        /// </summary>
        /// <returns>True si la connexion réussit, False sinon</returns>
        public bool Connect()
        {
            return ConnectAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Se déconnecte du serveur MSSQL
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                if (_connection != null && _isConnected)
                {
                    Logger.Instance.LogInfo($"Déconnexion du serveur {_serverName}");

                    await _connection.CloseAsync();
                    _isConnected = false;
                    _statusMessage = "Déconnecté";

                    Logger.Instance.LogSuccess("Déconnexion réussie");

                    OnConnectionStateChanged(new ConnectionStateChangedEventArgs
                    {
                        IsConnected = false,
                        ServerName = null,
                        DatabaseName = null,
                        StatusMessage = _statusMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _statusMessage = $"Erreur lors de la déconnexion : {ex.Message}";
                Logger.Instance.LogError(_statusMessage, ex);
            }
        }

        /// <summary>
        /// Se déconnecte du serveur MSSQL de manière synchrone
        /// </summary>
        public void Disconnect()
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Exécute une requête SQL et retourne un SqlDataReader
        /// </summary>
        /// <param name="query">La requête SQL à exécuter</param>
        /// <returns>Un SqlDataReader contenant les résultats</returns>
        public async Task<SqlDataReader?> ExecuteQueryAsync(string query)
        {
            try
            {
                if (!_isConnected || _connection == null)
                {
                    _statusMessage = "Connexion non établie";
                    Logger.Instance.LogWarning(_statusMessage);
                    return null;
                }

                Logger.Instance.LogDebug($"Exécution requête SELECT : {query.Substring(0, Math.Min(100, query.Length))}...");

                SqlCommand command = new SqlCommand(query, _connection);
                return await command.ExecuteReaderAsync();
            }
            catch (SqlException ex)
            {
                _statusMessage = $"Erreur lors de l'exécution de la requête : {ex.Message}";
                Logger.Instance.LogError(_statusMessage, ex);
                return null;
            }
        }

        /// <summary>
        /// Exécute une requête SQL qui modifie les données (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="query">La requête SQL à exécuter</param>
        /// <returns>Le nombre de lignes affectées</returns>
        public async Task<int> ExecuteNonQueryAsync(string query)
        {
            try
            {
                if (!_isConnected || _connection == null)
                {
                    _statusMessage = "Connexion non établie";
                    Logger.Instance.LogWarning(_statusMessage);
                    return 0;
                }

                Logger.Instance.LogDebug($"Exécution requête INSERT/UPDATE/DELETE : {query.Substring(0, Math.Min(100, query.Length))}...");

                SqlCommand command = new SqlCommand(query, _connection);
                int result = await command.ExecuteNonQueryAsync();

                Logger.Instance.LogInfo($"Requête exécutée : {result} ligne(s) affectée(s)");

                return result;
            }
            catch (SqlException ex)
            {
                _statusMessage = $"Erreur lors de l'exécution de la requête : {ex.Message}";
                Logger.Instance.LogError(_statusMessage, ex);
                return 0;
            }
        }

        /// <summary>
        /// Exécute une requête SQL et retourne un seul résultat scalaire
        /// </summary>
        /// <param name="query">La requête SQL à exécuter</param>
        /// <returns>Le résultat scalaire ou null</returns>
        public async Task<object?> ExecuteScalarAsync(string query)
        {
            try
            {
                if (!_isConnected || _connection == null)
                {
                    _statusMessage = "Connexion non établie";
                    Logger.Instance.LogWarning(_statusMessage);
                    return null;
                }

                Logger.Instance.LogDebug($"Exécution requête SCALAR : {query.Substring(0, Math.Min(100, query.Length))}...");

                SqlCommand command = new SqlCommand(query, _connection);
                object? result = await command.ExecuteScalarAsync();

                Logger.Instance.LogDebug($"Résultat scalaire : {result}");

                return result;
            }
            catch (SqlException ex)
            {
                _statusMessage = $"Erreur lors de l'exécution de la requête : {ex.Message}";
                Logger.Instance.LogError(_statusMessage, ex);
                return null;
            }
        }

        /// <summary>
        /// Obtient la connexion SQL directe pour les opérations avancées
        /// </summary>
        public SqlConnection? GetConnection()
        {
            return _isConnected ? _connection : null;
        }

        /// <summary>
        /// Lève l'événement de changement d'état de connexion
        /// </summary>
        protected virtual void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Récupère les colonnes d'une table
        /// </summary>
        /// <param name="tableName">Nom de la table</param>
        /// <returns>Liste des noms de colonnes</returns>
        public async Task<List<string>> GetTableColumnsAsync(string tableName)
        {
            var columns = new List<string>();

            try
            {
                if (!_isConnected || _connection == null)
                {
                    _statusMessage = "Connexion non établie";
                    Logger.Instance.LogWarning(_statusMessage);
                    return columns;
                }

                string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' ORDER BY ORDINAL_POSITION";

                Logger.Instance.LogDebug($"Récupération des colonnes pour la table {tableName}");

                using (SqlDataReader? reader = await ExecuteQueryAsync(query))
                {
                    if (reader != null)
                    {
                        while (await reader.ReadAsync())
                        {
                            var columnName = reader["COLUMN_NAME"].ToString();
                            if (!string.IsNullOrEmpty(columnName))
                            {
                                columns.Add(columnName);
                            }
                        }
                    }
                }

                Logger.Instance.LogInfo($"Colonnes récupérées pour {tableName} : {columns.Count}");
                return columns;
            }
            catch (Exception ex)
            {
                _statusMessage = $"Erreur lors de la récupération des colonnes : {ex.Message}";
                Logger.Instance.LogError(_statusMessage, ex);
                return columns;
            }
        }

        /// <summary>
        /// Récupère les données d'une table avec un filtre optionnel
        /// </summary>
        /// <param name="tableName">Nom de la table</param>
        /// <param name="whereClause">Clause WHERE optionnelle (ex: "DOS = 200")</param>
        /// <returns>TableData contenant les colonnes et les données</returns>
        public async Task<TableData> GetTableDataAsync(string tableName, string? whereClause = null, int? rowLimit = null)
        {
            var tableData = new TableData(tableName);

            try
            {
                if (!_isConnected || _connection == null)
                {
                    _statusMessage = "Connexion non établie";
                    Logger.Instance.LogWarning(_statusMessage);
                    return tableData;
                }

                // Récupérer les colonnes
                tableData.ColumnNames = await GetTableColumnsAsync(tableName);

                if (tableData.ColumnNames.Count == 0)
                {
                    Logger.Instance.LogWarning($"Aucune colonne trouvée pour la table {tableName}");
                    return tableData;
                }

                // Construire la requête avec TOP si rowLimit est spécifié
                string query = $"SELECT ";

                if (rowLimit.HasValue && rowLimit.Value > 0)
                {
                    query += $"TOP {rowLimit.Value} ";
                }

                query += $"* FROM {tableName}";

                if (!string.IsNullOrEmpty(whereClause))
                {
                    query += $" WHERE {whereClause}";
                }

                Logger.Instance.LogDebug($"Exécution requête : {query}");

                // Utiliser SqlDataAdapter pour charger les données dans un DataTable
                using (SqlCommand command = new SqlCommand(query, _connection))
                {
                    command.CommandTimeout = 30;
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(tableData.Data);
                    }
                }

                Logger.Instance.LogSuccess($"Données récupérées pour {tableName} : {tableData.Data.Rows.Count} lignes");
                return tableData;
            }
            catch (Exception ex)
            {
                _statusMessage = $"Erreur lors de la récupération des données : {ex.Message}";
                Logger.Instance.LogError(_statusMessage, ex);
                return tableData;
            }
        }

        /// <summary>
        /// Libère les ressources utilisées par la connexion
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            _connection?.Dispose();
        }
    }
}
