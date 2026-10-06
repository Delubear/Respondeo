namespace Respondeo.Components;

/// <summary>
/// Context handed to the dialog body and footer of a <see cref="FloatingGuide"/> so content can close the dialog (for example a footer button or an in-body link that navigates away).
/// </summary>
/// <param name="CloseAsync">Closes the floating dialog.</param>
public sealed record FloatingGuideContext(Func<Task> CloseAsync);
