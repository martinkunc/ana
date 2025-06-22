public class AnaRole
{
    public string Id { get; set; }
    public string Name { get; set; }
 
    public AnaRole()
    {
        Id = string.Empty;
        Name = string.Empty;
    }

    public AnaRole(string id, string name, string description)
    {
        Id = id;
        Name = name;
    }
}