using UnityEngine;
using System;
using System.IO;
using SFB;
using UnityEngine.Events;
using System.Collections.Generic;

public class DatabaseManager : MonoBehaviour
{
    public string databasePath;

    private DatabaseUI _databaseUI;
    private DatabaseLoader _databaseLoader;
    private DatabaseTableViewer _tableViewer;
    private DatabaseSearcher _databaseSearcher;
    private DatabaseTableModifier _tableModifier;

    public UnityEvent onDataLoaded;

    private void Start()
    {
        _databaseLoader = GetComponent<DatabaseLoader>();
        _databaseUI = GetComponent<DatabaseUI>();
        _tableViewer = GetComponent<DatabaseTableViewer>();
        _databaseSearcher = GetComponent<DatabaseSearcher>();
        _tableModifier = GetComponent<DatabaseTableModifier>();
        
        Initialize();
    }

    private void Initialize()
    {
        // Инициализация компонентов и подписка на события
        if (_databaseUI != null)
        {
            _databaseUI.OnTableSelected += ActiveTableSelected;
        }

        if (_databaseSearcher != null)
        {
            _databaseSearcher.Initialize(_tableViewer);
        }
    }

    // Загрузка базы данных и получение списка таблиц
    private void LoadDatabase()
    {
        if (string.IsNullOrEmpty(databasePath))
        {
            Debug.LogError("Путь к базе данных не выбран!");
            return;
        }

        if (!File.Exists(databasePath))
        {
            Debug.LogError("Файл базы данных не найден: " + databasePath);
            return;
        }

        try
        {
            // Получаем список таблиц
            var availableTables = _databaseLoader.GetAvailableTables(databasePath);

            if (availableTables.Count < 2)
            {
                Debug.LogError("База данных должна содержать как минимум 2 таблицы!");
                return;
            }
            
            onDataLoaded.Invoke();

            // Обновляем выпадающий список и устанавливаем первые две таблицы как активные
            _databaseUI.UpdateTablesDropdown(availableTables);
            
            // Загружаем данные из первых двух таблиц
            _tableViewer.LoadStaticTables(databasePath, availableTables[0], availableTables[1]);
            
            // Устанавливаем первую таблицу как активную для поиска и редактирования
            string activeTable = availableTables[_databaseUI.tablesDropdown.value];
            _databaseSearcher.SetCurrentTable(databasePath, activeTable);
            
            Debug.Log("База данных успешно загружена");
        }
        catch (Exception ex)
        {
            Debug.LogError("Ошибка при загрузке базы данных: " + ex.Message);
        }
    }

    // Обработчик выбора активной таблицы
    private void ActiveTableSelected(string tableName)
    {
        if (string.IsNullOrEmpty(tableName)) return;
        
        // Получаем информацию о структуре таблицы
        var structure = _databaseLoader.GetTableStructure(databasePath, tableName);

        GetComponent<DatabaseTableNewRow>().OnActiveTableChanged(tableName);
        
        // Обновляем выпадающий список столбцов для поиска
        _databaseSearcher.UpdateSearchColumns(structure);

        // Устанавливаем текущую активную таблицу для поиска и модификации
        _databaseSearcher.SetCurrentTable(databasePath, tableName);
        
        // Обновляем UI с информацией о том, какая таблица активна
        _tableViewer.SetActiveTable(tableName);
        
        Debug.Log($"Активная таблица изменена на: {tableName}");
    }

    // Метод для загрузки файла базы данных через диалог
    public void LoadFile()
    {
#if UNITY_EDITOR
        var path = UnityEditor.EditorUtility.OpenFilePanel("Выбрать базу данных SQLite", "", "db");

        if (path.Length == 0) return;
        
        databasePath = path;
        LoadDatabase();
#else
        // Открыть диалог выбора файла, который работает в билде
        var paths = StandaloneFileBrowser.OpenFilePanel("Выбрать базу данных SQLite", "", "db", false);

        // Проверяем, был ли выбран файл
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

        databasePath = paths[0];
        LoadDatabase();
#endif
    }
}