using Microsoft.Extensions.Logging;
using RpaWinUIComponents.AdvancedDataGrid.Events;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUIComponents.AdvancedDataGrid.Services.Implementation;

/// <summary>
/// Implementation of navigation service for keyboard navigation and cell selection
/// </summary>
public class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;
    private List<GridDataRow> _rows = new();
    private List<ColumnDefinition> _columns = new();
    private int _currentRowIndex = -1;
    private int _currentColumnIndex = -1;
    private CellViewModel? _currentCell;

    public NavigationService(ILogger<NavigationService>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NavigationService>.Instance;
    }

    public CellViewModel? CurrentCell
    {
        get => _currentCell;
        private set
        {
            if (_currentCell != value)
            {
                _currentCell = value;
                _logger.LogTrace("Current cell changed to: {ColumnName}", _currentCell?.ColumnName);
            }
        }
    }

    public int CurrentRowIndex => _currentRowIndex;
    public int CurrentColumnIndex => _currentColumnIndex;

    public event EventHandler<CellNavigationEventArgs>? CellChanged;
    public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

    public void Initialize(List<GridDataRow> rows, List<ColumnDefinition> columns)
    {
        try
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));

            _currentRowIndex = -1;
            _currentColumnIndex = -1;
            CurrentCell = null;

            _logger.LogInformation("NavigationService initialized with {RowCount} rows and {ColumnCount} columns",
                _rows.Count, _columns.Count);

            if (_rows.Count > 0)
            {
                var editableColumns = GetEditableColumns();
                if (editableColumns.Count > 0)
                {
                    MoveToCell(0, 0);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing NavigationService");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "Initialize"));
        }
    }

    public void MoveToNextCell()
    {
        try
        {
            var editableColumns = GetEditableColumns();
            if (editableColumns.Count == 0 || _rows.Count == 0) return;

            var oldRowIndex = _currentRowIndex;
            var oldColumnIndex = _currentColumnIndex;
            var oldCell = CurrentCell;

            var nextColumnIndex = _currentColumnIndex + 1;
            var nextRowIndex = _currentRowIndex;

            if (nextColumnIndex >= editableColumns.Count)
            {
                nextColumnIndex = 0;
                nextRowIndex = _currentRowIndex + 1;
                if (nextRowIndex >= _rows.Count)
                    nextRowIndex = 0;
            }

            MoveToCell(nextRowIndex, nextColumnIndex);

            _logger.LogDebug("Moved to next cell: [{Row},{Col}]", nextRowIndex, nextColumnIndex);

            OnCellChanged(new CellNavigationEventArgs
            {
                OldRowIndex = oldRowIndex,
                OldColumnIndex = oldColumnIndex,
                NewRowIndex = _currentRowIndex,
                NewColumnIndex = _currentColumnIndex,
                OldCell = oldCell,
                NewCell = CurrentCell
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving to next cell");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToNextCell"));
        }
    }

    public void MoveToPreviousCell()
    {
        try
        {
            var editableColumns = GetEditableColumns();
            if (editableColumns.Count == 0 || _rows.Count == 0) return;

            var oldRowIndex = _currentRowIndex;
            var oldColumnIndex = _currentColumnIndex;
            var oldCell = CurrentCell;

            var prevColumnIndex = _currentColumnIndex - 1;
            var prevRowIndex = _currentRowIndex;

            if (prevColumnIndex < 0)
            {
                prevColumnIndex = editableColumns.Count - 1;
                prevRowIndex = _currentRowIndex - 1;
                if (prevRowIndex < 0)
                    prevRowIndex = _rows.Count - 1;
            }

            MoveToCell(prevRowIndex, prevColumnIndex);

            _logger.LogDebug("Moved to previous cell: [{Row},{Col}]", prevRowIndex, prevColumnIndex);

            OnCellChanged(new CellNavigationEventArgs
            {
                OldRowIndex = oldRowIndex,
                OldColumnIndex = oldColumnIndex,
                NewRowIndex = _currentRowIndex,
                NewColumnIndex = _currentColumnIndex,
                OldCell = oldCell,
                NewCell = CurrentCell
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving to previous cell");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToPreviousCell"));
        }
    }

    public void MoveToNextRow()
    {
        try
        {
            if (_rows.Count == 0) return;

            var oldRowIndex = _currentRowIndex;
            var oldColumnIndex = _currentColumnIndex;
            var oldCell = CurrentCell;

            var nextRowIndex = (_currentRowIndex + 1) % _rows.Count;
            MoveToCell(nextRowIndex, _currentColumnIndex);

            _logger.LogDebug("Moved to next row: {Row}", nextRowIndex);

            OnCellChanged(new CellNavigationEventArgs
            {
                OldRowIndex = oldRowIndex,
                OldColumnIndex = oldColumnIndex,
                NewRowIndex = _currentRowIndex,
                NewColumnIndex = _currentColumnIndex,
                OldCell = oldCell,
                NewCell = CurrentCell
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving to next row");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToNextRow"));
        }
    }

    public void MoveToPreviousRow()
    {
        try
        {
            if (_rows.Count == 0) return;

            var oldRowIndex = _currentRowIndex;
            var oldColumnIndex = _currentColumnIndex;
            var oldCell = CurrentCell;

            var prevRowIndex = _currentRowIndex - 1;
            if (prevRowIndex < 0)
                prevRowIndex = _rows.Count - 1;

            MoveToCell(prevRowIndex, _currentColumnIndex);

            _logger.LogDebug("Moved to previous row: {Row}", prevRowIndex);

            OnCellChanged(new CellNavigationEventArgs
            {
                OldRowIndex = oldRowIndex,
                OldColumnIndex = oldColumnIndex,
                NewRowIndex = _currentRowIndex,
                NewColumnIndex = _currentColumnIndex,
                OldCell = oldCell,
                NewCell = CurrentCell
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving to previous row");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToPreviousRow"));
        }
    }

    public void MoveToCell(int rowIndex, int columnIndex)
    {
        try
        {
            if (rowIndex < 0 || rowIndex >= _rows.Count) return;

            var editableColumns = GetEditableColumns();
            if (columnIndex < 0 || columnIndex >= editableColumns.Count) return;

            var oldRowIndex = _currentRowIndex;
            var oldColumnIndex = _currentColumnIndex;
            var oldCell = CurrentCell;

            _currentRowIndex = rowIndex;
            _currentColumnIndex = columnIndex;

            var columnName = editableColumns[columnIndex].Name;
            CurrentCell = _rows[rowIndex].GetCell(columnName);

            _logger.LogTrace("Moved to cell: [{Row},{Col}] = {ColumnName}", rowIndex, columnIndex, columnName);

            if (oldRowIndex != _currentRowIndex || oldColumnIndex != _currentColumnIndex)
            {
                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving to cell [{Row},{Col}]", rowIndex, columnIndex);
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToCell"));
        }
    }

    private List<ColumnDefinition> GetEditableColumns()
    {
        return _columns.Where(c => !c.IsSpecialColumn).ToList();
    }

    protected virtual void OnCellChanged(CellNavigationEventArgs e)
    {
        CellChanged?.Invoke(this, e);
    }

    protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
    {
        ErrorOccurred?.Invoke(this, e);
    }
}