using System;
using System.Diagnostics;
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
        /// <summary>\r\n        /// Initialise le ViewModel au chargement de la fenetre\r\n        /// </summary>\r\n
        /// 
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

    }
}




