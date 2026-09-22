namespace GoodMorningGFN.Models;

/// <summary>
/// Zustand mit Prozess-Lifetime (nicht pro Tag zurückgesetzt) — bewusst getrennt von
/// <see cref="TagesStatus"/>, damit ein Tageswechsel-Reset diese Felder nicht versehentlich mitreißt.
/// </summary>
public sealed class SessionStatus
{
    public bool FirstRun { get; set; } = true;
    public bool? LastLoginState { get; set; }
}
