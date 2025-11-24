using System;
using System.Windows;
using System.Windows.Controls;
using Divalto.ViewModels;

namespace Divalto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded : Démarrage");

                // Créer et assigner le ViewModel
                _viewModel = new ConnectionViewModel();
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded : ViewModel créé");

                this.DataContext = _viewModel;
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded : DataContext assigné");

                // Enregistrer l'événement de fermeture
                this.Closing += OnWindowClosing;
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded : Événement de fermeture enregistré");

                // Connexion automatique au démarrage
                await _viewModel.AutoConnectAsync();
                System.Diagnostics.Debug.WriteLine("MainWindow_Loaded : Connexion automatique complétée");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation : {ex}");
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
    }
}
