using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class ScheduledFunction
{
    private readonly ILogger<ScheduledFunction> _logger;
    private readonly IApiClient _apiClient;

    public ScheduledFunction(ILogger<ScheduledFunction> logger, IApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    private const string ScheduleEachMinute = "0 * * * * *"; // for testing
    private const string ScheduleEachDay = "0 0 6 * * *"; // Every day at 6 AM UTC

    [Function("ScheduledFunction")]
    public async Task Run([TimerTrigger(ScheduleEachDay)] TimerInfo timer)
    {
        _logger.LogInformation("Function executed at: {time}", DateTime.UtcNow);
        _logger.LogDebug("Timer schedule: {schedule}", timer.ScheduleStatus);
        await _apiClient.RunDailyTasksAsync();
    }
}