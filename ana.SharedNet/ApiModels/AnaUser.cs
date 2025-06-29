using ana.SharedNet;

public class AnaUser
{
    public string Id { get; set; }

    public string DisplayName { get; set; }
    public string SelectedGroupId { get; set; }

    public string PreferredNotification { get; set; }

    public string WhatsAppNumber { get; set; }

    public AnaUser()
    {
        Id = Guid.NewGuid().ToString();
        SelectedGroupId = string.Empty;
        PreferredNotification = PreferredNotifications.None;
        WhatsAppNumber = string.Empty;
        DisplayName = string.Empty;
    }
}