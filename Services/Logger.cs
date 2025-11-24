using System;
using System.IO;
using System.Text;

namespace Divalto.Services
{
    /// <summary>
    /// Service de logging pour enregistrer les événements de l'application
    /// </summary>
    public class Logger
    {
        private static Logger? _instance;
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Niveaux de log disponibles
        /// </summary>
        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Debug,
            Success
        }

        private Logger()
        {
            // Créer le répertoire Logs dans le dossier de l'application
            _logDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Logs"
            );

            // Créer le répertoire s'il n'existe pas
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // Chemin du fichier de log (journalier)
            string dateString = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(_logDirectory, $"Divalto_{dateString}.log");
        }

        /// <summary>
        /// Obtient l'instance singleton du Logger
        /// </summary>
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Logger();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Enregistre un message de log
        /// </summary>
        public void Log(string message, LogLevel level = LogLevel.Info, Exception? exception = null)
        {
            lock (_lockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string levelString = level.ToString().ToUpper();

                    // Construire le message de log
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"[{timestamp}] [{levelString}] {message}");

                    // Ajouter les informations d'exception si présentes
                    if (exception != null)
                    {
                        sb.AppendLine($"Exception: {exception.GetType().Name}");
                        sb.AppendLine($"Message: {exception.Message}");
                        sb.AppendLine($"StackTrace: {exception.StackTrace}");

                        if (exception.InnerException != null)
                        {
                            sb.AppendLine($"InnerException: {exception.InnerException.Message}");
                        }
                    }

                    // Ajouter une ligne vide pour la lisibilité
                    sb.AppendLine();

                    // Écrire dans le fichier
                    File.AppendAllText(_logFilePath, sb.ToString(), Encoding.UTF8);

                    // Afficher aussi en Debug
                    System.Diagnostics.Debug.WriteLine($"[{levelString}] {message}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors de l'écriture du log : {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Enregistre un message d'information
        /// </summary>
        public void LogInfo(string message)
        {
            Log(message, LogLevel.Info);
        }

        /// <summary>
        /// Enregistre un message d'avertissement
        /// </summary>
        public void LogWarning(string message, Exception? exception = null)
        {
            Log(message, LogLevel.Warning, exception);
        }

        /// <summary>
        /// Enregistre un message d'erreur
        /// </summary>
        public void LogError(string message, Exception? exception = null)
        {
            Log(message, LogLevel.Error, exception);
        }

        /// <summary>
        /// Enregistre un message de succès
        /// </summary>
        public void LogSuccess(string message)
        {
            Log(message, LogLevel.Success);
        }

        /// <summary>
        /// Enregistre un message de debug
        /// </summary>
        public void LogDebug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        /// <summary>
        /// Obtient le chemin du fichier de log actuel
        /// </summary>
        public string GetCurrentLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Obtient le répertoire des logs
        /// </summary>
        public string GetLogDirectory()
        {
            return _logDirectory;
        }

        /// <summary>
        /// Récupère tous les fichiers de log
        /// </summary>
        public FileInfo[] GetAllLogFiles()
        {
            DirectoryInfo dir = new DirectoryInfo(_logDirectory);
            return dir.GetFiles("Divalto_*.log");
        }

        /// <summary>
        /// Nettoie les fichiers de log anciens (plus de N jours)
        /// </summary>
        public void CleanupOldLogs(int daysToKeep = 7)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(_logDirectory);
                FileInfo[] logFiles = dir.GetFiles("Divalto_*.log");

                DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (FileInfo file in logFiles)
                {
                    if (file.LastWriteTime < cutoffDate)
                    {
                        file.Delete();
                        LogInfo($"Fichier de log supprimé : {file.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Erreur lors du nettoyage des logs : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exporte tous les logs dans un fichier compressé (ZIP)
        /// </summary>
        public bool ExportLogsAsZip(string outputPath)
        {
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                System.IO.Compression.ZipFile.CreateFromDirectory(_logDirectory, outputPath);
                LogInfo($"Logs exportés vers : {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Erreur lors de l'export des logs : {ex.Message}", ex);
                return false;
            }
        }
    }
}
