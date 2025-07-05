using System.ComponentModel.DataAnnotations;

public class NewGroup
{
    public string UserId { get; set; }

    [Required]
    public string Name { get; set; }


    public NewGroup()
    {
        UserId = string.Empty;
        Name = string.Empty;
    }
}