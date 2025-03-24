using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreBot.Services;

public interface ISchedulingService
{
    Task<string> ScheduleMeetingAsync(string prospectName, List<string> decisionMakers);
}
