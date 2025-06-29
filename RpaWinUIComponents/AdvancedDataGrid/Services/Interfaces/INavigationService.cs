using RpaWinUIComponents.AdvancedDataGrid.Events;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;

namespace RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;

/// <summary>
/// Service for handling keyboard navigation and cell selection
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Initializes navigation with rows and columns
    /// </summary>
    void Initialize(List<GridDataRow> rows, List<ColumnDefinition> columns);

    /// <summary>
    /// Moves to the next cell
    /// </summary>
    void MoveToNextCell();

    /// <summary>
    /// Moves to the previous cell
    /// </summary>
    void MoveToPreviousCell();

    /// <summary>
    /// Moves to the next row (same column)
    /// </summary>
    void MoveToNextRow();

    /// <summary>
    /// Moves to the previous row (same column)
    /// </summary>
    void MoveToPreviousRow();

    /// <summary>
    /// Moves to specific cell coordinates
    /// </summary>
    void MoveToCell(int rowIndex, int columnIndex);

    /// <summary>
    /// Gets current cell
    /// </summary>
    CellViewModel? CurrentCell { get; }

    /// <summary>
    /// Gets current row index
    /// </summary>
    int CurrentRowIndex { get; }

    /// <summary>
    /// Gets current column index
    /// </summary>
    int CurrentColumnIndex { get; }

    /// <summary>
    /// Event fired when cell selection changes
    /// </summary>
    event EventHandler<CellNavigationEventArgs>? CellChanged;

    /// <summary>
    /// Event fired when navigation error occurs
    /// </summary>
    event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
}

/// <summary>
/// Arguments for cell navigation events
/// </summary>
public class CellNavigationEventArgs : EventArgs
{
    public int OldRowIndex { get; set; }
    public int OldColumnIndex { get; set; }
    public int NewRowIndex { get; set; }
    public int NewColumnIndex { get; set; }
    public CellViewModel? OldCell { get; set; }
    public CellViewModel? NewCell { get; set; }
}