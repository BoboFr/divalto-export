using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Divalto.Models;

namespace Divalto.Services
{
    /// <summary>
    /// Delegate pour les callbacks de progression d'export
    /// </summary>
    public delegate void ExportProgressCallback(int currentRow, int totalRows, string message);

    /// <summary>
    /// Service pour exporter les données en fichier Excel
    /// </summary>
    public class ExcelExportService
    {
        /// <summary>
        /// Exporte les données d'une table en fichier Excel de manière asynchrone avec progression
        /// </summary>
        /// <param name="tableData">Les données de la table</param>
        /// <param name="selectedColumns">Les colonnes à exporter</param>
        /// <param name="filePath">Le chemin du fichier de sortie</param>
        /// <param name="progressCallback">Callback pour rapporter la progression</param>
        /// <param name="cancellationToken">Token pour annuler l'export</param>
        /// <returns>True si l'export réussit, False sinon</returns>
        public async Task<bool> ExportToExcelAsync(TableData tableData, List<string> selectedColumns, string filePath,
            ExportProgressCallback? progressCallback = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (tableData == null || tableData.Data == null || tableData.Data.Rows.Count == 0)
                {
                    Logger.Instance.LogWarning("Aucune donnée à exporter");
                    return false;
                }

                if (selectedColumns == null || selectedColumns.Count == 0)
                {
                    Logger.Instance.LogWarning("Aucune colonne sélectionnée pour l'export");
                    return false;
                }

                Logger.Instance.LogInfo($"Début de l'export Excel vers {filePath}");
                progressCallback?.Invoke(0, tableData.Data.Rows.Count, "Préparation du fichier Excel...");

                // Exécuter l'export sur un thread séparé
                return await Task.Run(() => PerformExport(tableData, selectedColumns, filePath, progressCallback, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.Instance.LogWarning("Export annulé par l'utilisateur");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Erreur lors de l'export Excel : {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Effectue l'export réel avec suivi de progression
        /// </summary>
        private bool PerformExport(TableData tableData, List<string> selectedColumns, string filePath,
            ExportProgressCallback? progressCallback, CancellationToken cancellationToken)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(tableData.TableName);

                    // Ajouter les en-têtes
                    progressCallback?.Invoke(0, tableData.Data.Rows.Count, "Création des en-têtes...");
                    for (int colIndex = 0; colIndex < selectedColumns.Count; colIndex++)
                    {
                        worksheet.Cell(1, colIndex + 1).Value = selectedColumns[colIndex];
                        worksheet.Cell(1, colIndex + 1).Style.Font.Bold = true;
                        worksheet.Cell(1, colIndex + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    // Ajouter les données avec suivi de progression
                    int totalRows = tableData.Data.Rows.Count;
                    var startTime = DateTime.Now;

                    for (int rowIndex = 0; rowIndex < totalRows; rowIndex++)
                    {
                        // Vérifier l'annulation
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Logger.Instance.LogWarning("Export annulé");
                            return false;
                        }

                        DataRow dataRow = tableData.Data.Rows[rowIndex];
                        for (int colIndex = 0; colIndex < selectedColumns.Count; colIndex++)
                        {
                            var value = dataRow[selectedColumns[colIndex]];
                            if (value == DBNull.Value || value == null)
                            {
                                worksheet.Cell(rowIndex + 2, colIndex + 1).Value = "";
                            }
                            else
                            {
                                worksheet.Cell(rowIndex + 2, colIndex + 1).Value = value.ToString();
                            }
                        }

                        // Mettre à jour la progression tous les 100 lignes
                        if (rowIndex % 100 == 0 || rowIndex == totalRows - 1)
                        {
                            var elapsed = DateTime.Now - startTime;
                            var eta = CalculateETA(rowIndex + 1, totalRows, elapsed);
                            string message = $"Traitement des données ({rowIndex + 1}/{totalRows})... ETA: {eta}";
                            progressCallback?.Invoke(rowIndex + 1, totalRows, message);
                        }
                    }

                    // Auto-ajuster la largeur des colonnes
                    progressCallback?.Invoke(totalRows, totalRows, "Ajustement des colonnes...");
                    worksheet.Columns().AdjustToContents();

                    // Sauvegarder le fichier
                    progressCallback?.Invoke(totalRows, totalRows, "Sauvegarde du fichier...");
                    workbook.SaveAs(filePath);

                    Logger.Instance.LogSuccess($"Export Excel réussi : {filePath} ({tableData.Data.Rows.Count} lignes)");
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Instance.LogWarning("Export annulé");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Erreur lors de l'export Excel : {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Calcule l'ETA en fonction de la progression
        /// </summary>
        private string CalculateETA(int currentRow, int totalRows, TimeSpan elapsed)
        {
            if (currentRow == 0) return "Calcul...";

            double rowsPerSecond = currentRow / elapsed.TotalSeconds;
            if (rowsPerSecond <= 0) return "Calcul...";

            double remainingRows = totalRows - currentRow;
            double secondsRemaining = remainingRows / rowsPerSecond;

            if (secondsRemaining < 0) return "Terminé";
            if (secondsRemaining < 1) return "< 1s";
            if (secondsRemaining < 60) return $"{(int)secondsRemaining}s";

            int minutes = (int)(secondsRemaining / 60);
            int seconds = (int)(secondsRemaining % 60);
            return $"{minutes}m {seconds}s";
        }

        /// <summary>
        /// Exporte toutes les colonnes disponibles de manière asynchrone
        /// </summary>
        public Task<bool> ExportToExcelAsync(TableData tableData, string filePath,
            ExportProgressCallback? progressCallback = null, CancellationToken cancellationToken = default)
        {
            return ExportToExcelAsync(tableData, tableData.ColumnNames, filePath, progressCallback, cancellationToken);
        }
    }
}
