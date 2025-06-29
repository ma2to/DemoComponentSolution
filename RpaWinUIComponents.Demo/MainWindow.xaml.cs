// RpaWinUIComponents.Demo/MainWindow.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RpaWinUIComponents.AdvancedDataGrid.Helpers;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using RpaWinUIComponents.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace RpaWinUIComponents.Demo;

/// <summary>
/// Demo window showcasing AdvancedDataGrid functionality
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IHost _host;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow()
    {
        this.InitializeComponent();

        // Configure dependency injection
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddRpaWinUIComponents();
            })
            .Build();

        _logger = _host.Services.GetRequiredService<ILogger<MainWindow>>();

        this.Loaded += OnLoaded;
        this.Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("🚀 Inicializujem AdvancedDataGrid Demo...");

            // Initialize the DataGrid with sample configuration
            await InitializeDataGridAsync();

            _logger.LogInformation("✅ Demo úspešne inicializované!");
            UpdateStatusText("Demo pripravené - skúste editovať bunky, používať Tab/Enter navigáciu a kopírovať/vložiť dáta");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri inicializácii demo");
            await ShowErrorDialog("Chyba pri inicializácii", ex.Message);
        }
    }

    private async Task InitializeDataGridAsync()
    {
        try
        {
            // Define columns
            var columns = new List<ColumnDefinition>
            {
                new("Name", typeof(string), 150, 300) { DisplayName = "Meno" },
                new("Age", typeof(int), 80, 120) { DisplayName = "Vek" },
                new("Email", typeof(string), 200, 400) { DisplayName = "Email" },
                new("Salary", typeof(decimal), 100, 150) { DisplayName = "Plat" },
                new("Department", typeof(string), 120, 200) { DisplayName = "Oddelenie" },
                new("StartDate", typeof(DateTime), 120, 180) { DisplayName = "Dátum nástupu" }
            };

            // Define validation rules with Slovak error messages
            var validationRules = new List<ValidationRule>
            {
                // Name validations
                ValidationHelper.Required("Name", "Meno je povinné"),
                ValidationHelper.Length("Name", 2, 50, "Meno musí mať 2-50 znakov"),

                // Age validations
                ValidationHelper.Range("Age", 18, 65, "Vek musí byť medzi 18-65 rokmi"),

                // Email validations
                ValidationHelper.Required("Email", "Email je povinný"),
                ValidationHelper.Email("Email", "Email musí mať platný formát"),

                // Salary validations
                ValidationHelper.Range("Salary", 1000, 999999, "Plat musí byť medzi 1000-999999€"),

                // Department validation
                ValidationHelper.Required("Department", "Oddelenie je povinné"),

                // Conditional validation - senior employees must have higher salary
                ValidationHelper.Conditional(
                    "Salary",
                    (value, row) =>
                    {
                        if (decimal.TryParse(value?.ToString(), out decimal salary))
                        {
                            return salary >= 3000;
                        }
                        return false;
                    },
                    row =>
                    {
                        var age = row.GetValue<int>("Age");
                        return age > 50;
                    },
                    "Zamestnanci nad 50 rokov musia mať plat aspoň 3000€"
                )
            };

            // Configure throttling for better performance
            var throttlingConfig = ThrottlingConfig.Default;

            _logger.LogDebug("Inicializujem DataGrid s {ColumnCount} stĺpcami a {RuleCount} validačnými pravidlami",
                columns.Count, validationRules.Count);

            // Initialize the DataGrid
            await MainDataGrid.InitializeAsync(columns, validationRules, throttlingConfig, 100);

            // Load sample data
            await LoadSampleDataAsync();

            UpdateRowCount();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri inicializácii DataGrid");
            throw;
        }
    }

    private async Task LoadSampleDataAsync()
    {
        try
        {
            var sampleData = CreateSampleDataTable();
            _logger.LogDebug("Načítavam {RowCount} vzorových záznamov", sampleData.Rows.Count);

            await MainDataGrid.LoadDataAsync(sampleData);

            UpdateRowCount();
            UpdateStatusText($"Načítané {sampleData.Rows.Count} vzorových záznamov s real-time validáciou");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri načítavaní vzorových dát");
            throw;
        }
    }

    private DataTable CreateSampleDataTable()
    {
        var dataTable = new DataTable();

        // Add columns
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Age", typeof(int));
        dataTable.Columns.Add("Email", typeof(string));
        dataTable.Columns.Add("Salary", typeof(decimal));
        dataTable.Columns.Add("Department", typeof(string));
        dataTable.Columns.Add("StartDate", typeof(DateTime));

        // Add sample data (mix of valid and invalid for testing)
        var sampleData = new object[][]
        {
            // Valid records
            new object[] { "Ján Novák", 25, "jan.novak@company.sk", 2500m, "IT", DateTime.Now.AddYears(-2) },
            new object[] { "Mária Svobodová", 30, "maria.svoboda@company.sk", 3200m, "HR", DateTime.Now.AddYears(-5) },
            new object[] { "Peter Dvořák", 45, "peter.dvorak@company.sk", 4500m, "Finance", DateTime.Now.AddYears(-10) },
            new object[] { "Anna Nováková", 28, "anna.novakova@company.sk", 2800m, "Marketing", DateTime.Now.AddYears(-3) },
            new object[] { "Tomáš Krejčí", 35, "tomas.krejci@company.sk", 3800m, "IT", DateTime.Now.AddYears(-7) },

            // Invalid records for testing validation
            new object[] { "", 17, "invalid-email", 500m, "", DateTime.Now },                    // Multiple validation errors
            new object[] { "A", 70, "missing@", 200m, "Test", DateTime.Now.AddYears(1) },       // Short name, high age, bad email, low salary
            new object[] { "Senior Employee", 55, "senior@company.sk", 2500m, "Management", DateTime.Now.AddYears(-20) }, // Senior with low salary
            new object[] { "Junior Dev", 22, "junior@company.sk", 1500m, "IT", DateTime.Now.AddMonths(-6) },

            // Partially filled records
            new object[] { "Incomplete Record", null, "", null, "Sales", null },
            new object[] { "Test User", 30, "test@company.sk", null, "", DateTime.Now }
        };

        foreach (var row in sampleData)
        {
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    #region Event Handlers

    private async void ValidateAllButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateStatusText("Spúšťam validáciu všetkých riadkov...");
            var isValid = await MainDataGrid.ValidateAllAsync();

            var message = isValid ? "✅ Všetky dáta sú validné!" : "⚠️ Nájdené nevalidné dáta!";
            UpdateStatusText(message);

            await ShowInfoDialog("Validácia dokončená", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri validácii");
            await ShowErrorDialog("Chyba pri validácii", ex.Message);
        }
    }

    private async void ClearDataButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await MainDataGrid.ClearAllDataAsync();
            UpdateStatusText("Všetky dáta vymazané");
            UpdateRowCount();
            await ShowInfoDialog("Dokončené", "Všetky dáta boli vymazané!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri mazaní dát");
            await ShowErrorDialog("Chyba pri mazaní", ex.Message);
        }
    }

    private async void RemoveEmptyRowsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await MainDataGrid.RemoveEmptyRowsAsync();
            UpdateStatusText("Prázdne riadky odstránené");
            UpdateRowCount();
            await ShowInfoDialog("Dokončené", "Prázdne riadky boli odstránené!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri odstraňovaní prázdnych riadkov");
            await ShowErrorDialog("Chyba pri odstraňovaní", ex.Message);
        }
    }

    private async void RemoveInvalidRowsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Custom validation rules for removal
            var customRules = new List<ValidationRule>
            {
                ValidationHelper.Range("Age", 20, 60, "Vek mimo rozsahu 20-60"),
                ValidationHelper.Required("Email", "Chýba email"),
                ValidationHelper.Range("Salary", 2000, 10000, "Plat mimo rozsahu 2000-10000")
            };

            var removedCount = await MainDataGrid.RemoveRowsByValidationAsync(customRules);
            UpdateStatusText($"Odstránené {removedCount} nevalidných riadkov");
            UpdateRowCount();

            await ShowInfoDialog("Custom validácia", $"Odstránené {removedCount} riadkov podľa vlastných pravidiel!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri custom validácii");
            await ShowErrorDialog("Chyba pri validácii", ex.Message);
        }
    }

    private async void LoadFromDatabaseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateStatusText("Načítavam dáta z databázy...");

            // Simulate database loading
            var databaseData = await SimulateLoadFromDatabaseAsync();
            await MainDataGrid.LoadDataAsync(databaseData);

            UpdateRowCount();
            UpdateStatusText($"Načítané {databaseData.Rows.Count} záznamov z databázy");

            await ShowInfoDialog("Databáza", "Dáta z databázy boli úspešne načítané!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri načítavaní z databázy");
            await ShowErrorDialog("Chyba pri načítavaní", ex.Message);
        }
    }

    private async void ExportDataButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataTable = await MainDataGrid.ExportDataAsync();
            UpdateStatusText($"Export úspešný - {dataTable.Rows.Count} riadkov");

            await ShowInfoDialog("Export",
                $"Export úspešný!\n" +
                $"Počet riadkov: {dataTable.Rows.Count}\n" +
                $"Počet stĺpcov: {dataTable.Columns.Count}\n\n" +
                $"Dáta sú pripravené na ďalšie spracovanie.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri exporte");
            await ShowErrorDialog("Chyba pri exporte", ex.Message);
        }
    }

    private async void CustomValidationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Example of adding custom validation rules at runtime
            await ShowInfoDialog("Custom validácia",
                "Demo vlastných validačných pravidiel:\n\n" +
                "• Vek 18-65\n" +
                "• Email povinný a platný formát\n" +
                "• Plat 1000-999999€\n" +
                "• Seniori (50+) musia mať plat min. 3000€\n\n" +
                "Skúste editovať bunky a validácia sa spustí real-time!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri zobrazovaní custom validácie");
            await ShowErrorDialog("Chyba", ex.Message);
        }
    }

    private async void ShowStatsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get statistics from the grid
            var dataTable = await MainDataGrid.ExportDataAsync();
            var totalRows = dataTable.Rows.Count;
            var totalColumns = dataTable.Columns.Count;

            // Calculate some basic statistics
            var employees = dataTable.AsEnumerable();
            var avgAge = employees.Where(r => r.Field<int?>("Age").HasValue)
                                 .Average(r => r.Field<int>("Age"));
            var avgSalary = employees.Where(r => r.Field<decimal?>("Salary").HasValue)
                                   .Average(r => r.Field<decimal>("Salary"));

            var departments = employees.Where(r => !string.IsNullOrEmpty(r.Field<string>("Department")))
                                     .GroupBy(r => r.Field<string>("Department"))
                                     .Select(g => $"{g.Key}: {g.Count()}")
                                     .ToList();

            var stats = $"📊 Štatistiky DataGrid:\n\n" +
                       $"Celkový počet riadkov: {totalRows}\n" +
                       $"Počet stĺpcov: {totalColumns}\n" +
                       $"Priemerný vek: {avgAge:F1} rokov\n" +
                       $"Priemerný plat: {avgSalary:F0}€\n\n" +
                       $"Oddelenia:\n{string.Join("\n", departments)}";

            await ShowInfoDialog("Štatistiky", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri zobrazovaní štatistík");
            await ShowErrorDialog("Chyba", ex.Message);
        }
    }

    #endregion

    #region Helper Methods

    private async Task<DataTable> SimulateLoadFromDatabaseAsync()
    {
        // Simulate database delay
        await Task.Delay(1000);

        var dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Age", typeof(int));
        dataTable.Columns.Add("Email", typeof(string));
        dataTable.Columns.Add("Salary", typeof(decimal));
        dataTable.Columns.Add("Department", typeof(string));
        dataTable.Columns.Add("StartDate", typeof(DateTime));

        // Simulated database records
        var dbData = new object[][]
        {
            new object[] { "Database User 1", 28, "user1@db.com", 3000m, "Development", DateTime.Now.AddYears(-3) },
            new object[] { "Database User 2", 35, "user2@db.com", 3500m, "Testing", DateTime.Now.AddYears(-6) },
            new object[] { "Database User 3", 42, "user3@db.com", 4200m, "DevOps", DateTime.Now.AddYears(-8) },
            new object[] { "Senior Developer", 38, "senior.dev@db.com", 5000m, "Development", DateTime.Now.AddYears(-12) },
            new object[] { "Project Manager", 45, "pm@db.com", 4800m, "Management", DateTime.Now.AddYears(-15) }
        };

        foreach (var row in dbData)
        {
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    private void UpdateStatusText(string text)
    {
        StatusTextBlock.Text = text;
        _logger.LogInformation("Status: {Status}", text);
    }

    private void UpdateRowCount()
    {
        try
        {
            // This would need to be implemented to get actual row count from the grid
            // For now, we'll show a placeholder
            RowCountTextBlock.Text = "Riadkov: Aktívne";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri aktualizácii počtu riadkov");
        }
    }

    private async Task ShowInfoDialog(string title, string message)
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var dialog = new ContentDialog()
        {
            Title = $"❌ {title}",
            Content = $"Chyba: {message}",
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    #endregion

    #region Cleanup

    private void OnClosed(object sender, WindowEventArgs args)
    {
        try
        {
            _logger.LogInformation("Zatváram Demo aplikáciu...");
            _host?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri zatváraní aplikácie");
        }
    }

    #endregion
}