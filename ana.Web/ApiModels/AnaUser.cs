public class AnaUser
{
    public string Id { get; set; }
    public string SelectedGroupId { get; set; }

    public string DisplayName { get; set; }

    public string PreferredNotification { get; set; }

    public string WhatsAppNumber { get; set; }

    public AnaUser()
    {
        Id = Guid.NewGuid().ToString();
        SelectedGroupId = string.Empty;
        PreferredNotification = Config.PreferredNotifications.None.ToString();
        WhatsAppNumber = string.Empty;
    }
}