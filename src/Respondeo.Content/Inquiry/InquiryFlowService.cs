using Respondeo.Content.Contracts;
using Respondeo.Content.Shared;

namespace Respondeo.Content.Inquiry;

/// <summary>
/// Loads the Inquiry orchestration file (flow.json) and exposes the journey's flow: stage order, the
/// branch edges between nodes, and each stage's culminating transition. Runs entirely client-side: it
/// fetches the JSON once via <see cref="ContentFetcher"/> and caches a set of node-keyed lookups for
/// the app's lifetime.
///
/// Flow is kept separate from content on purpose. This service owns "where does a node lead and how do
/// stages chain", while <see cref="InquiryService"/> owns the node prose. Branch/transition hrefs are
/// left to the UI (ContentRoutes) exactly as before; this service returns the raw target ids and copy.
/// </summary>
internal sealed class InquiryFlowService(HttpClient http) : IInquiryFlow
{
    private const string FlowPath = "_content/Respondeo.Content/inquiry/flow.json";

    private readonly ContentFetcher _fetcher = new(http, ContentCachePolicy.Revalidate);
    private readonly AsyncInitCache<FlowGraph> _graph = new();

    public async Task<IReadOnlyList<InquiryStage>> GetStagesAsync()
    {
        var graph = await EnsureLoadedAsync();
        return graph.Stages;
    }

    public async Task<IReadOnlyList<BranchLink>> GetBranchesAsync(string nodeId)
    {
        var graph = await EnsureLoadedAsync();
        return graph.BranchesByNode.TryGetValue(nodeId, out var branches) ? branches : [];
    }

    public async Task<StageLink?> GetTransitionForNodeAsync(string nodeId, string? stage)
    {
        var graph = await EnsureLoadedAsync();
        return graph.TransitionByNode.TryGetValue(nodeId, out var transition) ? transition : null;
    }

    private Task<FlowGraph> EnsureLoadedAsync() => _graph.GetAsync(async () =>
    {
        var manifest = await _fetcher.GetFromJsonAsync<FlowManifest>(FlowPath) ?? new FlowManifest();

        var stages = new List<InquiryStage>();
        var branchesByNode = new Dictionary<string, IReadOnlyList<BranchLink>>(StringComparer.OrdinalIgnoreCase);
        var transitionByNode = new Dictionary<string, StageLink>(StringComparer.OrdinalIgnoreCase);

        foreach (var stage in manifest.Stages.OrderBy(s => s.Order))
        {
            stages.Add(new InquiryStage { Id = stage.Id, Title = stage.Title, Kicker = stage.Kicker, Order = stage.Order, Entry = stage.Entry });

            foreach (var (sourceId, edges) in stage.Edges)
            {
                branchesByNode[sourceId] = [.. edges.Select(e => new BranchLink { To = e.To, Label = e.Label, Prompt = e.Prompt })];
            }

            if (stage.Transition is { } t && !string.IsNullOrWhiteSpace(t.From))
            {
                transitionByNode[t.From] = new StageLink { Href = t.Href, Label = t.Label, Prompt = t.Prompt, Icon = t.Icon };
            }
        }

        return new FlowGraph(stages, branchesByNode, transitionByNode);
    });

    // The flattened, lookup-ready form of flow.json. The nested stage shape is pleasant to author but the
    // UI needs edges and transitions keyed by the node they render on, so we build those maps once on load.
    private sealed record FlowGraph(
        IReadOnlyList<InquiryStage> Stages,
        IReadOnlyDictionary<string, IReadOnlyList<BranchLink>> BranchesByNode,
        IReadOnlyDictionary<string, StageLink> TransitionByNode);
}
