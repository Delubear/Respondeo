using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;

namespace Respondeo.Content.Inquiry;

/// <summary>
/// Maps the internal <see cref="InquiryNodeDocument"/> domain model onto the public
/// <see cref="InquiryNode"/> contract DTO at the service boundary, so consumers never
/// depend on the parsing/domain types.
/// </summary>
internal static class InquiryNodeMapper
{
    public static InquiryNode ToContract(this InquiryNodeDocument document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        Summary = document.Summary,
        BodyHtml = document.BodyHtml,
        Tags = document.Tags,
        Branches = [.. document.Branches.Select(b => new BranchLink { To = b.To, Label = b.Label, Prompt = b.Prompt })],
        Sections = document.Sections,
        Stage = document.Stage,
        NextStage = document.NextStage is null
            ? null
            : new StageLink { Href = document.NextStage.Href, Label = document.NextStage.Label, Prompt = document.NextStage.Prompt, Icon = document.NextStage.Icon },
    };
}
