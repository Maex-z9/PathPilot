namespace PathPilot.Core.Parsers;

/// <summary>
/// Exception thrown when PoB parsing fails
/// </summary>
public class PobParserException : Exception
{
    public PobParserException(string message) : base(message)
    {
    }

    public PobParserException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    /// <summary>
    /// The type of parsing error that occurred
    /// </summary>
    public PobParserErrorType ErrorType { get; set; }
}

/// <summary>
/// Types of PoB parsing errors
/// </summary>
public enum PobParserErrorType
{
    InvalidFormat,
    DecompressionFailed,
    XmlParseFailed,
    MissingRequiredData,
    NetworkError,
    Unknown
}
