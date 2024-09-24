public class XUiC_RadioInfo : XUiController
{
    private string _radioStationText = string.Empty;
    private string _currentSongText = string.Empty;
    private bool _isVisible = false;
    
    public string RadioStationText
    {
        get => _radioStationText;
        set
        {
            if (value == _radioStationText)
                return;
            
            _radioStationText = value;
            IsDirty = true;
        }
    }

    public string CurrentSongText
    {
        get => _currentSongText;
        set
        {
            if (value == _currentSongText)
                return;
            
            _currentSongText = value;
            IsDirty = true;
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (value == _isVisible)
                return;
            
            _isVisible = value;
            IsDirty = true;
        }
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (!IsDirty)
            return;

        RefreshBindings(true);
        IsDirty = false;
    }

    public override bool GetBindingValue(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "radioStation":
                value = RadioStationText;
                return true;
            case "currentSong":
                value = CurrentSongText;
                return true;
            case "isVisible":
                value = _isVisible? "true" : "false";
                return true;
            default:
                return base.GetBindingValue(ref value, bindingName);
        }
    }
}