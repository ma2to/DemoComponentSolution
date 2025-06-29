using System;

namespace RpaWinUIComponents.AdvancedDataGrid.Models;

/// <summary>
/// Configuration for throttling real-time validation
/// </summary>
public class ThrottlingConfig
{
    /// <summary>
    /// Delay for real-time validation during typing (default: 100ms)
    /// </summary>
    public int TypingDelayMs { get; set; } = 100;

    /// <summary>
    /// Delay for validation after paste operation (default: 50ms)
    /// </summary>
    public int PasteDelayMs { get; set; } = 50;

    /// <summary>
    /// Delay for batch validation of all rows (default: 200ms)
    /// </summary>
    public int BatchValidationDelayMs { get; set; } = 200;

    /// <summary>
    /// Maximum number of concurrent validations (default: 5)
    /// </summary>
    public int MaxConcurrentValidations { get; set; } = 5;

    /// <summary>
    /// Whether throttling is enabled (default: true)
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Delay for complex validations (DB calls, API calls) (default: 300ms)
    /// </summary>
    public int ComplexValidationDelayMs { get; set; } = 300;

    /// <summary>
    /// Default configuration with 100ms delay
    /// </summary>
    public static ThrottlingConfig Default => new();

    /// <summary>
    /// Fast configuration for simple validations
    /// </summary>
    public static ThrottlingConfig Fast => new()
    {
        TypingDelayMs = 50,
        PasteDelayMs = 25,
        BatchValidationDelayMs = 100,
        ComplexValidationDelayMs = 150
    };

    /// <summary>
    /// Slow configuration for complex validations or slow systems
    /// </summary>
    public static ThrottlingConfig Slow => new()
    {
        TypingDelayMs = 250,
        PasteDelayMs = 100,
        BatchValidationDelayMs = 500,
        ComplexValidationDelayMs = 750
    };

    /// <summary>
    /// Disabled throttling - immediate validation
    /// </summary>
    public static ThrottlingConfig Disabled => new()
    {
        IsEnabled = false,
        TypingDelayMs = 0,
        PasteDelayMs = 0,
        BatchValidationDelayMs = 0,
        ComplexValidationDelayMs = 0
    };

    /// <summary>
    /// Custom configuration with specified typing delay
    /// </summary>
    public static ThrottlingConfig Custom(int typingDelayMs)
    {
        return new ThrottlingConfig
        {
            TypingDelayMs = typingDelayMs,
            PasteDelayMs = Math.Max(10, typingDelayMs / 2),
            BatchValidationDelayMs = typingDelayMs * 2,
            ComplexValidationDelayMs = typingDelayMs * 3
        };
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (TypingDelayMs < 0)
            throw new ArgumentException("TypingDelayMs must be >= 0");

        if (PasteDelayMs < 0)
            throw new ArgumentException("PasteDelayMs must be >= 0");

        if (BatchValidationDelayMs < 0)
            throw new ArgumentException("BatchValidationDelayMs must be >= 0");

        if (MaxConcurrentValidations < 1)
            throw new ArgumentException("MaxConcurrentValidations must be >= 1");
    }
}