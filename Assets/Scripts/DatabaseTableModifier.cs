using UnityEngine;
using System;
using Mono.Data.Sqlite;
using UnityEngine.Events;

public class DatabaseTableModifier : MonoBehaviour
{
    private DatabaseTableViewer _tableViewer;
    
    [Tooltip("Событие вызывается после успешного обновления данных")]
    public UnityEvent<string, string> onCellUpdated;
    
    private void Start()
    {
        _tableViewer = GetComponent<DatabaseTableViewer>();
    }
    
    // Обновление значения ячейки в базе данных
    public void UpdateCellValue(string tableName, string columnName, string primaryKeyColumn, string primaryKeyValue, string newValue)
    {
        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(columnName) || 
            string.IsNullOrEmpty(primaryKeyColumn) || string.IsNullOrEmpty(primaryKeyValue))
        {
            Debug.LogError("Недостаточно данных для обновления ячейки");
            return;
        }
        
        var dbPath = GetCurrentDatabasePath();
        if (string.IsNullOrEmpty(dbPath))
        {
            Debug.LogError("Путь к базе данных не найден");
            return;
        }
        
        try
        {
            // Создаем SQL-запрос для обновления значения
            var query = $"UPDATE {tableName} SET {columnName} = @newValue WHERE {primaryKeyColumn} = @primaryKeyValue";
            
            using (var connection = new SqliteConnection($"URI=file:{dbPath}"))
            {
                connection.Open();
                
                using (var command = new SqliteCommand(query, connection))
                {
                    // Добавляем параметры для безопасного обновления
                    command.Parameters.AddWithValue("@newValue", newValue);
                    command.Parameters.AddWithValue("@primaryKeyValue", primaryKeyValue);
                    
                    // Выполняем запрос
                    var rowsAffected = command.ExecuteNonQuery();
                    
                    if (rowsAffected > 0)
                    {
                        Debug.Log($"Значение в ячейке {columnName} успешно обновлено");
                        
                        // Вызываем событие об успешном обновлении
                        onCellUpdated?.Invoke(tableName, columnName);
                        
                        // Обновляем отображение таблицы
                        _tableViewer.LoadTableData(dbPath, tableName);
                    }
                    else
                    {
                        Debug.LogWarning($"Ни одна строка не была обновлена. Проверьте условие поиска.");
                    }
                }
                
                connection.Close();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при обновлении значения в ячейке: {ex.Message}");
        }
    }
    
    public void DeleteRow(string tableName, string primaryKeyColumn, string primaryKeyValue)
    {
        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(primaryKeyColumn) || string.IsNullOrEmpty(primaryKeyValue))
        {
            Debug.LogError("Недостаточно данных для удаления строки");
            return;
        }

        var dbPath = GetCurrentDatabasePath();
        if (string.IsNullOrEmpty(dbPath))
        {
            Debug.LogError("Путь к базе данных не найден");
            return;
        }

        try
        {
            var query = $"DELETE FROM {tableName} WHERE {primaryKeyColumn} = @primaryKeyValue";

            using (var connection = new SqliteConnection($"URI=file:{dbPath}"))
            {
                connection.Open();
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@primaryKeyValue", primaryKeyValue);

                    var rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Debug.Log($"Строка с ключом {primaryKeyValue} успешно удалена");

                        // Обновляем UI
                        _tableViewer.LoadTableData(dbPath, tableName);
                    }
                    else
                    {
                        Debug.LogWarning("Строка не найдена для удаления.");
                    }
                }
                connection.Close();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при удалении строки: {ex.Message}");
        }
    }
    
    // Получение текущего пути к базе данных
    private string GetCurrentDatabasePath()
    {
        // Получаем путь из компонента DatabaseManager
        var databaseManager = GetComponent<DatabaseManager>();
        if (databaseManager != null)
        {
            return databaseManager.databasePath;
        }
        
        return null;
    }
}