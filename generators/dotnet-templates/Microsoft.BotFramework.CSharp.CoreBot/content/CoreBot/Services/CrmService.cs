using CoreBot.Models;
using Npgsql;
using System.Threading.Tasks;

namespace CoreBot.Services;

/// <summary>
/// Service for interacting with a PostgreSQL based CRM system. 
// This service handles saving `ProspectProfile` data to a PostgreSQL database. 
// It’s a basic implementation. Extend with more features (e.g., updates, retrieval).
// Adapt to your own CRM system such as HubSpot or SalesForce.
/// </summary>
public class CrmService
{
    private readonly string _connectionString;

    public CrmService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveProspectProfileAsync(ProspectProfile profile)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            @"INSERT INTO ProspectProfiles (
                Id, Name, Role, Company, DirectReports, TeamSize, BusinessPerformanceRating, 
                KeyChallenges, Objectives, ProgressTowardObjectives, MissingSkillsOrResources, 
                ConfidenceLevel, HasBudget, DecisionMakers, DesiredOutcome, Timeline, 
                IsQualified, IsFollowUpBooked, PhoneNumber, PreferredCallTime, LastUpdated, ConversationSummary
            ) VALUES (
                @Id, @Name, @Role, @Company, @DirectReports, @TeamSize, @BusinessPerformanceRating, 
                @KeyChallenges, @Objectives, @ProgressTowardObjectives, @MissingSkillsOrResources, 
                @ConfidenceLevel, @HasBudget, @DecisionMakers, @DesiredOutcome, @Timeline, 
                @IsQualified, @IsFollowUpBooked, @PhoneNumber, @PreferredCallTime, @LastUpdated, @ConversationSummary
            ) ON CONFLICT (Id) DO UPDATE SET
            Name = @Name, Role = @Role, Company = @Company, DirectReports = @DirectReports, 
            TeamSize = @TeamSize, BusinessPerformanceRating = @BusinessPerformanceRating, 
            KeyChallenges = @KeyChallenges, Objectives = @Objectives, 
            ProgressTowardObjectives = @ProgressTowardObjectives, MissingSkillsOrResources = @MissingSkillsOrResources, 
            ConfidenceLevel = @ConfidenceLevel, HasBudget = @HasBudget, DecisionMakers = @DecisionMakers, 
            DesiredOutcome = @DesiredOutcome, Timeline = @Timeline, IsQualified = @IsQualified, 
            IsFollowUpBooked = @IsFollowUpBooked, PhoneNumber = @PhoneNumber, PreferredCallTime = @PreferredCallTime, 
            LastUpdated = @LastUpdated, ConversationSummary = @ConversationSummary", conn);

        cmd.Parameters.AddWithValue("Id", profile.Id);
        cmd.Parameters.AddWithValue("Name", profile.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("Role", profile.Role ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("Company", profile.Company ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("DirectReports", profile.DirectReports);
        cmd.Parameters.AddWithValue("TeamSize", profile.TeamSize);
        cmd.Parameters.AddWithValue("BusinessPerformanceRating", profile.BusinessPerformanceRating);
        cmd.Parameters.AddWithValue("KeyChallenges", string.Join(",", profile.KeyChallenges));
        cmd.Parameters.AddWithValue("Objectives", string.Join(",", profile.Objectives));
        cmd.Parameters.AddWithValue("ProgressTowardObjectives", profile.ProgressTowardObjectives ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("MissingSkillsOrResources", string.Join(",", profile.MissingSkillsOrResources));
        cmd.Parameters.AddWithValue("ConfidenceLevel", profile.ConfidenceLevel);
        cmd.Parameters.AddWithValue("HasBudget", profile.HasBudget);
        cmd.Parameters.AddWithValue("DecisionMakers", string.Join(",", profile.DecisionMakers));
        cmd.Parameters.AddWithValue("DesiredOutcome", profile.DesiredOutcome ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("Timeline", profile.Timeline ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("IsQualified", profile.IsQualified);
        cmd.Parameters.AddWithValue("IsFollowUpBooked", profile.IsFollowUpBooked);
        cmd.Parameters.AddWithValue("PhoneNumber", profile.PhoneNumber ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("PreferredCallTime", profile.PreferredCallTime ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("LastUpdated", profile.LastUpdated);
        cmd.Parameters.AddWithValue("ConversationSummary", profile.ConversationSummary ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }
}
