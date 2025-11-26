using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Divalto.Services;
using static Divalto.Services.UpdateService;

namespace Divalto
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private UpdateService? _updateService;
        private UpdateProgressWindow? _progressWindow;

        public App()
        {
            this.Startup += App_Startup;
        }

        /// <summary>
        /// Gère le démarrage de l'application
        /// </summary>
        private void App_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                _updateService = new UpdateService();
                _updateService.UpdateCheckCompleted += UpdateService_UpdateCheckCompleted;
                _updateService.UpdateError += UpdateService_UpdateError;
                _updateService.ProgressChanged += UpdateService_ProgressChanged;

                // Vérifier les mises à jour en arrière-plan (sans bloquer l'interface)
                _ = Task.Run(() => _updateService.CheckForUpdatesAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'initialisation du service de mise à jour : {ex.Message}");
            }
        }

        /// <summary>
        /// Gère la fin de la vérification des mises à jour
        /// </summary>
        private void UpdateService_UpdateCheckCompleted(object? sender, UpdateCheckEventArgs e)
        {
            try
            {
                if (e.UpdateAvailable)
                {
                    var result = MessageBox.Show(
                        $"Une nouvelle version est disponible !\n\n" +
                        $"Version actuelle : {e.CurrentVersion}\n" +
                        $"Nouvelle version : {e.NewVersion}\n\n" +
                        $"Notes de version :\n{e.ReleaseNotes}\n\n" +
                        $"Voulez-vous mettre à jour maintenant ?",
                        "Mise à jour disponible",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && _updateService != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Créer et afficher la modale sur le thread UI
                                Current.Dispatcher.Invoke(() =>
                                {
                                    _progressWindow = new UpdateProgressWindow();
                                    _progressWindow.Show();
                                });

                                if (await _updateService.DownloadAndInstallUpdateAsync())
                                {
                                    // La fenêtre se fermera quand l'application redémarrera
                                }
                                else
                                {
                                    // Fermer la fenêtre en cas d'erreur
                                    Current.Dispatcher.Invoke(() =>
                                    {
                                        _progressWindow?.Close();
                                        MessageBox.Show(
                                            "La mise à jour a échoué.",
                                            "Erreur",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Error);
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Erreur lors du téléchargement : {ex.Message}");
                                Current.Dispatcher.Invoke(() =>
                                {
                                    _progressWindow?.Close();
                                });
                            }
                        });
                    }
                }
                else
                {
                    Debug.WriteLine($"Vous utilisez la dernière version : {e.CurrentVersion}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du traitement de la mise à jour : {ex.Message}");
            }
        }

        /// <summary>
        /// Gère les erreurs du service de mise à jour
        /// </summary>
        private void UpdateService_UpdateError(object? sender, string errorMessage)
        {
            Debug.WriteLine($"Erreur de mise à jour : {errorMessage}");
        }

        /// <summary>
        /// Gère la progression du téléchargement
        /// </summary>
        private void UpdateService_ProgressChanged(object? sender, ProgressEventArgs e)
        {
            if (_progressWindow != null)
            {
                _progressWindow.Dispatcher.Invoke(() =>
                {
                    _progressWindow.UpdateProgress(e.BytesDownloaded, e.TotalBytes);
                });
            }
        }
    }
}
