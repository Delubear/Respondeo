namespace Respondeo.Components;

/// <summary>
/// The mutually-exclusive corner badge a <see cref="Card"/> can advertise. A card shows at most one
/// of these because the checkmark and the ribbon occupy the same top-right corner. The feast glow is
/// a separate, full-card treatment (<see cref="Card.Feast"/>), so it is not part of this enum and may
/// appear alongside any status.
/// </summary>
public enum CardStatus
{
    /// <summary>No corner badge — an ordinary card.</summary>
    None = 0,

    /// <summary>Already-read content: a faded card with a small checkmark badge.</summary>
    Visited = 1,

    /// <summary>Unvetted / draft content: an angled "In progress" ribbon in the top-right corner.</summary>
    InProgress = 2,
}
