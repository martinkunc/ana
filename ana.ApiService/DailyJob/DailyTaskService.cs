using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

public class DailyTaskService : BackgroundService
{
    int hourToStart = 6; // 6 AM
    private readonly ILogger<DailyTaskService> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public DailyTaskService(ILogger<DailyTaskService> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Calculate initial delay until next 6AM
        var now = DateTime.Now;
        var nextHourToStart = now.Date.AddHours(hourToStart);
        if (now > nextHourToStart)
            nextHourToStart = nextHourToStart.AddDays(1);

        var initialDelay = nextHourToStart - now;
        await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await DoDailyWork();

            // Wait for 24 hours until next run
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    public async Task RunNowAsync()
    {
        await DoDailyWork();
    }

    private async Task DoDailyWork()
    {
        _logger.LogInformation($"Daily task running at {DateTime.Now}");



        var tomorrow = DateTime.Now.AddDays(1);
        var formattedDate = FormatDate(tomorrow);
        _logger.LogInformation($"Formatted date for stored procedure: {formattedDate}");

        var _applicationDbContext = _dbContextFactory.CreateDbContext();
        var annivs = await _applicationDbContext
            .AnaAnnivs
            .Where(a => a.Date == formattedDate).ToListAsync();

        var anniversariesTomorrow = string.Join(", ", annivs.Select(a => $"On {a.Date}: {a.Name} from GroupId {a.GroupId}"));

        Dictionary<string, List<string>> groupNotifications = new Dictionary<string, List<string>>();
        foreach (var anniv in annivs)
        {
            _logger.LogInformation($"Anniversary: {anniv.Name} on {anniv.Date} from GroupId {anniv.GroupId}");
            if (groupNotifications.ContainsKey(anniv.GroupId))
            {
                groupNotifications[anniv.GroupId].Add(anniv.Name);
            }
            else
            {
                groupNotifications[anniv.GroupId] = new List<string> { anniv.Name };
            }
        }



        _logger.LogInformation($"Anniversaries for tomorrow ({formattedDate}): {anniversariesTomorrow}");
        var distinctGroupIds = annivs.Select(a => a.GroupId).Distinct().ToList();

        var membersToGetNotified = await _applicationDbContext
            .AnaGroupToUsers
            .Where(agu => distinctGroupIds.Contains(agu.GroupId))
            .Select(agu => new { agu.UserId, agu.GroupId })
            .Distinct()
            .ToListAsync();
        // Your logic here
        


    }

    private string FormatDate(DateTime inputDate)
    {
        return inputDate.Day + "/" + inputDate.Month;
    }
}