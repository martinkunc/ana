public class CreateAnniversaryResponse
{
    public string GroupId { get; set; }
    public AnaAnniv Anniversary { get; set; }
    public CreateAnniversaryResponse()
    {
        GroupId = string.Empty;
        Anniversary = new AnaAnniv();
    }
}