namespace Wasteland_Waves.Source.NetPackages;

public class NetPackageVehicleRadioRequestData : NetPackage
{
    private int _vehicleEntityId;

    public NetPackageVehicleRadioRequestData Setup(int vehicleEntityId)
    {
        _vehicleEntityId = vehicleEntityId;
        return this;
    }

    public override void read(PooledBinaryReader reader)
    {
        _vehicleEntityId = reader.ReadInt32();
    }

    public override void write(PooledBinaryWriter writer)
    {
        base.write(writer);

        writer.Write(_vehicleEntityId);
    }

    public override void ProcessPackage(World world, GameManager gameManager)
    {
        var rm = SingletonMonoBehaviour<RadioManager>.Instance;

        if (world.GetEntity(_vehicleEntityId) is not EntityVehicle vehicle)
            return;

        var vehicleRadio = vehicle.GetComponent<VehicleRadioComponent>();
        if (vehicleRadio is null)
            return;

        var stationToSend = vehicleRadio.GetCurrentStationPlaying();
        var currentSongToSend = rm.GetStation(stationToSend).GetCurrentSongName();
        var currentTime = rm.GetStation(stationToSend).GetStationTime();

        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioSendData>()
            .Setup(_vehicleEntityId, stationToSend, currentSongToSend, currentTime);

        Sender.SendPackage(package);
    }

    public override int GetLength()
    {
        return 0;
    }
}