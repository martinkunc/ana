public class AnaUser
{
    public string Id { get; set; }
    public string SelectedGroupId { get; set; }

    public AnaUser()
    {
        Id = Guid.NewGuid().ToString();
        SelectedGroupId = string.Empty;
    }
}