namespace PathPilot.Core.Parsers;

/// <summary>
/// Result of decoding a PoE passive skill tree URL.
/// </summary>
public class TreeDecodeResult
{
    public List<int> AllocatedNodes { get; set; } = new();
    /// <summary>
    /// Mastery selections: nodeId → effectId
    /// </summary>
    public Dictionary<int, int> MasterySelections { get; set; } = new();
}

/// <summary>
/// Decodes PoE passive skill tree URLs to extract allocated node IDs and mastery selections.
/// Based on PathOfBuilding PassiveSpec.lua DecodeURL function.
/// </summary>
public static class TreeUrlDecoder
{
    /// <summary>
    /// Extracts allocated node IDs from a passive skill tree URL.
    /// </summary>
    public static List<int> DecodeAllocatedNodes(string treeUrl)
    {
        return DecodeTreeUrl(treeUrl).AllocatedNodes;
    }

    /// <summary>
    /// Full decode: allocated nodes + mastery selections.
    /// </summary>
    public static TreeDecodeResult DecodeTreeUrl(string treeUrl)
    {
        var result = new TreeDecodeResult();

        if (string.IsNullOrWhiteSpace(treeUrl))
            return result;

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
                return result;
            }

            // Version (4 bytes, big endian)
            var version = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];

            if (version > 6)
            {
                Console.WriteLine($"Unsupported tree version: {version}");
                return result;
            }

            // Class ID (byte 4)
            var classId = bytes[4];

            // Ascendancy ID (byte 5, for version >= 4)
            var ascendancyId = version >= 4 ? bytes[5] : 0;

            // Determine where nodes start and how many there are
            // PoB Lua (1-indexed): b:byte(7) = C# bytes[6] = nodeCount
            // nodesStart = Lua 8 = C# 7
            int nodesStart;
            int nodeCount;

            if (version >= 4)
            {
                nodesStart = 7;
                nodeCount = bytes.Length >= 7 ? bytes[6] : 0;
            }
            else
            {
                nodesStart = 6;
                nodeCount = (bytes.Length - 6) / 2;
            }

            // Decode regular nodes (2 bytes per node ID, big endian)
            for (int i = 0; i < nodeCount && (nodesStart + i * 2 + 1) < bytes.Length; i++)
            {
                var offset = nodesStart + i * 2;
                var nodeId = (bytes[offset] << 8) | bytes[offset + 1];
                if (nodeId > 0)
                    result.AllocatedNodes.Add(nodeId);
            }

            Console.WriteLine($"Decoded tree URL: version={version}, class={classId}, ascendancy={ascendancyId}, nodes={result.AllocatedNodes.Count}");

            // Version 5+: cluster jewel nodes after regular nodes
            if (version >= 5)
            {
                int clusterCountIndex = nodesStart + nodeCount * 2;
                if (clusterCountIndex < bytes.Length)
                {
                    int clusterCount = bytes[clusterCountIndex];
                    int clusterStart = clusterCountIndex + 1;

                    for (int i = 0; i < clusterCount && (clusterStart + i * 2 + 1) < bytes.Length; i++)
                    {
                        var offset = clusterStart + i * 2;
                        var nodeId = (bytes[offset] << 8) | bytes[offset + 1];
                        if (nodeId > 0)
                            result.AllocatedNodes.Add(nodeId);
                    }

                    Console.WriteLine($"Decoded {clusterCount} cluster nodes");

                    // Version 6+: mastery selections after cluster nodes
                    if (version >= 6)
                    {
                        int masteryCountIndex = clusterStart + clusterCount * 2;
                        if (masteryCountIndex < bytes.Length)
                        {
                            int masteryCount = bytes[masteryCountIndex];
                            int masteryDataStart = masteryCountIndex + 1;

                            // Each mastery: effectId (2 bytes) + nodeId (2 bytes) = 4 bytes
                            for (int i = 0; i < masteryCount && (masteryDataStart + i * 4 + 3) < bytes.Length; i++)
                            {
                                var offset = masteryDataStart + i * 4;
                                var effectId = (bytes[offset] << 8) | bytes[offset + 1];
                                var masteryNodeId = (bytes[offset + 2] << 8) | bytes[offset + 3];

                                if (masteryNodeId > 0 && effectId > 0)
                                    result.MasterySelections[masteryNodeId] = effectId;
                            }

                            Console.WriteLine($"Decoded {result.MasterySelections.Count} mastery selections");
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to decode tree URL: {ex.Message}");
            return result;
        }
    }
}
