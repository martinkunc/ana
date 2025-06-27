using ana.Web.Pages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
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

        //

        //

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
        var distinctGroupIds = groupNotifications.Keys.ToList();

        var membersToGetNotified = await _applicationDbContext
            .AnaGroupToUsers
            .Where(agu => distinctGroupIds.Contains(agu.GroupId))
            .Select(agu => new { agu.UserId, agu.GroupId })
            .Distinct()
            .ToListAsync();
        
        var userGroups = membersToGetNotified
            .GroupBy(m => m.UserId)
            .ToDictionary(
                g => g.Key, // UserId
                g => g.Select(x => x.GroupId).ToList() // List of GroupIds for this user
            );
        
        var usersToMessages = new Dictionary<string, List<string>>();
        foreach (var group in userGroups)
        {
            var userMsgs = new List<string>();
            foreach (var userGroup in group.Value)
            {
                foreach (var n in groupNotifications[userGroup])
                {
                    userMsgs.Add(n);
                }
            }

            if (usersToMessages.ContainsKey(group.Key))
            {
                usersToMessages[group.Key].AddRange(userMsgs);
            }
            else
            {
                usersToMessages[group.Key] = userMsgs;
            }
        }

        var membersWithMessages = membersToGetNotified
            .Where(m => groupNotifications.ContainsKey(m.GroupId) && groupNotifications[m.GroupId].Any())
            .Select(m => new { m.UserId, m.GroupId, Messages = groupNotifications[m.GroupId] })
            .ToList();
        // Your logic here
        var notifiedMembers = membersToGetNotified.Select(m => m.UserId).Distinct().ToList();
        var notificationTypes = new List<string> { NotificationType.Email.ToString(), NotificationType.WhatsApp.ToString() };
        var notifiedUsers = await _applicationDbContext.AnaUsers
            .Where(u => notifiedMembers.Contains(u.Id) && notificationTypes.Contains(u.PreferredNotification))
            .Select(u => new { u.Id, u.PreferredNotification, u.WhatsAppNumber }).ToListAsync();

        var notifiedUsersDict = notifiedUsers.ToDictionary(u => u.Id, u => u);

        //var notifiedUsersWithMessages = notifiedUsers.Select(u => new { u.Id, u.PreferredNotification, u.WhatsAppNumber, Messages = usersToMessages.ContainsKey(u.Id) ? usersToMessages[u.Id] : new List<string>() }).ToList();

        var notifiedUsersWithEmail = await _applicationDbContext.Users.Where(u => notifiedMembers.Contains(u.Id)).Select(u => new { u.Id, u.Email }).ToListAsync();

        var notifiedUsersWithEmailAndMessages = notifiedUsersWithEmail.Select(u => new {
            u.Id,
            u.Email,
            PreferredNotification = notifiedUsersDict.ContainsKey(u.Id) ? notifiedUsersDict[u.Id].PreferredNotification : NotificationType.None.ToString(),
            WhatsAppNumber = notifiedUsersDict.ContainsKey(u.Id) ? notifiedUsersDict[u.Id].WhatsAppNumber : string.Empty,
            Messages = usersToMessages.ContainsKey(u.Id) ? usersToMessages[u.Id] : new List<string>() }
        ).ToList();

        foreach (var nu in notifiedUsersWithEmailAndMessages)
        {

            if (nu.PreferredNotification == NotificationType.Email.ToString() && !string.IsNullOrEmpty(nu.Email))
            {
                var formattedMessages = "On " + formattedDate + " there are following anniversaries " + string.Join("<br/>", nu.Messages);
                SendMail(nu.Email, formattedMessages);
            }
            else if (nu.PreferredNotification == NotificationType.WhatsApp.ToString() && !string.IsNullOrEmpty(nu.WhatsAppNumber))
            {
                var formattedMessages = "On " + formattedDate + " there are following anniversaries " + string.Join("\n", nu.Messages);
                SendWhatsAppMessage(nu.WhatsAppNumber, formattedMessages);
            }
        }
    }

    private void SendWhatsAppMessage(string whatsAppNumber, string formattedMessages)
    {
        Console.WriteLine($"Sending WhatsApp message to {whatsAppNumber}: {formattedMessages}");
    }

    private void SendMail(string email, string formattedMessages)
    {
        Console.WriteLine($"Sending Email message to {email}: {formattedMessages}");
    }

    private string FormatDate(DateTime inputDate)
    {
        return inputDate.Day + "/" + inputDate.Month;
    }
}