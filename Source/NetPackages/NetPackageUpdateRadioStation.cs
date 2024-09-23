namespace Wasteland_Waves.Source.NetPackages;

public class NetPackageUpdateRadioStation : NetPackage
{
    private string _stationName = string.Empty;
    private string _newCurrentSong = string.Empty;
    private string _newNextSong = string.Empty;
    private float _currentTime = 0;

    public NetPackageUpdateRadioStation Setup(string stationName, string newCurrentSong, string newNextSong, float newCurrentTime)
    {
        _stationName = stationName;
        _newCurrentSong = newCurrentSong;
        _newNextSong = newNextSong;
        _currentTime = newCurrentTime;
        
        return this;
    }

    public override void read(PooledBinaryReader reader)
    {
        _stationName = reader.ReadString();
        _newCurrentSong = reader.ReadString();
        _newNextSong = reader.ReadString();
        _currentTime = (float) reader.ReadDouble();
    }

    public override void write(PooledBinaryWriter writer)
    {
        base.write(writer);

        writer.Write(_stationName);
        writer.Write(_newCurrentSong);
        writer.Write(_newNextSong);
        writer.Write((double) _currentTime);
    }

    public override void ProcessPackage(World world, GameManager gameManager)
    {
        var isClient = SingletonMonoBehaviour<ConnectionManager>.Instance.IsClient;
        if (!isClient) return;
        
        SingletonMonoBehaviour<RadioManager>.Instance
            .UpdateRadioStationFromServer(_stationName, _newCurrentSong, _newNextSong, _currentTime);
    }

    public override int GetLength()
    {
        return 0;
    }
}