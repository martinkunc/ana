public class AnaGroup
{
    public string Id { get; set; }
    public string? Name { get; set; }
    public AnaGroup()
    {
        Id = Guid.NewGuid().ToString();
    }
}