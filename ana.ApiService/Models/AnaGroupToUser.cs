public class AnaGroupToUser
{
    public string UserId { get; set; }
    public string GroupId { get; set; }
    public string RoleId { get; set; }

    public AnaGroupToUser()
    {
        UserId = string.Empty;
        GroupId = string.Empty;
    }
}