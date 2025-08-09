// Provider for change of Display name
public class UserDisplayNameService
{
    public event Action? OnChange;
    private string _displayName = "";

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName != value)
            {
                _displayName = value;
                OnChange?.Invoke();
            }
        }
    }
}