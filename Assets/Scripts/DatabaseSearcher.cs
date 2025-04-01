using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class DatabaseSearcher : MonoBehaviour
{
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button searchButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Dropdown searchColumnDropdown;
    
    private DatabaseLoader _databaseLoader;
    private DatabaseTableViewer _tableViewer;
    private DatabaseTableModifier _tableModifier;
    
    private List<string> _currentColumns = new List<string>();
    private string _currentTable = "";
    private string _currentDbPath = "";
    private string _primaryKeyColumn = "";
    
    private void Awake()
    {
        _databaseLoader = GetComponent<DatabaseLoader>();
        _tableModifier = GetComponent<DatabaseTableModifier>();
    
        // Инициализация кнопок
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(SearchInDatabase);
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(DeleteBySearchId);
        }
        
        // Добавляем слушатель для поиска при нажатии Enter
        if (searchInputField != null)
        {
            searchInputField.onEndEdit.AddListener(OnSearchFieldEndEdit);
        }
    }
    
    public void Initialize(DatabaseTableViewer viewer)
    {
        _tableViewer = viewer;
    }
    
    // Установка текущей таблицы для поиска
    public void SetCurrentTable(string dbPath, string tableName)
    {
        _currentDbPath = dbPath;
        _currentTable = tableName;
        
        // Определяем первичный ключ активной таблицы
        IdentifyPrimaryKey();
    }
    
    // Определение первичного ключа текущей таблицы
    private void IdentifyPrimaryKey()
    {
        try
        {
            var query = $"PRAGMA table_info({_currentTable})";
            var tableInfo = _databaseLoader.ExecuteQuery(_currentDbPath, query);
            
            _primaryKeyColumn = null;
            
            // Ищем столбец с primary key
            foreach (System.Data.DataRow row in tableInfo.Rows)
            {
                if (Convert.ToInt32(row["pk"]) > 0)
                {
                    _primaryKeyColumn = row["name"].ToString();
                    Debug.Log($"Первичный ключ для таблицы {_currentTable}: {_primaryKeyColumn}");
                    break;
                }
            }
            
            // Если не нашли PK, используем первый столбец
            if (string.IsNullOrEmpty(_primaryKeyColumn) && tableInfo.Rows.Count > 0)
            {
                _primaryKeyColumn = tableInfo.Rows[0]["name"].ToString();
                Debug.Log($"Первичный ключ не найден, используем столбец {_primaryKeyColumn} как идентификатор");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при определении первичного ключа: {ex.Message}");
            _primaryKeyColumn = null;
        }
    }
    
    // Обновление выпадающего списка столбцов для поиска
    public void UpdateSearchColumns(List<TableColumn> columns)
    {
        _currentColumns.Clear();
        foreach (var column in columns)
        {
            _currentColumns.Add(column.Name);
        }
        
        UpdateSearchColumnDropdown();
    }
    
    // Обновление выпадающего списка столбцов для поиска
    private void UpdateSearchColumnDropdown()
    {
        if (searchColumnDropdown == null)
            return;
            
        searchColumnDropdown.ClearOptions();
        
        // Добавляем опцию для поиска по всем столбцам
        var options = new List<TMP_Dropdown.OptionData>();
        options.Add(new TMP_Dropdown.OptionData("Все столбцы"));
        
        foreach (var columnName in _currentColumns)
        {
            options.Add(new TMP_Dropdown.OptionData(columnName));
        }
        
        searchColumnDropdown.AddOptions(options);
        searchColumnDropdown.value = 0; // По умолчанию "Все столбцы"
    }
    
    // Метод для обработки нажатия Enter в поле поиска
    private void OnSearchFieldEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SearchInDatabase();
        }
    }
    
    // Метод поиска строк в базе данных
    private void SearchInDatabase()
    {
        if (searchInputField == null || string.IsNullOrEmpty(_currentTable) || string.IsNullOrEmpty(_currentDbPath))
            return;
            
        var searchText = searchInputField.text;
        
        if (string.IsNullOrEmpty(searchText))
        {
            // Если поле поиска пустое, показываем все данные
            _tableViewer.ReloadActiveTable();
            return;
        }
        
        // Определяем, по какому столбцу искать
        var selectedColumn = "Все столбцы";
        if (searchColumnDropdown != null && searchColumnDropdown.value > 0)
        {
            selectedColumn = _currentColumns[searchColumnDropdown.value - 1]; // -1 так как первый элемент "Все столбцы"
        }
        
        try
        {
            string query;
            
            if (selectedColumn == "Все столбцы")
            {
                // Создаем SQL запрос для поиска по всем столбцам
                var whereClause = new StringBuilder();
                
                for (var i = 0; i < _currentColumns.Count; i++)
                {
                    if (i > 0) whereClause.Append(" OR ");
                    whereClause.Append($"{_currentColumns[i]} LIKE @searchParam");
                }
                
                query = $"SELECT * FROM {_currentTable} WHERE {whereClause} LIMIT 100";
            }
            else
            {
                // Создаем SQL запрос для поиска по выбранному столбцу
                query = $"SELECT * FROM {_currentTable} WHERE {selectedColumn} LIKE @searchParam LIMIT 100";
            }
            
            // Выполняем запрос с параметром
            var searchResults = _databaseLoader.ExecuteParameterizedQuery(_currentDbPath, query, "@searchParam", $"%{searchText}%");
            
            // Отображаем результаты поиска
            _tableViewer.DisplaySearchResults(searchResults);
            
            Debug.Log($"Поиск выполнен. Найдено строк: {searchResults.Rows.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при выполнении поиска: {ex.Message}");
        }
    }
    
    // Метод для удаления записи по ID из поля поиска
    private void DeleteBySearchId()
    {
        if (string.IsNullOrEmpty(searchInputField.text) || string.IsNullOrEmpty(_primaryKeyColumn))
            return;
            
        try
        {
            // Предполагаем, что в поле поиска введен ID для удаления
            string idToDelete = searchInputField.text.Trim();
            
            // Удаляем запись
            _tableModifier.DeleteRow(_currentTable, _primaryKeyColumn, idToDelete);
            
            // Очищаем поле поиска
            searchInputField.text = "";
            
            // Обновляем отображение таблицы
            _tableViewer.ReloadActiveTable();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при удалении записи: {ex.Message}");
        }
    }
    
    // Метод для поиска записи для редактирования
    private void FindForEdit()
    {
        if (string.IsNullOrEmpty(searchInputField.text) || string.IsNullOrEmpty(_primaryKeyColumn))
            return;
            
        try
        {
            string idToEdit = searchInputField.text.Trim();
            
            // Создаем запрос для поиска записи по первичному ключу
            string query = $"SELECT * FROM {_currentTable} WHERE {_primaryKeyColumn} = @id";
            
            // Выполняем запрос с параметром
            var result = _databaseLoader.ExecuteParameterizedQuery(_currentDbPath, query, "@id", idToEdit);
            
            if (result.Rows.Count > 0)
            {
                // Отображаем результат поиска для редактирования
                _tableViewer.DisplaySearchResults(result);
                Debug.Log($"Запись найдена для редактирования");
            }
            else
            {
                Debug.LogWarning($"Запись с ID {idToEdit} не найдена");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при поиске записи для редактирования: {ex.Message}");
        }
    }
}