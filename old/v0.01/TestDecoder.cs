using System;
using PathPilot.Core.Parsers;

class Program
{
    static void Main()
    {
        string testCode = "eNpljkEKgzAQRfc9RTYuFTJq0gpdiuewl4gMTlBIYiQxUErvXqtQXM3j_Xn8mQ26A2MdeCwQEowxBh6VRGXQWBv0ykH8o9YYazyMjzU-xkdkCNYYf_MNRRkn0i4ZnXMKbU5L0R7c0r9VpXOuaKsq15ZlOhLZs8_JvUBZMqVKobKsqEpl2VBlKtN9X6YypUqRKlWmTJUqU6ZMlSpTpkqVKVOlypQpU6XKlKlSpf4BUmBKsA";
        
        Console.WriteLine($"Input length: {testCode.Length}");
        Console.WriteLine($"First 20 chars: {testCode.Substring(0, 20)}");
        
        try 
        {
            string xml = PobDecoder.DecodeToXml(testCode);
            Console.WriteLine("✓ SUCCESS!");
            Console.WriteLine($"XML length: {xml.Length}");
            Console.WriteLine($"First 100 chars: {xml.Substring(0, Math.Min(100, xml.Length))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("✗ ERROR!");
            Console.WriteLine($"Type: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }
    }
}
