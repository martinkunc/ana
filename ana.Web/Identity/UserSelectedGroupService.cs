public class UserSelectedGroupService
{
    public event Action? OnChange;

    public void RaiseChange()
    {
        Console.WriteLine($"OnChange is {OnChange.Method}");
        OnChange?.Invoke();
    }
}