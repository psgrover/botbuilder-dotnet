using System;
using System.Collections.Generic;

namespace CoreBot.Models;

/// <summary>
/// Represents a prospect profile.
/// </summary>
public class ProspectProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Role { get; set; }
    public string Company { get; set; }
    public int DirectReports { get; set; }
    public int TeamSize { get; set; }
    public int BusinessPerformanceRating { get; set; }
    public List<string> KeyChallenges { get; set; } = new List<string>();
    public List<string> Objectives { get; set; } = new List<string>();
    public string ProgressTowardObjectives { get; set; }
    public List<string> MissingSkillsOrResources { get; set; } = new List<string>();
    public int ConfidenceLevel { get; set; }
    public bool HasBudget { get; set; }
    public List<string> DecisionMakers { get; set; } = new List<string>();
    public string DesiredOutcome { get; set; }
    public string Timeline { get; set; }
    public bool IsQualified { get; set; }
    public bool IsFollowUpBooked { get; set; }
    public string PhoneNumber { get; set; }
    public string PreferredCallTime { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string ConversationSummary { get; set; }
}
