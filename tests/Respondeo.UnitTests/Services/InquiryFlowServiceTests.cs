using Respondeo.Content.Inquiry;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="InquiryFlowService"/>, which loads flow.json and flattens it into node-keyed
/// lookups. Covers stage ordering, branch edges keyed by source node, stage transitions, case-insensitive
/// node keys, and empty results for unknown nodes.
/// </summary>
public class InquiryFlowServiceTests
{
    private const string FlowJson = """
        {
          "stages": [
            {
              "id": "which-god",
              "title": "Which God?",
              "order": 2,
              "entry": "which-god-intro",
              "edges": {
                "which-god-intro": [
                  { "to": "one-god", "label": "One God", "prompt": "Monotheism" },
                  { "to": "many-gods" }
                ]
              },
              "transition": {
                "from": "one-god",
                "to": "why-jesus-intro",
                "href": "why-jesus",
                "label": "Why Jesus?",
                "prompt": "Continue",
                "icon": "compass"
              }
            },
            {
              "id": "why-god",
              "title": "Why God?",
              "kicker": "The First Question",
              "order": 1,
              "entry": "why-god-intro",
              "edges": {
                "why-god-intro": [
                  { "to": "contingency" }
                ]
              }
            }
          ]
        }
        """;

    private const string FlowPath = "_content/Respondeo.Content/inquiry/flow.json";

    private static InquiryFlowService CreateService()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            [FlowPath] = FlowJson,
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new InquiryFlowService(http);
    }

    [Fact]
    public async Task GetStages_returns_stages_in_order()
    {
        var service = CreateService();

        var stages = await service.GetStagesAsync();

        Assert.Equal(["why-god", "which-god"], stages.Select(s => s.Id));
        Assert.Equal("The First Question", stages[0].Kicker);
        Assert.Equal("why-god-intro", stages[0].Entry);
    }

    [Fact]
    public async Task GetBranches_returns_edges_for_source_node()
    {
        var service = CreateService();

        var branches = await service.GetBranchesAsync("which-god-intro");

        Assert.Equal(2, branches.Count);
        Assert.Equal("one-god", branches[0].To);
        Assert.Equal("One God", branches[0].Label);
        Assert.Equal("Monotheism", branches[0].Prompt);
        Assert.Equal("many-gods", branches[1].To);
        Assert.Null(branches[1].Label);
    }

    [Fact]
    public async Task GetBranches_is_case_insensitive_by_node_id()
    {
        var service = CreateService();

        var branches = await service.GetBranchesAsync("WHICH-GOD-INTRO");

        Assert.Equal(2, branches.Count);
    }

    [Fact]
    public async Task GetBranches_returns_empty_for_unknown_node()
    {
        var service = CreateService();

        Assert.Empty(await service.GetBranchesAsync("nope"));
    }

    [Fact]
    public async Task GetTransition_returns_link_keyed_by_from_node()
    {
        var service = CreateService();

        var transition = await service.GetTransitionForNodeAsync("one-god", stage: null);

        Assert.NotNull(transition);
        Assert.Equal("why-jesus", transition!.Href);
        Assert.Equal("Why Jesus?", transition.Label);
        Assert.Equal("Continue", transition.Prompt);
        Assert.Equal("compass", transition.Icon);
    }

    [Fact]
    public async Task GetTransition_returns_null_for_node_without_transition()
    {
        var service = CreateService();

        Assert.Null(await service.GetTransitionForNodeAsync("why-god-intro", stage: null));
    }
}
