public class XuiC_RadioInfo : XUiController
{
    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set
        {
            if (value == _text)
                return;
            
            _text = value;
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
        if (bindingName != "text")
            return base.GetBindingValue(ref value, bindingName);

        value = _text;
        return true;
    }
}