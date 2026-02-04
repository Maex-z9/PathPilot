namespace PathPilot.Core.Parsers;

/// <summary>
/// Decodes PoE passive skill tree URLs to extract allocated node IDs.
/// Based on PathOfBuilding PassiveSpec.lua DecodeURL function.
/// </summary>
public static class TreeUrlDecoder
{
    /// <summary>
    /// Extracts allocated node IDs from a passive skill tree URL.
    /// </summary>
    /// <param name="treeUrl">The tree URL (pathofexile.com or pobb.in format)</param>
    /// <returns>List of allocated node IDs</returns>
    public static List<int> DecodeAllocatedNodes(string treeUrl)
    {
        if (string.IsNullOrWhiteSpace(treeUrl))
            return new List<int>();

        try
        {
            // Extract base64 portion after last /
            var base64Part = treeUrl.Split('/').Last();

            // Handle URL-safe base64
            base64Part = base64Part.Replace("-", "+").Replace("_", "/");

            // Add padding if needed
            int padding = base64Part.Length % 4;
            if (padding > 0)
                base64Part += new string('=', 4 - padding);

            var bytes = Convert.FromBase64String(base64Part);

            if (bytes.Length < 6)
            {
                Console.WriteLine($"Tree URL too short: {bytes.Length} bytes");
                return new List<int>();
            }

            // Version (4 bytes, big endian)
            var version = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];

            if (version > 6)
            {
                Console.WriteLine($"Unsupported tree version: {version}");
                return new List<int>();
            }

            // Class ID (byte 4)
            var classId = bytes[4];

            // Ascendancy ID (byte 5, for version >= 4)
            var ascendancyId = version >= 4 ? bytes[5] : 0;

            // Determine where nodes start and how many there are
            // Note: PoB uses 1-indexed Lua, so b:byte(7) = bytes[6] in C#
            int nodesStart;
            int nodeCount;

            if (version >= 5)
            {
                // Version 5+: Lua byte 7 (C# index 6) is node count, nodes start at Lua byte 8 (C# index 7)
                nodesStart = 7;
                nodeCount = bytes.Length >= 7 ? bytes[6] : 0;
            }
            else if (version >= 4)
            {
                // Version 4: Lua byte 7 (C# index 6) is node count, nodes start at Lua byte 8 (C# index 7)
                nodesStart = 7;
                nodeCount = bytes.Length >= 7 ? bytes[6] : 0;
            }
            else
            {
                // Older versions: all remaining bytes are nodes
                nodesStart = 6;
                nodeCount = (bytes.Length - 6) / 2;
            }

            // Decode nodes (2 bytes per node ID, big endian)
            var nodes = new List<int>();
            for (int i = 0; i < nodeCount && (nodesStart + i * 2 + 1) < bytes.Length; i++)
            {
                var offset = nodesStart + i * 2;
                var nodeId = (bytes[offset] << 8) | bytes[offset + 1];
                if (nodeId > 0)
                    nodes.Add(nodeId);
            }

            Console.WriteLine($"Decoded tree URL: version={version}, class={classId}, ascendancy={ascendancyId}, nodes={nodes.Count}");
            return nodes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to decode tree URL: {ex.Message}");
            return new List<int>();
        }
    }
}
