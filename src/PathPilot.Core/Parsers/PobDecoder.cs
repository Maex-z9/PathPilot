using System.IO.Compression;
using System.Text;
using System.Linq;

namespace PathPilot.Core.Parsers;

/// <summary>
/// Handles decoding of Path of Building paste codes
/// PoB uses Base64 encoding + Deflate compression
/// </summary>
public static class PobDecoder
{
    /// <summary>
    /// Decodes a PoB paste code to XML string
    /// </summary>
    /// <param name="pasteCode">The base64-encoded PoB string</param>
    /// <returns>Decompressed XML string</returns>
    public static string DecodeToXml(string pasteCode)
{
    if (string.IsNullOrWhiteSpace(pasteCode))
    {
        throw new ArgumentException("Paste code cannot be empty", nameof(pasteCode));
    }

    try
    {
        // Remove ALL whitespace characters (newlines, spaces, tabs)
        pasteCode = new string(pasteCode.Where(c => !char.IsWhiteSpace(c)).ToArray());

        // Convert from URL-safe Base64 to standard Base64
        pasteCode = pasteCode.Replace('-', '+').Replace('_', '/');
        
        // Add padding if needed
        int padding = pasteCode.Length % 4;
        if (padding > 0)
        {
            pasteCode += new string('=', 4 - padding);
        }

        // Decode from Base64
        byte[] compressedBytes = Convert.FromBase64String(pasteCode);

        // Skip ZLIB header (first 2 bytes)
        // PoB uses ZLIB compression, but DeflateStream expects raw deflate data
        using var compressedStream = new MemoryStream(compressedBytes, 2, compressedBytes.Length - 2);
        using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        
        deflateStream.CopyTo(resultStream);
        
        // Convert to string
        byte[] decompressedBytes = resultStream.ToArray();
        string xml = Encoding.UTF8.GetString(decompressedBytes);
        
        return xml;
    }
    catch (FormatException ex)
    {
        throw new InvalidOperationException("Invalid PoB paste code format. Must be valid Base64.", ex);
    }
    catch (InvalidDataException ex)
    {
        throw new InvalidOperationException("Failed to decompress PoB data. Data may be corrupted.", ex);
    }
}
    /// <summary>
    /// Encodes XML string back to PoB paste code format
    /// </summary>
    /// <param name="xml">The XML string to encode</param>
    /// <returns>Base64-encoded compressed string</returns>
    public static string EncodeFromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("XML cannot be empty", nameof(xml));
        }

        try
        {
            // Convert to bytes
            byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

            // Compress using Deflate
            using var outputStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
            {
                deflateStream.Write(xmlBytes, 0, xmlBytes.Length);
            }

            byte[] compressedBytes = outputStream.ToArray();

            // Encode to Base64
            string pasteCode = Convert.ToBase64String(compressedBytes);
            
            return pasteCode;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to encode XML to PoB format.", ex);
        }
    }

    /// <summary>
    /// Validates if a string is a valid PoB paste code
    /// </summary>
    public static bool IsValidPobCode(string pasteCode)
    {
        if (string.IsNullOrWhiteSpace(pasteCode))
        {
            return false;
        }

        try
        {
            // Try to decode - if it works, it's valid
            DecodeToXml(pasteCode);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
