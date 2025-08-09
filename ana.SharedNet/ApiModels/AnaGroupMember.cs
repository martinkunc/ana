using ana.SharedNet;

public class AnaGroupMember
{
    public string UserId { get; set; }
    public string GroupId { get; set; }
    public string Role { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public AnaGroupMember()
    {
        UserId = string.Empty;
        GroupId = string.Empty;
        Email = string.Empty;
        Role = PreferredNotifications.None;
        DisplayName = string.Empty;
    }
}