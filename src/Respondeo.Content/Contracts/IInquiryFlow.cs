namespace Respondeo.Content.Contracts;

/// <summary>
/// The public contract for the Inquiry journey's orchestration (flow.json): stage ordering, the
/// branch edges between nodes, and the culminating transition out of each stage. This is a separate
/// concern from <see cref="IContentService"/> (which owns node prose) and from the load manifest
/// (which owns the file list). Consumers depend only on this surface; the implementation owns loading
/// and caching of flow.json.
/// </summary>
public interface IInquiryFlow
{
    /// <summary>Returns the journey's stages in their declared order.</summary>
    Task<IReadOnlyList<InquiryStage>> GetStagesAsync();

    /// <summary>
    /// Returns the outgoing branch edges for a node, in author order. Empty when the node has none.
    /// Each <see cref="BranchLink"/> carries the target id plus optional label/prompt overrides.
    /// </summary>
    Task<IReadOnlyList<BranchLink>> GetBranchesAsync(string nodeId);

    /// <summary>
    /// Returns the stage transition that should appear at the foot of the given node, or null when the
    /// node is not the culminating node of its stage. The node is identified by id; the stage it belongs
    /// to is passed so the lookup never has to re-derive it.
    /// </summary>
    Task<StageLink?> GetTransitionForNodeAsync(string nodeId, string? stage);
}
