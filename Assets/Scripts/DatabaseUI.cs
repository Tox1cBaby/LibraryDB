using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;

public class DatabaseUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown tablesDropdown;
    public TMP_Text activeTableLabel;
    
    // Событие выбора таблицы
    public event Action<string> OnTableSelected;
    
    public List<string> availableTables = new List<string>();
    
    private void Start()
    {
        // Регистрация события выбора таблицы
        tablesDropdown.onValueChanged.AddListener(OnTableDropdownChanged);
    }
    
    // Обработчик изменения выбора в выпадающем списке
    private void OnTableDropdownChanged(int index)
    {
        if (index < 0 || index >= availableTables.Count) return;
        
        var selectedTable = availableTables[index];
        Debug.Log($"Выбрана активная таблица: {selectedTable}");
        
        if (activeTableLabel != null)
        {
            activeTableLabel.text = $"Активная таблица: {selectedTable}";
        }
            
        // Вызываем событие выбора таблицы
        OnTableSelected?.Invoke(selectedTable);
    }
    
    // Обновление выпадающего списка таблиц
    public void UpdateTablesDropdown(List<string> tables)
    {
        availableTables = tables;
        tablesDropdown.ClearOptions();
        
        var options = new List<TMP_Dropdown.OptionData>();
        
        foreach (var tableName in availableTables)
        {
            options.Add(new TMP_Dropdown.OptionData(tableName));
        }
        
        tablesDropdown.AddOptions(options);
        
        // Если есть таблицы, выбираем первую по умолчанию
        if (availableTables.Count <= 0) return;
        
        tablesDropdown.value = 0;
        OnTableDropdownChanged(0);
    }
}