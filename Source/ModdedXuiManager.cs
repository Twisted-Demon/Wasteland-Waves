namespace Wasteland_Waves.Source;

public class ModdedXuiManager : SingletonMonoBehaviour<ModdedXuiManager>
{
    public string currentSongToShow = string.Empty;
    public string currentStationToShow = string.Empty;
    public bool showRadioInfo = false;
    
    public override void singletonAwake()
    {
        base.singletonAwake();
        IsPersistant = true;
    }
}