using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Divalto.Commands;
using Divalto.Helpers;
using Divalto.Models;
using Divalto.Services;

namespace Divalto.ViewModels
{
    /// <summary>
    /// ViewModel pour gérer l'état de la connexion et les données de table
    /// Implémente INotifyPropertyChanged pour la liaison de données WPF
    /// </summary>
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        private MssqlConnection? _mssqlConnection;
        private string _serverName = "srv-divalto-bdd.incb.local";
        private string _databaseName = "DIVATURBOSELF";
        private string _statusMessage = "Déconnecté";
        private bool _isConnected = false;
        private bool _isConnectButtonEnabled = true;
        private bool _isDisconnectButtonEnabled = false;
        private TableInfo? _selectedTable;
        private ObservableCollection<TableInfo> _availableTables = new();
        private ObservableCollection<SelectableColumn> _availableColumns = new();
        private DataView? _tableDataView;
        private DataView? _tableDataViewForDisplay;
        private string _dataGridStatusMessage = "";
        private string _exportStatusMessage = "";
        private TableData? _currentTableData;
        private ExcelExportService? _excelExportService;
        private bool _isLoading = false;
        private bool _isLoadingVisible = true;
        private string _loadingStatusMessage = "Préparation du chargement...";
        private CancellationTokenSource? _loadingCancellationTokenSource;
        private const int DISPLAY_LIMIT = 20; // Limite pour l'affichage
        private bool _isExporting = false;
        private string _exportProgressMessage = "";
        private int _exportCurrentRow = 0;
        private int _exportTotalRows = 0;
        private double _exportProgressPercentage = 0;
        private CancellationTokenSource? _exportCancellationTokenSource;
        private DataView? _exportPreviewDataView;
        private string _exportPreviewMessage = "";
        private string _columnSearchFilter = "";
        private ObservableCollection<SelectableColumn> _filteredColumns = new();
        private string _columnFilterStatus = "";
        private DebounceDispatcher? _filterDebouncer;
        private DebounceDispatcher? _previewDebouncer;
        private CancellationTokenSource? _previewUpdateCancellation;
        private bool _isUpdatingPreview = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Propriétés de connexion
        public string ServerName
        {
            get => _serverName;
            set => SetProperty(ref _serverName, value);
        }

        public string DatabaseName
        {
            get => _databaseName;
            set => SetProperty(ref _databaseName, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public bool IsConnectButtonEnabled
        {
            get => _isConnectButtonEnabled;
            set => SetProperty(ref _isConnectButtonEnabled, value);
        }

        public bool IsDisconnectButtonEnabled
        {
            get => _isDisconnectButtonEnabled;
            set => SetProperty(ref _isDisconnectButtonEnabled, value);
        }

        // Propriétés de table
        public TableInfo? SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (SetProperty(ref _selectedTable, value))
                {
                    // Notifier que HasSelectedTable a changé
                    OnPropertyChanged(nameof(HasSelectedTable));
                    // Charger les données quand la table change
                    // Utiliser Dispatcher.BeginInvoke pour repousser le chargement après la mise à jour du UI
                    // Cela évite le freeze au moment de la sélection
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        await LoadTableDataAsync();
                    }));
                }
            }
        }

        /// <summary>
        /// Indique si une table est sélectionnée
        /// </summary>
        public bool HasSelectedTable => _selectedTable != null;

        public ObservableCollection<TableInfo> AvailableTables
        {
            get => _availableTables;
            set => SetProperty(ref _availableTables, value);
        }

        public ObservableCollection<SelectableColumn> AvailableColumns
        {
            get => _availableColumns;
            set => SetProperty(ref _availableColumns, value);
        }

        public DataView? TableDataView
        {
            get => _tableDataView;
            set => SetProperty(ref _tableDataView, value);
        }

        public string DataGridStatusMessage
        {
            get => _dataGridStatusMessage;
            set => SetProperty(ref _dataGridStatusMessage, value);
        }

        public string ExportStatusMessage
        {
            get => _exportStatusMessage;
            set => SetProperty(ref _exportStatusMessage, value);
        }

        // Propriétés de chargement
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsLoadingVisible
        {
            get => _isLoadingVisible;
            set => SetProperty(ref _isLoadingVisible, value);
        }

        public string LoadingStatusMessage
        {
            get => _loadingStatusMessage;
            set => SetProperty(ref _loadingStatusMessage, value);
        }

        // Propriétés d'export
        public bool IsExporting
        {
            get => _isExporting;
            set => SetProperty(ref _isExporting, value);
        }

        public string ExportProgressMessage
        {
            get => _exportProgressMessage;
            set => SetProperty(ref _exportProgressMessage, value);
        }

        public int ExportCurrentRow
        {
            get => _exportCurrentRow;
            set => SetProperty(ref _exportCurrentRow, value);
        }

        public int ExportTotalRows
        {
            get => _exportTotalRows;
            set => SetProperty(ref _exportTotalRows, value);
        }

        public double ExportProgressPercentage
        {
            get => _exportProgressPercentage;
            set => SetProperty(ref _exportProgressPercentage, value);
        }

        public DataView? ExportPreviewDataView
        {
            get => _exportPreviewDataView;
            set
            {
                if (SetProperty(ref _exportPreviewDataView, value))
                {
                    OnPropertyChanged(nameof(HasExportPreview));
                }
            }
        }

        public string ExportPreviewMessage
        {
            get => _exportPreviewMessage;
            set => SetProperty(ref _exportPreviewMessage, value);
        }

        /// <summary>
        /// Indique si l'aperçu d'export doit être visible
        /// </summary>
        public bool HasExportPreview => ExportPreviewDataView != null;

        // Propriétés de filtrage des colonnes
        public string ColumnSearchFilter
        {
            get => _columnSearchFilter;
            set
            {
                if (SetProperty(ref _columnSearchFilter, value))
                {
                    // Utiliser le debouncer pour différer le filtrage
                    _filterDebouncer?.Debounce(() => UpdateColumnFilter(), 150);
                }
            }
        }

        public ObservableCollection<SelectableColumn> FilteredColumns
        {
            get => _filteredColumns;
            set => SetProperty(ref _filteredColumns, value);
        }

        public string ColumnFilterStatus
        {
            get => _columnFilterStatus;
            set => SetProperty(ref _columnFilterStatus, value);
        }

        // Commandes
        public ICommand? ConnectCommand { get; set; }
        public ICommand? DisconnectCommand { get; set; }
        public ICommand? ExportCommand { get; set; }

        public ConnectionViewModel()
        {
            try
            {
                Logger.Instance.LogInfo("=== Application Divalto démarrée ===");
                Logger.Instance.LogInfo("Initialisation du ViewModel");

                // Initialiser le service d'export
                _excelExportService = new ExcelExportService();

                // Initialiser les debouncers
                _filterDebouncer = new DebounceDispatcher(System.Windows.Application.Current.Dispatcher);
                _previewDebouncer = new DebounceDispatcher(System.Windows.Application.Current.Dispatcher);

                // Initialiser la connexion d'abord
                InitializeConnection();

                // Initialiser les commandes après
                ConnectCommand = new AsyncRelayCommand(async _ => await ConnectAsync());
                DisconnectCommand = new AsyncRelayCommand(async _ => await DisconnectAsync());
                ExportCommand = new RelayCommand(_ => ExportToExcel());

                Logger.Instance.LogSuccess("ViewModel initialisé avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans le constructeur : {ex.Message}");
                Logger.Instance.LogError($"Erreur dans le constructeur du ViewModel : {ex.Message}", ex);
                StatusMessage = $"Erreur : {ex.Message}";
            }
        }

        /// <summary>
        /// Se connecte automatiquement au démarrage de l'application
        /// </summary>
        public async System.Threading.Tasks.Task AutoConnectAsync()
        {
            try
            {
                Logger.Instance.LogInfo("Connexion automatique au démarrage");
                StatusMessage = "Connexion en cours...";
                await ConnectAsync();

                // Si la connexion a réussi, charger les tables
                if (IsConnected)
                {
                    await InitializeTablesAsync();
                    // Ne pas sélectionner de table par défaut - l'utilisateur doit en sélectionner une
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Erreur lors de la connexion automatique : {ex.Message}", ex);
                StatusMessage = $"Erreur de connexion : {ex.Message}";
            }
        }

        /// <summary>
        /// Initialise la connexion MSSQL
        /// </summary>
        private void InitializeConnection()
        {
            try
            {
                Logger.Instance.LogInfo($"Initialisation de la connexion MSSQL pour {_serverName} / {_databaseName}");

                _mssqlConnection = new MssqlConnection(_serverName, _databaseName);
                Logger.Instance.LogInfo("Instance MssqlConnection créée");

                _mssqlConnection.ConnectionStateChanged += OnConnectionStateChanged;
                Logger.Instance.LogInfo("Event handler ConnectionStateChanged abonné");

                Logger.Instance.LogSuccess("Connexion MSSQL initialisée avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation de la connexion : {ex}");
                Logger.Instance.LogError($"Erreur lors de l'initialisation de la connexion : {ex.Message}", ex);
                StatusMessage = $"Erreur d'initialisation : {ex.Message}";
                _mssqlConnection = null;
            }
        }

        /// <summary>
        /// Se connecte au serveur
        /// </summary>
        private async System.Threading.Tasks.Task ConnectAsync()
        {
            try
            {
                if (_mssqlConnection == null)
                {
                    StatusMessage = "Connexion non initialisée";
                    Logger.Instance.LogWarning(StatusMessage);
                    return;
                }

                StatusMessage = "Connexion en cours...";
                IsConnectButtonEnabled = false;

                bool success = await _mssqlConnection.ConnectAsync();

                if (!success)
                {
                    StatusMessage = _mssqlConnection.StatusMessage;
                    Logger.Instance.LogWarning($"Échec de connexion : {StatusMessage}");
                }
                else
                {
                    // Charger la liste des tables après connexion
                    await InitializeTablesAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur : {ex.Message}";
                Logger.Instance.LogError($"Exception lors de la connexion : {ex.Message}", ex);
            }
            finally
            {
                IsConnectButtonEnabled = true;
            }
        }

        /// <summary>
        /// Se déconnecte du serveur
        /// </summary>
        private async System.Threading.Tasks.Task DisconnectAsync()
        {
            try
            {
                if (_mssqlConnection == null)
                {
                    StatusMessage = "Connexion non initialisée";
                    Logger.Instance.LogWarning(StatusMessage);
                    return;
                }

                StatusMessage = "Déconnexion en cours...";
                IsDisconnectButtonEnabled = false;

                await _mssqlConnection.DisconnectAsync();

                // Nettoyer les données
                AvailableTables.Clear();
                AvailableColumns.Clear();
                TableDataView = null;
                SelectedTable = null;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur : {ex.Message}";
                Logger.Instance.LogError($"Exception lors de la déconnexion : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Initialise la liste des tables disponibles
        /// </summary>
        private System.Threading.Tasks.Task InitializeTablesAsync()
        {
            try
            {
                Logger.Instance.LogInfo("Initialisation de la liste des tables");

                AvailableTables.Clear();
                AvailableTables.Add(new TableInfo("Matériel", "GMMAT"));
                AvailableTables.Add(new TableInfo("Clients", "CLI"));
                AvailableTables.Add(new TableInfo("Articles", "ART"));
                AvailableTables.Add(new TableInfo("Pièces - Entêtes", "ENT"));
                AvailableTables.Add(new TableInfo("Pièces - Mouvements", "MOUV"));
                AvailableTables.Add(new TableInfo("Contrat", "CEACONTRAT"));
                AvailableTables.Add(new TableInfo("Lignes Contrat", "CONTRATLMAT"));

                Logger.Instance.LogSuccess("Liste des tables initialisée");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Erreur lors de l'initialisation des tables : {ex.Message}", ex);
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Charge les données de la table sélectionnée avec le filtre DOS = 200 en arrière-plan
        /// </summary>
        private async System.Threading.Tasks.Task LoadTableDataAsync()
        {
            try
            {
                if (_selectedTable == null || _mssqlConnection == null || !_mssqlConnection.IsConnected)
                {
                    TableDataView = null;
                    AvailableColumns.Clear();
                    DataGridStatusMessage = "";
                    IsLoadingVisible = true;
                    return;
                }

                // Annuler tout chargement précédent
                _loadingCancellationTokenSource?.Cancel();
                _loadingCancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = _loadingCancellationTokenSource.Token;

                // Afficher l'écran de chargement
                IsLoading = true;
                IsLoadingVisible = false;
                LoadingStatusMessage = "Récupération des colonnes...";
                Logger.Instance.LogInfo($"Démarrage du chargement pour la table {_selectedTable.TableName}");

                // Charger les données en arrière-plan pour ne pas bloquer l'UI
                await System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        LoadingStatusMessage = "Chargement de l'aperçu des données...";
                        Logger.Instance.LogInfo($"Chargement de l'aperçu pour la table {_selectedTable.TableName} avec filtre DOS = 200");

                        // Charger seulement 51 lignes pour affichage (pour vérifier s'il y a plus)
                        _currentTableData = await _mssqlConnection.GetTableDataAsync(_selectedTable.TableName, "DOS = 200", rowLimit: 51);

                        // Vérifier l'annulation
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Logger.Instance.LogWarning("Chargement annulé par l'utilisateur");
                            return;
                        }

                        // Mettre à jour les colonnes disponibles sur le thread UI
                        var columnList = new List<string>(_currentTableData.ColumnNames);
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            AvailableColumns.Clear();
                            foreach (var column in columnList)
                            {
                                var selectableColumn = new SelectableColumn(column, isSelected: true);
                                // Abonner aux changements - l'aperçu utilisera le debouncer
                                selectableColumn.PropertyChanged += (s, e) =>
                                {
                                    if (e.PropertyName == nameof(SelectableColumn.IsSelected))
                                    {
                                        // L'aperçu sera mis à jour avec debouncing (300ms)
                                        UpdateExportPreview();
                                        // Le statut est mis à jour immédiatement mais en background
                                        System.Windows.Application.Current.Dispatcher.BeginInvoke(
                                            new Action(() => UpdateColumnFilterStatus()),
                                            System.Windows.Threading.DispatcherPriority.Background);
                                    }
                                };
                                AvailableColumns.Add(selectableColumn);
                            }
                            // Initialiser le filtre (pas de debounce nécessaire au démarrage)
                            UpdateColumnFilter();
                            // Initialiser l'aperçu (utilisation normale)
                            UpdateExportPreview();
                        });

                        // Vérifier l'annulation
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Logger.Instance.LogWarning("Chargement annulé par l'utilisateur");
                            return;
                        }

                        // Afficher les données dans le DataGrid sur le thread UI
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (_currentTableData.Data.Rows.Count > 0)
                            {
                                // Créer une DataTable temporaire avec seulement les 50 premiers résultats
                                var displayTable = _currentTableData.Data.Clone();
                                int rowsToDisplay = Math.Min(DISPLAY_LIMIT, _currentTableData.Data.Rows.Count);

                                for (int i = 0; i < rowsToDisplay; i++)
                                {
                                    displayTable.ImportRow(_currentTableData.Data.Rows[i]);
                                }

                                // Affecter la DataView pour l'affichage
                                _tableDataViewForDisplay = displayTable.DefaultView;
                                TableDataView = _tableDataViewForDisplay;

                                // Message de statut
                                // Si nous avons chargé 51 lignes, ça signifie qu'il y a plus de 50 lignes
                                if (_currentTableData.Data.Rows.Count > DISPLAY_LIMIT)
                                {
                                    DataGridStatusMessage = $"{DISPLAY_LIMIT} ligne(s) affichée(s) - Il y a plus de données (affichage limité pour la performance)";
                                    Logger.Instance.LogSuccess($"Aperçu chargé : {DISPLAY_LIMIT} premières lignes sur plusieurs");
                                }
                                else
                                {
                                    DataGridStatusMessage = $"{_currentTableData.Data.Rows.Count} ligne(s) trouvée(s)";
                                    Logger.Instance.LogSuccess($"Données chargées : {_currentTableData.Data.Rows.Count} lignes");
                                }
                            }
                            else
                            {
                                TableDataView = null;
                                DataGridStatusMessage = "Aucune donnée trouvée avec le filtre DOS = 200";
                                Logger.Instance.LogWarning(DataGridStatusMessage);
                            }
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Instance.LogWarning("Chargement annulé");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogError($"Erreur lors du chargement des données : {ex.Message}", ex);
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            DataGridStatusMessage = $"Erreur : {ex.Message}";
                            TableDataView = null;
                        });
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.Instance.LogWarning("Chargement annulé");
                DataGridStatusMessage = "Chargement annulé par l'utilisateur";
            }
            catch (Exception ex)
            {
                DataGridStatusMessage = $"Erreur : {ex.Message}";
                Logger.Instance.LogError($"Erreur lors du chargement des données : {ex.Message}", ex);
                TableDataView = null;
            }
            finally
            {
                // Masquer l'écran de chargement
                IsLoading = false;
                IsLoadingVisible = true;
            }
        }

        /// <summary>
        /// Annule le chargement en cours
        /// </summary>
        public void CancelLoading()
        {
            Logger.Instance.LogInfo("Annulation du chargement demandée");
            _loadingCancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Met à jour l'aperçu des données qui seront exportées (avec debouncing)
        /// </summary>
        private void UpdateExportPreview()
        {
            // Utiliser le debouncer pour différer l'appel
            _previewDebouncer?.Debounce(async () => await UpdateExportPreviewAsync(), 300);
        }

        /// <summary>
        /// Met à jour l'aperçu des données qui seront exportées de manière asynchrone
        /// </summary>
        private async Task UpdateExportPreviewAsync()
        {
            // Éviter les appels multiples simultanés
            if (_isUpdatingPreview)
            {
                return;
            }

            try
            {
                _isUpdatingPreview = true;

                // Annuler toute mise à jour précédente
                _previewUpdateCancellation?.Cancel();
                _previewUpdateCancellation = new CancellationTokenSource();
                var cancellationToken = _previewUpdateCancellation.Token;

                if (_currentTableData == null || _currentTableData.Data.Rows.Count == 0)
                {
                    ExportPreviewDataView = null;
                    ExportPreviewMessage = "Aucune donnée disponible pour l'aperçu";
                    return;
                }

                // Récupérer les colonnes sélectionnées
                var selectedColumns = AvailableColumns
                    .Where(col => col.IsSelected)
                    .Select(col => col.ColumnName)
                    .ToList();

                if (selectedColumns.Count == 0)
                {
                    ExportPreviewDataView = null;
                    ExportPreviewMessage = "Sélectionnez au moins une colonne pour voir l'aperçu";
                    return;
                }

                // Créer la DataTable en arrière-plan pour ne pas bloquer l'UI
                var previewData = await Task.Run(() =>
                {
                    var previewTable = new DataTable();

                    // Ajouter les colonnes
                    foreach (var columnName in selectedColumns)
                    {
                        if (_currentTableData.Data.Columns.Contains(columnName))
                        {
                            previewTable.Columns.Add(columnName, _currentTableData.Data.Columns[columnName]!.DataType);
                        }
                    }

                    // Ajouter les 5 premières lignes
                    int previewRowCount = Math.Min(5, _currentTableData.Data.Rows.Count);
                    for (int i = 0; i < previewRowCount; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return null;
                        }

                        var newRow = previewTable.NewRow();
                        foreach (var columnName in selectedColumns)
                        {
                            if (_currentTableData.Data.Columns.Contains(columnName))
                            {
                                newRow[columnName] = _currentTableData.Data.Rows[i][columnName];
                            }
                        }
                        previewTable.Rows.Add(newRow);
                    }

                    return new { Table = previewTable, RowCount = previewRowCount, SelectedCount = selectedColumns.Count };
                }, cancellationToken);

                if (previewData != null && !cancellationToken.IsCancellationRequested)
                {
                    // Mettre à jour l'UI sur le thread UI
                    ExportPreviewDataView = previewData.Table.DefaultView;
                    ExportPreviewMessage = $"Aperçu : {previewData.SelectedCount} colonne(s) sélectionnée(s) • {previewData.RowCount} premières lignes sur {_currentTableData.Data.Rows.Count}";
                    Logger.Instance.LogDebug($"Aperçu d'export mis à jour : {previewData.SelectedCount} colonnes, {previewData.RowCount} lignes");
                }
            }
            catch (OperationCanceledException)
            {
                // Normal - l'opération a été annulée
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Erreur lors de la mise à jour de l'aperçu d'export : {ex.Message}", ex);
                ExportPreviewDataView = null;
                ExportPreviewMessage = "Erreur lors de la génération de l'aperçu";
            }
            finally
            {
                _isUpdatingPreview = false;
            }
        }

        /// <summary>
        /// Met à jour le filtre des colonnes en fonction du texte de recherche
        /// </summary>
        private void UpdateColumnFilter()
        {
            try
            {
                // Créer une liste temporaire pour éviter de notifier à chaque Add()
                List<SelectableColumn> filteredList;

                if (string.IsNullOrWhiteSpace(_columnSearchFilter))
                {
                    // Aucun filtre : afficher toutes les colonnes
                    filteredList = new List<SelectableColumn>(AvailableColumns);
                }
                else
                {
                    // Filtrer les colonnes (insensible à la casse)
                    var filter = _columnSearchFilter.ToLowerInvariant();
                    filteredList = AvailableColumns
                        .Where(col => col.ColumnName.ToLowerInvariant().Contains(filter))
                        .ToList();
                }

                // Remplacer en une seule fois pour minimiser les notifications
                FilteredColumns.Clear();
                foreach (var column in filteredList)
                {
                    FilteredColumns.Add(column);
                }

                // Mettre à jour le statut (différé pour ne pas bloquer)
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateColumnFilterStatus();
                }), System.Windows.Threading.DispatcherPriority.Background);

                Logger.Instance.LogDebug($"Filtre de colonnes mis à jour : {FilteredColumns.Count}/{AvailableColumns.Count} colonnes affichées");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Erreur lors du filtrage des colonnes : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Met à jour le message de statut du filtre de colonnes
        /// </summary>
        private void UpdateColumnFilterStatus()
        {
            int selectedCount = AvailableColumns.Count(c => c.IsSelected);
            ColumnFilterStatus = $"{FilteredColumns.Count} colonne(s) affichée(s) • {selectedCount} sélectionnée(s)";
        }

        /// <summary>
        /// Efface le filtre de recherche
        /// </summary>
        public void ClearColumnFilter()
        {
            ColumnSearchFilter = "";
        }

        /// <summary>
        /// Sélectionne toutes les colonnes filtrées
        /// </summary>
        public void SelectAllFilteredColumns()
        {
            // Désactiver temporairement les événements pour éviter les mises à jour multiples
            foreach (var column in FilteredColumns)
            {
                column.IsSelected = true;
            }

            // Mettre à jour le statut et l'aperçu une seule fois
            UpdateColumnFilterStatus();
            UpdateExportPreview();

            Logger.Instance.LogInfo($"{FilteredColumns.Count} colonnes filtrées sélectionnées");
        }

        /// <summary>
        /// Désélectionne toutes les colonnes filtrées
        /// </summary>
        public void DeselectAllFilteredColumns()
        {
            // Désactiver temporairement les événements pour éviter les mises à jour multiples
            foreach (var column in FilteredColumns)
            {
                column.IsSelected = false;
            }

            // Mettre à jour le statut et l'aperçu une seule fois
            UpdateColumnFilterStatus();
            UpdateExportPreview();

            Logger.Instance.LogInfo($"{FilteredColumns.Count} colonnes filtrées désélectionnées");
        }

        /// <summary>
        /// Exporte les données sélectionnées en fichier Excel avec dialogue de fichier
        /// </summary>
        private async void ExportToExcel()
        {
            try
            {
                if (_currentTableData == null || _currentTableData.Data.Rows.Count == 0)
                {
                    ExportStatusMessage = "Aucune donnée à exporter";
                    Logger.Instance.LogWarning(ExportStatusMessage);
                    return;
                }

                // Récupérer les colonnes sélectionnées
                var selectedColumns = AvailableColumns
                    .Where(col => col.IsSelected)
                    .Select(col => col.ColumnName)
                    .ToList();

                if (selectedColumns.Count == 0)
                {
                    ExportStatusMessage = "Veuillez sélectionner au moins une colonne";
                    Logger.Instance.LogWarning("Aucune colonne sélectionnée pour l'export");
                    return;
                }

                // Ouvrir le dialogue de sélection de fichier
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Fichiers Excel (*.xlsx)|*.xlsx|Tous les fichiers (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"Export_{_selectedTable?.TableName ?? "data"}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    Logger.Instance.LogInfo("Export annulé par l'utilisateur");
                    return;
                }

                string filePath = saveFileDialog.FileName;

                // Préparer l'export
                ExportStatusMessage = "Chargement des données complètes...";
                IsExporting = true;
                ExportCurrentRow = 0;
                ExportProgressPercentage = 0;

                // Annuler tout export précédent
                _exportCancellationTokenSource?.Cancel();
                _exportCancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = _exportCancellationTokenSource.Token;

                Logger.Instance.LogInfo($"Chargement des données complètes pour l'export vers {filePath}");

                // Charger toutes les données (sans limite) depuis la base de données
                var fullTableData = await _mssqlConnection!.GetTableDataAsync(_selectedTable!.TableName, "DOS = 200");
                ExportTotalRows = fullTableData.Data.Rows.Count;

                Logger.Instance.LogInfo($"Données complètes chargées : {ExportTotalRows} lignes");
                ExportStatusMessage = "Création du fichier Excel...";

                // Créer un callback de progression
                Services.ExportProgressCallback progressCallback = (currentRow, totalRows, message) =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ExportCurrentRow = currentRow;
                        ExportTotalRows = totalRows;
                        ExportProgressPercentage = totalRows > 0 ? (currentRow * 100.0 / totalRows) : 0;
                        ExportProgressMessage = message;
                    });
                };

                // Exporter les données complètes de manière asynchrone
                bool success = await _excelExportService!.ExportToExcelAsync(
                    fullTableData,
                    selectedColumns,
                    filePath,
                    progressCallback,
                    cancellationToken);

                if (success)
                {
                    ExportStatusMessage = $"Export réussi !\n{System.IO.Path.GetFileName(filePath)}";
                    Logger.Instance.LogSuccess($"Export réussi : {filePath}");
                }
                else
                {
                    ExportStatusMessage = "Export échoué ou annulé";
                    Logger.Instance.LogWarning("Export échoué ou annulé");
                }
            }
            catch (Exception ex)
            {
                ExportStatusMessage = $"Erreur : {ex.Message}";
                Logger.Instance.LogError($"Exception lors de l'export : {ex.Message}", ex);
            }
            finally
            {
                IsExporting = false;
            }
        }

        /// <summary>
        /// Annule l'export en cours
        /// </summary>
        public void CancelExport()
        {
            Logger.Instance.LogInfo("Annulation de l'export demandée");
            _exportCancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Met à jour les colonnes sélectionnées
        /// </summary>
        public void UpdateSelectedColumns(System.Collections.IList selectedItems)
        {
            // Les colonnes sélectionnées sont mises à jour via la ListBox
            Logger.Instance.LogDebug($"Colonnes sélectionnées : {selectedItems.Count}");
        }

        /// <summary>
        /// Gère le changement d'état de la connexion
        /// </summary>
        private void OnConnectionStateChanged(object? sender, Interfaces.ConnectionStateChangedEventArgs e)
        {
            // Mettre à jour les propriétés liées
            IsConnected = e.IsConnected;
            StatusMessage = e.StatusMessage ?? "N/A";
            ServerName = e.ServerName ?? "N/A";
            DatabaseName = e.DatabaseName ?? "N/A";

            // Mettre à jour les boutons en fonction de l'état
            if (e.IsConnected)
            {
                IsConnectButtonEnabled = false;
                IsDisconnectButtonEnabled = true;
            }
            else
            {
                IsConnectButtonEnabled = true;
                IsDisconnectButtonEnabled = false;
            }
        }

        /// <summary>
        /// Libère les ressources
        /// </summary>
        public void Cleanup()
        {
            try
            {
                Logger.Instance.LogInfo("Nettoyage du ViewModel en cours...");

                if (_mssqlConnection != null)
                {
                    _mssqlConnection.Dispose();
                }

                Logger.Instance.LogSuccess("=== Application Divalto fermée ===");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Erreur lors du nettoyage : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lève l'événement PropertyChanged
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Définit une propriété et lève PropertyChanged si la valeur change
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
