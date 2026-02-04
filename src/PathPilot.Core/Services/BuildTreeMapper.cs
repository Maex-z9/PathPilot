using PathPilot.Core.Models;

namespace PathPilot.Core.Services;

/// <summary>
/// Maps build allocated nodes to parsed tree data
/// </summary>
public class BuildTreeMapper
{
    private readonly SkillTreeDataService _treeService;

    public BuildTreeMapper(SkillTreeDataService treeService)
    {
        _treeService = treeService;
    }

    /// <summary>
    /// Gets all allocated nodes as PassiveNode objects
    /// </summary>
    /// <param name="treeSet">The skill tree set from an imported build</param>
    /// <returns>List of PassiveNode with full details</returns>
    public async Task<List<PassiveNode>> GetAllocatedNodesAsync(SkillTreeSet treeSet)
    {
        var treeData = await _treeService.GetTreeDataAsync();
        if (treeData == null)
        {
            Console.WriteLine("Warning: Tree data not available, cannot map allocated nodes");
            return new List<PassiveNode>();
        }

        var allocatedNodes = new List<PassiveNode>();
        var missingCount = 0;

        foreach (var nodeId in treeSet.AllocatedNodes)
        {
            if (treeData.Nodes.TryGetValue(nodeId, out var node))
            {
                allocatedNodes.Add(node);
            }
            else
            {
                missingCount++;
                // Log first few missing IDs for debugging
                if (missingCount <= 5)
                {
                    Console.WriteLine($"Warning: Node {nodeId} not found in tree data");
                }
            }
        }

        if (missingCount > 5)
        {
            Console.WriteLine($"Warning: {missingCount} total nodes not found (possibly outdated build)");
        }

        return allocatedNodes;
    }

    /// <summary>
    /// Enriches a SkillTreeSet with node details from tree data
    /// </summary>
    /// <param name="treeSet">The skill tree set to enrich</param>
    public async Task EnrichTreeSetAsync(SkillTreeSet treeSet)
    {
        var nodes = await GetAllocatedNodesAsync(treeSet);

        // Populate keystone names
        treeSet.Keystones = nodes
            .Where(n => n.IsKeystone)
            .Select(n => n.Name)
            .Distinct()
            .ToList();

        // Populate notable names
        treeSet.Notables = nodes
            .Where(n => n.IsNotable)
            .Select(n => n.Name)
            .Distinct()
            .ToList();

        Console.WriteLine($"Enriched tree set: {treeSet.Keystones.Count} keystones, {treeSet.Notables.Count} notables");
    }

    /// <summary>
    /// Gets allocated nodes filtered by type
    /// </summary>
    public async Task<List<PassiveNode>> GetAllocatedByTypeAsync(SkillTreeSet treeSet, NodeType type)
    {
        var nodes = await GetAllocatedNodesAsync(treeSet);
        return nodes.Where(n => n.Type == type).ToList();
    }

    /// <summary>
    /// Gets allocated keystones
    /// </summary>
    public async Task<List<PassiveNode>> GetAllocatedKeystonesAsync(SkillTreeSet treeSet)
    {
        var nodes = await GetAllocatedNodesAsync(treeSet);
        return nodes.Where(n => n.IsKeystone).ToList();
    }

    /// <summary>
    /// Gets allocated notables
    /// </summary>
    public async Task<List<PassiveNode>> GetAllocatedNotablesAsync(SkillTreeSet treeSet)
    {
        var nodes = await GetAllocatedNodesAsync(treeSet);
        return nodes.Where(n => n.IsNotable).ToList();
    }

    /// <summary>
    /// Gets ascendancy nodes only
    /// </summary>
    public async Task<List<PassiveNode>> GetAllocatedAscendancyNodesAsync(SkillTreeSet treeSet)
    {
        var nodes = await GetAllocatedNodesAsync(treeSet);
        return nodes.Where(n => n.IsAscendancy).ToList();
    }
}
