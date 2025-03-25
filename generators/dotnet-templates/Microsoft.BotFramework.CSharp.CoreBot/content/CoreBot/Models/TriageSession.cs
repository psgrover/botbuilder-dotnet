using System.Collections.Generic;

namespace CoreBot.Models;

/// <summary>
/// Stores conversation state and prospect information.
/// </summary>
public class TriageSession
{
    public string ProspectName { get; set; }
    public string TempCompany { get; set; }
    public int TempTeamSize { get; set; }
    public string TempHiringType { get; set; }
    public string TempKeyChallenges { get; set; }
    public string TempObjectives { get; set; }
    public string TempDesiredOutcome { get; set; }
    public string TempTimeline { get; set; }
    public List<string> TempDecisionMakers { get; set; } = new List<string>();
    public bool IsQualified { get; set; }
    public string CurrentSection { get; set; }
    public bool IsTimeConfirmed { get; set; }
    public bool IsSummaryConfirmed { get; set; }
}
