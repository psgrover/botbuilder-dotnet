using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CoreBot.Services;

/// <summary>
/// Service for scheduling meetings, currently using a static Calendly link with optional API integration.
/// </summary>
public class SchedulingService : ISchedulingService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly string _calendlyApiKey;
    private readonly string _eventTypeUuid;
    private readonly bool _useApi;

    public SchedulingService(IConfiguration configuration, HttpClient httpClient = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? new HttpClient(); // Allow injection for testing
        _calendlyApiKey = configuration["CalendlyApiKey"];
        _eventTypeUuid = configuration["CalendlyEventTypeUuid"];
        _useApi = !string.IsNullOrEmpty(_calendlyApiKey) && !string.IsNullOrEmpty(_eventTypeUuid);

        if (_useApi)
        {
            _httpClient.BaseAddress = new Uri("https://api.calendly.com/");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _calendlyApiKey);
        }
    }

    /// <summary>
    /// Schedules a meeting for the prospect and decision-makers, returning a booking URL.
    /// Uses Calendly API if configured, otherwise falls back to a static link.
    /// </summary>
    /// <param name="prospectName">Name of the prospect.</param>
    /// <param name="decisionMakers">List of decision-makers’ names or emails.</param>
    /// <returns>A URL for the scheduled meeting.</returns>
    public async Task<string> ScheduleMeetingAsync(string prospectName, List<string> decisionMakers)
    {
        if (!_useApi)
        {
            // Fallback to static link from configuration
            return _configuration["FollowUpMeetingLink"] ?? "https://calendly.com/caretechpros/gameplan";
        }

        try
        {
            // Calendly API: Create a scheduling link
            var payload = new
            {
                max_event_count = 1,
                owner = $"https://api.calendly.com/event_types/{_eventTypeUuid}",
                owner_type = "EventType"
            };

            var response = await _httpClient.PostAsJsonAsync("scheduling_links", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CalendlyResponse>();
            return result?.Resource?.BookingUrl ?? throw new InvalidOperationException("Booking URL not found in response.");
        }
        catch (Exception ex)
        {
            // Log error in production; fallback to static link for now
            Console.WriteLine($"Scheduling error: {ex.Message}");
            return _configuration["FollowUpMeetingLink"] ?? "https://calendly.com/caretechpros/gameplan";
        }
    }

    private class CalendlyResponse
    {
        public CalendlyResource Resource { get; set; }
    }

    private class CalendlyResource
    {
        public string BookingUrl { get; set; }
    }
}
