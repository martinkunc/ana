using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public sealed class PersonalDataConverter : ValueConverter<string, string>
{
    public PersonalDataConverter(IPersonalDataProtector protector) : base(s => protector.Protect(s), s => protector.Unprotect(s), default)
    { }
}