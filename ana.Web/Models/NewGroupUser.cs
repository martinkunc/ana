using System.ComponentModel.DataAnnotations;

public class NewGroupUser
{
    public string GroupId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }


    public NewGroupUser()
    {
        GroupId = string.Empty;
        Email = string.Empty;
    }
}