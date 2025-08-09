using ana.Web.Pages;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

// Service is started by Azure function throught Api call It runs daily at 6 AM GMT, select anniversaries for upcoming day and sends them
public class DailyTaskService
{
    int hourToStart = 6; // 6 AM
    private string _secretFromEmail;
    private string _secretSendGridKey;
    private string _secretTwilioAccountSID;
    private string _secretTwilioAccountToken;
    private string _secretWhatsAppFrom;
    private readonly ILogger<DailyTaskService> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public DailyTaskService(ILogger<DailyTaskService> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
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
        var humanReadableDate = formattedDate;
        _logger.LogInformation($"Formatted date: {formattedDate}");

        var _applicationDbContext = _dbContextFactory.CreateDbContext();
        var annivs = await _applicationDbContext
            .AnaAnnivs
            .Where(a => a.Date == formattedDate).ToListAsync();

        var anniversariesTomorrow = string.Join(", ", annivs.Select(a => $"On {a.Date}: {a.Name} from GroupId {a.GroupId}"));

        Dictionary<string, List<string>> groupNotifications = [];
        foreach (var anniv in annivs)
        {
            _logger.LogInformation($"Anniversary: {anniv.Name} on {anniv.Date} from GroupId {anniv.GroupId}");
            if (groupNotifications.ContainsKey(anniv.GroupId))
            {
                groupNotifications[anniv.GroupId].Add(anniv.Name);
            }
            else
            {
                groupNotifications[anniv.GroupId] = [anniv.Name];
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

        var notifiedMembers = membersToGetNotified.Select(m => m.UserId).Distinct().ToList();
        var notificationTypes = new List<string> { NotificationType.Email.ToString(), NotificationType.WhatsApp.ToString() };
        var notifiedUsers = await _applicationDbContext.AnaUsers
            .Where(u => notifiedMembers.Contains(u.Id) && notificationTypes.Contains(u.PreferredNotification))
            .Select(u => new { u.Id, u.PreferredNotification, u.WhatsAppNumber }).ToListAsync();

        var notifiedUsersDict = notifiedUsers.ToDictionary(u => u.Id, u => u);

        var notifiedUsersWithEmail = await _applicationDbContext.Users.Where(u => notifiedMembers.Contains(u.Id)).Select(u => new { u.Id, u.Email }).ToListAsync();

        var notifiedUsersWithEmailAndMessages = notifiedUsersWithEmail.Select(u => new
        {
            u.Id,
            u.Email,
            PreferredNotification = notifiedUsersDict.ContainsKey(u.Id) ? notifiedUsersDict[u.Id].PreferredNotification : NotificationType.None.ToString(),
            WhatsAppNumber = notifiedUsersDict.ContainsKey(u.Id) ? notifiedUsersDict[u.Id].WhatsAppNumber : string.Empty,
            Messages = usersToMessages.ContainsKey(u.Id) ? usersToMessages[u.Id] : []
        }
        ).ToList();

        foreach (var nu in notifiedUsersWithEmailAndMessages)
        {

            if (nu.PreferredNotification == NotificationType.Email.ToString() && !string.IsNullOrEmpty(nu.Email))
            {
                var formattedMessages = "On " + formattedDate + " there are following anniversaries " + string.Join("<br/>", nu.Messages);
                await SendMail(nu.Email, humanReadableDate, formattedMessages);
            }
            else if (nu.PreferredNotification == NotificationType.WhatsApp.ToString() && !string.IsNullOrEmpty(nu.WhatsAppNumber))
            {
                var anniversaries = string.Join(" ", nu.Messages);
                var fm = $"The upcoming anniversaries on {humanReadableDate} are the following anniversaries: {string.Join(" ", nu.Messages)}";
                await SendWhatsAppMessage(nu.WhatsAppNumber, humanReadableDate, anniversaries);
            }
        }
    }

    private async Task SendWhatsAppMessage(string whatsAppNumber, string humanReadableDate, string anniversaries)
    {
        _logger.LogInformation($"Sending WhatsApp message to {whatsAppNumber}: {anniversaries}");
        // Send using twilio
        TwilioClient.Init(_secretTwilioAccountSID, _secretTwilioAccountToken);

        var message = await MessageResource.CreateAsync(
            from: new Twilio.Types.PhoneNumber(_secretWhatsAppFrom),
            to: new Twilio.Types.PhoneNumber("whatsapp:" + whatsAppNumber),
            contentSid: "HXd4d1fc17f54fa1e0d76951629323a8db",
            contentVariables: JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["1"] = humanReadableDate,
                ["2"] = anniversaries,
            }, new JsonSerializerOptions
            {
                WriteIndented = true
            }),
            messagingServiceSid: "MG51f74f879a04bb15f550cc83d54b6b72"
        );

        _logger.LogInformation($"Body: {message.Body}");
        _logger.LogInformation($"message.Status: {message.Status}");
    }

    private async Task SendMail(string email, string humanReadableDate, string formattedMessages)
    {
        _logger.LogInformation($"Sending Email message to {email}: {formattedMessages}");
        var subject = $"Upcoming anniversaries on {humanReadableDate}";
        _logger.LogInformation($"Sending email notification from: {_secretFromEmail}");
        var client = new SendGridClient(_secretSendGridKey);
        var from = new EmailAddress(_secretFromEmail, "Anniversary Notification Application");
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, formattedMessages, formattedMessages);
        var response = await client.SendEmailAsync(msg);
        _logger.LogInformation($"Email to {email} sent with status code: {response.ToString} {JsonContent.Create(response)}");
    }

    private string FormatDate(DateTime inputDate)
    {
        return inputDate.Day + "/" + inputDate.Month;
    }

    internal void SetSecrets(string secretFromEmail,
        string secretSendGridKey,
        string secretTwilioAccountSID,
        string secretTwilioAccountToken,
        string secretWhatsAppFrom)
    {
        _secretFromEmail = secretFromEmail;
        _secretSendGridKey = secretSendGridKey;
        _secretTwilioAccountSID = secretTwilioAccountSID;
        _secretTwilioAccountToken = secretTwilioAccountToken;
        _secretWhatsAppFrom = secretWhatsAppFrom;
    }
}