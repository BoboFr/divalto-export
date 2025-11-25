using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Divalto.ViewModels;
using Velopack;
using Velopack.Sources;

namespace Divalto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string GithubRepoUrl = "https://github.com/BoboFr/divalto-export";
        private ConnectionViewModel? _viewModel;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans le constructeur MainWindow : {ex}");
                throw;
            }
        }

        /// <summary>
        /// Initialise le ViewModel au chargement de la fenêtre
        /// </summary>
                                        /// <summary>
        /// Initialise le ViewModel au chargement de la fenetre
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("MainWindow_Loaded : Demarrage");

                // Creer et assigner le ViewModel
                _viewModel = new ConnectionViewModel();
                Debug.WriteLine("MainWindow_Loaded : ViewModel cree");

                this.DataContext = _viewModel;
                Debug.WriteLine("MainWindow_Loaded : DataContext assigne");

                await RunAutoUpdateAsync();

                // Enregistrer l'evenement de fermeture
                this.Closing += OnWindowClosing;
                Debug.WriteLine("MainWindow_Loaded : Evenement de fermeture enregistre");

                // Connexion automatique au demarrage
                await _viewModel.AutoConnectAsync();
                Debug.WriteLine("MainWindow_Loaded : Connexion automatique complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de l'initialisation : {ex}");
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}\n\n{ex.StackTrace}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
/// <summary>
        /// Gère la fermeture de la fenêtre
        /// </summary>
        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.Cleanup();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la fermeture : {ex.Message}");
            }
        }

        /// <summary>
        /// Événement clic sur le bouton "Se connecter"
        /// </summary>
        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.ConnectCommand?.Execute(null);
        }

        /// <summary>
        /// Événement clic sur le bouton "Se déconnecter"
        /// </summary>
        private void OnDisconnectClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.DisconnectCommand?.Execute(null);
        }

        /// <summary>
        /// Sélectionne toutes les colonnes filtrées
        /// </summary>
        private void OnSelectAllColumnsClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.SelectAllFilteredColumns();
        }

        /// <summary>
        /// Désélectionne toutes les colonnes filtrées
        /// </summary>
        private void OnDeselectAllColumnsClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.DeselectAllFilteredColumns();
        }

        /// <summary>
        /// Efface le filtre de recherche des colonnes
        /// </summary>
        private void OnClearColumnFilterClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.ClearColumnFilter();
        }

        /// <summary>
        /// Annule le chargement en cours
        /// </summary>
        private void OnCancelLoadingClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.CancelLoading();
        }

        /// <summary>
        /// Annule l'export en cours
        /// </summary>
        private void OnCancelExportClick(object sender, RoutedEventArgs e)
        {
            _viewModel?.CancelExport();
        }

        /// <summary>
        /// Vérifie et applique automatiquement les mises à jour Velopack depuis la release GitHub "latest".
        /// </summary>
        private async Task RunAutoUpdateAsync()
        {
            try
            {
                using var mgr = new UpdateManager(
                    new GithubSource(GithubRepoUrl, accessToken: null, prerelease: false, downloader: null));

                if (!mgr.IsInstalled)
                {
                    Debug.WriteLine("Velopack: application non installée, mise à jour ignorée.");
                    return;
                }

                if (mgr.UpdatePendingRestart is VelopackAsset pending)
                {
                    Debug.WriteLine("Velopack: application d'une mise à jour en attente, redémarrage...");
                    mgr.ApplyUpdatesAndRestart(pending);
                    return;
                }

                var update = await mgr.CheckForUpdatesAsync();
                if (update == null)
                {
                    Debug.WriteLine("Velopack: aucune mise à jour disponible.");
                    return;
                }

                Debug.WriteLine($"Velopack: mise à jour trouvée vers {update.TargetFullRelease.Version}, téléchargement...");
                await mgr.DownloadUpdatesAsync(update);
                Debug.WriteLine("Velopack: application de la mise à jour et redémarrage.");
                mgr.ApplyUpdatesAndRestart(update.TargetFullRelease);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Velopack: mise à jour automatique ignorée ({ex.Message}).");
            }
        }
    }
}






