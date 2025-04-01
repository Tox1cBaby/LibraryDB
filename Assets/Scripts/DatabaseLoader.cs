using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.Collections.Generic;

public class DatabaseLoader : MonoBehaviour
{
    // Получение списка таблиц из базы данных
    public List<string> GetAvailableTables(string dbPath)
    {
        var availableTables = new List<string>();
        
        var query = @"
            SELECT name 
            FROM sqlite_master 
            WHERE type='table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name";
            
        var result = ExecuteQuery(dbPath, query);
        
        foreach (DataRow row in result.Rows)
        {
            availableTables.Add(row["name"].ToString());
        }
        
        Debug.Log($"Найдено таблиц: {availableTables.Count}");
        return availableTables;
    }
    
    // Получение структуры таблицы
    public List<TableColumn> GetTableStructure(string dbPath, string tableName)
    {
        var query = $"PRAGMA table_info({tableName})";
        var structure = ExecuteQuery(dbPath, query);
        
        var columns = new List<TableColumn>();
        
        foreach (DataRow row in structure.Rows)
        {
            var columnName = row["name"].ToString();
            var columnType = row["type"].ToString();
            
            columns.Add(new TableColumn { Name = columnName, Type = columnType });
        }
        
        return columns;
    }
    
    // Выполнение SQL-запроса и получение результата в виде DataTable
    public DataTable ExecuteQuery(string dbPath, string query)
    {
        var dataTable = new DataTable();
        
        using (SqliteConnection connection = new SqliteConnection($"URI=file:{dbPath}"))
        {
            connection.Open();
            
            using (SqliteCommand command = new SqliteCommand(query, connection))
            {
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }
            
            connection.Close();
        }
        
        return dataTable;
    }
    
    // Выполнение параметризованного SQL-запроса для безопасного поиска
    public DataTable ExecuteParameterizedQuery(string dbPath, string query, string paramName, string paramValue)
    {
        var dataTable = new DataTable();
        
        using (SqliteConnection connection = new SqliteConnection($"URI=file:{dbPath}"))
        {
            connection.Open();
            
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue(paramName, paramValue);
                
                using (var reader = command.ExecuteReader())
                {
                    dataTable.Load(reader);
                }
            }
            
            connection.Close();
        }
        
        return dataTable;
    }
}

// Класс для хранения информации о столбце таблицы
[System.Serializable]
public class TableColumn
{
    public string Name;
    public string Type;
}