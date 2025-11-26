using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Divalto.Services
{
    /// <summary>
    /// Service de gestion des mises à jour via GitHub Releases
    /// </summary>
    public class UpdateService
    {
        private readonly string _githubApiUrl = "https://api.github.com/repos/BoboFr/divalto-export/releases/latest";
        private bool _isCheckingForUpdates = false;
        private string? _downloadUrl = null;
        private string? _newVersion = null;

        public event EventHandler<UpdateCheckEventArgs>? UpdateCheckCompleted;
        public event EventHandler<string>? UpdateError;
        public event EventHandler<ProgressEventArgs>? ProgressChanged;

        public UpdateService()
        {
        }

        /// <summary>
        /// Vérifie s'il y a une mise à jour disponible
        /// </summary>
        public async Task CheckForUpdatesAsync()
        {
            if (_isCheckingForUpdates)
                return;

            _isCheckingForUpdates = true;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Divalto-Update-Check");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    var response = await client.GetAsync(_githubApiUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        UpdateCheckCompleted?.Invoke(this, new UpdateCheckEventArgs
                        {
                            UpdateAvailable = false,
                            CurrentVersion = GetCurrentVersion()
                        });
                        return;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(content))
                    {
                        var root = doc.RootElement;
                        var latestVersion = root.GetProperty("tag_name").GetString() ?? "unknown";
                        var releaseNotes = root.GetProperty("body").GetString() ?? "";
                        var isDraft = root.GetProperty("draft").GetBoolean();

                        // Ignorer les versions de brouillon
                        if (isDraft)
                        {
                            UpdateCheckCompleted?.Invoke(this, new UpdateCheckEventArgs
                            {
                                UpdateAvailable = false,
                                CurrentVersion = GetCurrentVersion()
                            });
                            return;
                        }

                        // Extraire l'URL de téléchargement du premier asset (Divalto.exe)
                        var assets = root.GetProperty("assets");
                        _downloadUrl = null;
                        foreach (var asset in assets.EnumerateArray())
                        {
                            var assetName = asset.GetProperty("name").GetString() ?? "";
                            if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                _downloadUrl = asset.GetProperty("browser_download_url").GetString();
                                break;
                            }
                        }

                        var currentVersion = GetCurrentVersion();
                        var updateAvailable = CompareVersions(latestVersion, currentVersion) > 0;

                        if (updateAvailable)
                        {
                            _newVersion = latestVersion.TrimStart('v');
                        }

                        UpdateCheckCompleted?.Invoke(this, new UpdateCheckEventArgs
                        {
                            UpdateAvailable = updateAvailable,
                            CurrentVersion = currentVersion,
                            NewVersion = latestVersion.TrimStart('v'),
                            ReleaseNotes = releaseNotes,
                            IsPrerelease = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la vérification des mises à jour : {ex.Message}");
                UpdateError?.Invoke(this, $"Erreur lors de la vérification des mises à jour : {ex.Message}");
            }
            finally
            {
                _isCheckingForUpdates = false;
            }
        }

        /// <summary>
        /// Télécharge et installe la mise à jour avec suivi de progression
        /// </summary>
        public async Task<bool> DownloadAndInstallUpdateAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_downloadUrl) || string.IsNullOrEmpty(_newVersion))
                {
                    UpdateError?.Invoke(this, "Aucune mise à jour disponible à installer");
                    return false;
                }

                Debug.WriteLine($"Téléchargement de la mise à jour {_newVersion}...");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Divalto-Update-Download");
                    client.Timeout = TimeSpan.FromMinutes(10);

                    // Télécharger avec rapport de progression
                    var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        UpdateError?.Invoke(this, $"Impossible de télécharger la mise à jour : {response.StatusCode}");
                        return false;
                    }

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var tempPath = Path.Combine(Path.GetTempPath(), "Divalto_Update.exe");

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(tempPath))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            // Signaler la progression
                            ProgressChanged?.Invoke(this, new ProgressEventArgs { BytesDownloaded = totalRead, TotalBytes = totalBytes });
                        }
                    }

                    Debug.WriteLine($"Mise à jour téléchargée : {tempPath}");

                    // Sauvegarder le chemin du nouvel exe en tant que backup de l'ancien
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "Divalto.exe";
                    var backupPath = exePath + ".bak";

                    // Créer un script batch pour remplacer l'exe et redémarrer
                    // Utiliser taskkill pour arrêter l'application proprement et éviter le verrouillage
                    var batchPath = Path.Combine(Path.GetTempPath(), "Divalto_Update.bat");
                    var processId = Process.GetCurrentProcess().Id;

                    var batchContent = $@"@echo off
setlocal enabledelayedexpansion

REM Tuer le processus Divalto.exe s'il existe encore
taskkill /F /IM Divalto.exe 2>nul

REM Attendre que l'application se ferme complètement et que les fichiers soient libérés
timeout /t 2 /nobreak

REM Copier la nouvelle version
copy /y ""{tempPath}"" ""{exePath}""

REM Nettoyer le fichier temporaire
if exist ""{tempPath}"" del /f /q ""{tempPath}""

REM Redémarrer l'application
if exist ""{exePath}"" (
    start """" ""{exePath}""
)

REM Attendre un peu avant de supprimer le batch
timeout /t 1 /nobreak

REM Supprimer ce batch file
del /f /q ""%~f0""
";

                    File.WriteAllText(batchPath, batchContent);
                    Debug.WriteLine($"Script batch créé : {batchPath}");

                    // Exécuter le script en arrière-plan
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batchPath,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });

                    Debug.WriteLine("Fermeture de l'application...");
                    // Fermer l'application
                    System.Windows.Application.Current?.Shutdown();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la mise à jour : {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                UpdateError?.Invoke(this, $"Erreur lors de la mise à jour : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtient la version actuelle de l'application (AssemblyFileVersion)
        /// </summary>
        private static string GetCurrentVersion()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName
                    ?? Path.Combine(AppContext.BaseDirectory, "Divalto.exe");

                if (File.Exists(exePath))
                {
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(exePath);
                    return fileVersionInfo.FileVersion ?? "1.0.0.0";
                }

                return "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        /// <summary>
        /// Compare deux versions au format "v1.0.0" ou "1.0.0" ou "v22"
        /// Retourne > 0 si v2 > v1, 0 si égales, < 0 si v2 < v1
        /// </summary>
        private static int CompareVersions(string newVersion, string currentVersion)
        {
            try
            {
                var newVersionStr = newVersion.TrimStart('v');
                var currVersionStr = currentVersion;

                // Normaliser les versions avec format incomplet (ex: "22" -> "22.0.0.0")
                var v1 = NormalizeVersion(newVersionStr);
                var v2 = NormalizeVersion(currVersionStr);

                return v1.CompareTo(v2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la comparaison de versions '{newVersion}' vs '{currentVersion}': {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Normalise une version pour qu'elle soit valide (major.minor.build.revision)
        /// </summary>
        private static Version NormalizeVersion(string versionStr)
        {
            var parts = versionStr.Split('.');

            // Pad avec des zéros si nécessaire
            while (parts.Length < 4)
            {
                versionStr += ".0";
                parts = versionStr.Split('.');
            }

            return new Version(versionStr);
        }
    }

    /// <summary>
    /// Événement pour les informations de mise à jour
    /// </summary>
    public class UpdateCheckEventArgs : EventArgs
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string NewVersion { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public bool IsPrerelease { get; set; }
    }

    /// <summary>
    /// Événement pour le suivi de la progression du téléchargement
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
    }
}
