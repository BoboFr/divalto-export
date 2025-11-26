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
                    // Utiliser Environment.ProcessPath (méthode recommandée en .NET 5+)
                    var exePath = Environment.ProcessPath ??
                                  Path.Combine(AppContext.BaseDirectory, "Divalto.exe");

                    // Vérifier si le fichier existe et afficher un warning si non
                    if (!File.Exists(exePath))
                    {
                        Debug.WriteLine($"ATTENTION: Le fichier exe n'existe pas au chemin détecté : {exePath}");
                        // Tenter de trouver Divalto.exe dans le répertoire de base
                        var alternativePath = Path.Combine(AppContext.BaseDirectory, "Divalto.exe");
                        if (File.Exists(alternativePath))
                        {
                            exePath = alternativePath;
                            Debug.WriteLine($"Chemin alternatif trouvé : {exePath}");
                        }
                    }

                    var backupPath = exePath + ".bak";

                    Debug.WriteLine($"Chemin exe final : {exePath}");
                    Debug.WriteLine($"Fichier existe ? {File.Exists(exePath)}");

                    // Créer un script batch pour remplacer l'exe et redémarrer
                    // Utiliser taskkill pour arrêter l'application proprement et éviter le verrouillage
                    var batchPath = Path.Combine(Path.GetTempPath(), "Divalto_Update.bat");
                    var processId = Process.GetCurrentProcess().Id;

                    var batchContent = $@"@echo off
setlocal enabledelayedexpansion

echo. > ""{batchPath}.log""
echo Debut du script >> ""{batchPath}.log""
echo Chemin source (tempPath): {tempPath} >> ""{batchPath}.log""
echo Chemin destination (exePath): {exePath} >> ""{batchPath}.log""
echo Chemin backup (backupPath): {backupPath} >> ""{batchPath}.log""

REM Tuer le processus Divalto.exe s'il existe encore
taskkill /F /IM Divalto.exe 2>nul

REM Attendre que l'application se ferme complètement et que les fichiers soient libérés
timeout /t 5 /nobreak

REM Supprimer le backup précédent s'il existe
if exist ""{backupPath}"" (
    echo Suppression du backup ancien >> ""{batchPath}.log""
    del /f /q ""{backupPath}""
)

REM Créer un backup de l'ancien exe (seulement s'il existe)
if exist ""{exePath}"" (
    echo Sauvegarde de l'ancien exe >> ""{batchPath}.log""
    move /y ""{exePath}"" ""{backupPath}""
) else (
    echo Premiere installation, pas de backup >> ""{batchPath}.log""
)

REM Copier la nouvelle version
if exist ""{tempPath}"" (
    echo Copie du nouveau fichier >> ""{batchPath}.log""
    copy /y ""{tempPath}"" ""{exePath}""
    if errorlevel 1 (
        echo ERREUR lors de la copie >> ""{batchPath}.log""
        if exist ""{backupPath}"" (
            echo Restauration depuis le backup >> ""{batchPath}.log""
            copy /y ""{backupPath}"" ""{exePath}""
        )
    ) else (
        echo Copie reussie >> ""{batchPath}.log""
        if exist ""{backupPath}"" del /f /q ""{backupPath}""
        if exist ""{tempPath}"" del /f /q ""{tempPath}""
    )
) else (
    echo ERREUR: tempPath introuvable >> ""{batchPath}.log""
    echo Chemin attendu: {tempPath} >> ""{batchPath}.log""
)

REM Redémarrer l'application
if exist ""{exePath}"" (
    echo Demarrage de l'application >> ""{batchPath}.log""
    start """" ""{exePath}""
) else (
    echo ERREUR: Impossible de redemarrer, exePath introuvable >> ""{batchPath}.log""
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
