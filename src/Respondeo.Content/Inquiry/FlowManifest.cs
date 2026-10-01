namespace Respondeo.Content.Inquiry;

// ---------------------------------------------------------------------------
// Serialization DTOs for the Inquiry orchestration file (flow.json).
//
// flow.json is a dedicated source of truth for the journey's *flow* — the stage
// ordering, each stage's entry node, the branch edges between nodes, and the
// culminating transition from one stage into the next. It deliberately does NOT
// carry content (prose lives in the .md body) or loading (file list lives in
// manifest.json); those remain separate concerns.
//
// Deserialized via ContentFetcher.GetFromJsonAsync, which uses JsonSerializerDefaults.Web:
// PascalCase C# properties map to camelCase JSON, case-insensitively, so no
// [JsonPropertyName] attributes are required.
// ---------------------------------------------------------------------------

/// <summary>Root of flow.json: the ordered list of stages that make up the Inquiry journey.</summary>
internal sealed class FlowManifest
{
    public List<FlowStageDto> Stages { get; set; } = [];
}

/// <summary>
/// One stage of the journey and its complete, self-contained subgraph. Branch edges never cross
/// stage boundaries, so a stage owns every edge whose source node belongs to it.
/// </summary>
internal sealed class FlowStageDto
{
    /// <summary>Stage key; must match the content sub-folder the stage's nodes live in (e.g. "why-god").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name for stage-ordering UI (e.g. "Why God?").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional short kicker shown above the stage's landing title (e.g. "The First Question").</summary>
    public string? Kicker { get; set; }

    /// <summary>Explicit 1-based position of this stage in the journey.</summary>
    public int Order { get; set; }

    /// <summary>Id of the node this stage opens on.</summary>
    public string Entry { get; set; } = string.Empty;

    /// <summary>
    /// Outgoing branch edges keyed by source node id. Each value is the ordered list of cards shown
    /// at the foot of that node. Omitted keys simply have no branches.
    /// </summary>
    public Dictionary<string, List<FlowEdgeDto>> Edges { get; set; } = [];

    /// <summary>Optional culminating transition carrying the visitor into the next stage.</summary>
    public FlowTransitionDto? Transition { get; set; }
}

/// <summary>A directed branch edge from one node to another within the same stage.</summary>
internal sealed class FlowEdgeDto
{
    /// <summary>Target node id (required).</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Optional card title; when unset the card falls back to the target node's title.</summary>
    public string? Label { get; set; }

    /// <summary>Optional transitional copy shown beneath the label on the card.</summary>
    public string? Prompt { get; set; }
}

/// <summary>The prominent move from the end of one stage to the beginning of the next.</summary>
internal sealed class FlowTransitionDto
{
    /// <summary>Id of the culminating node this transition renders beneath (the last node of the stage).</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Entry node id of the next stage; present so the flow graph can be validated end to end.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Route of the next stage's landing page (e.g. "which-god").</summary>
    public string Href { get; set; } = string.Empty;

    /// <summary>Headline shown on the transition button.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Optional short reason shown beneath the label.</summary>
    public string? Prompt { get; set; }

    /// <summary>Optional emblem name shown in the golden box (e.g. "compass"). Falls back to the cross when unset.</summary>
    public string? Icon { get; set; }
}
