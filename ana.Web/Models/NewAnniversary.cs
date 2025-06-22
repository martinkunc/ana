using System.ComponentModel.DataAnnotations;

public class newAnniversary
{
    public string Id { get; set; }
    public string GroupId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [RegularExpression(@"^(0?[1-9]|[12][0-9]|3[01])/(0?[1-9]|1[0-2])$", ErrorMessage = "Date must be in day/month format (e.g., 15/3 or 01/12)")]
    public string Date { get; set; }

    public newAnniversary()
    {
        Id = string.Empty;
        GroupId = Guid.NewGuid().ToString();
        Name = string.Empty;
        Date = string.Empty;
    }
}