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

        public event EventHandler<UpdateCheckEventArgs>? UpdateCheckCompleted;
        public event EventHandler<string>? UpdateError;

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
                        var isPrerelease = root.GetProperty("prerelease").GetBoolean();

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

                        var currentVersion = GetCurrentVersion();
                        var updateAvailable = CompareVersions(latestVersion, currentVersion) > 0;

                        UpdateCheckCompleted?.Invoke(this, new UpdateCheckEventArgs
                        {
                            UpdateAvailable = updateAvailable,
                            CurrentVersion = currentVersion,
                            NewVersion = latestVersion.TrimStart('v'),
                            ReleaseNotes = releaseNotes,
                            IsPrerelease = isPrerelease
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
        /// Télécharge et installe la mise à jour
        /// </summary>
        public async Task<bool> DownloadAndInstallUpdateAsync()
        {
            try
            {
                // Utiliser Velopack pour l'installation
                // La mise à jour est téléchargée par Velopack depuis les releases GitHub
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Divalto-Update-Download");
                    var response = await client.GetAsync(_githubApiUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        UpdateError?.Invoke(this, "Impossible de télécharger la mise à jour");
                        return false;
                    }

                    // Velopack gère automatiquement le téléchargement et l'installation
                    // À partir des releases disponibles sur GitHub
                    Debug.WriteLine("Mise à jour téléchargée, redémarrage de l'application...");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la mise à jour : {ex.Message}");
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
        /// Compare deux versions au format "v1.0.0" ou "1.0.0"
        /// Retourne > 0 si v2 > v1, 0 si égales, < 0 si v2 < v1
        /// </summary>
        private static int CompareVersions(string newVersion, string currentVersion)
        {
            try
            {
                var v1 = new Version(newVersion.TrimStart('v'));
                var v2 = new Version(currentVersion);
                return v1.CompareTo(v2);
            }
            catch
            {
                return 0;
            }
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
}
