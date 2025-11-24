using System;
using System.Collections.Generic;
using System.Data;

namespace Divalto.Models
{
    /// <summary>
    /// Représente les données d'une table SQL
    /// </summary>
    public class TableData
    {
        public string TableName { get; set; } = "";
        public List<string> ColumnNames { get; set; }
        public DataTable Data { get; set; }

        public TableData()
        {
            ColumnNames = new List<string>();
            Data = new DataTable();
        }

        public TableData(string tableName) : this()
        {
            TableName = tableName;
        }
    }
}
