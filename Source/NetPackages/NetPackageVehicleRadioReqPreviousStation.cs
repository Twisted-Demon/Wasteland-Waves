using UniLinq;

namespace Wasteland_Waves.Source.NetPackages;

public class NetPackageVehicleRadioReqPreviousStation : NetPackage
{
    private int _vehicleEntityId;

    public NetPackageVehicleRadioReqPreviousStation Setup(int vehicleEntityId)
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
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;

        if (world.GetEntity(_vehicleEntityId) is not EntityVehicle vehicle)
            return;

        var vehicleRadio = vehicle.GetComponent<VehicleRadioComponent>();
        if (vehicleRadio == null)
            return;

        //get the next station
        var stationList = rm.GetStationNames().ToList();
        var currentStation = vehicleRadio.GetCurrentStationPlaying();
        var currentStationIndex = stationList.IndexOf(currentStation);

        var newStationIndex = currentStationIndex - 1;
        if (newStationIndex < 0)
            newStationIndex = stationList.Count - 1;

        var newStation = stationList[newStationIndex];
        var newSong = rm.GetStation(newStation).GetCurrentSongName();
        var currentTime = rm.GetStation(newStation).GetStationTime();

        //if we are a client update locally
        vehicleRadio.UpdateRadio(newStation);

        //send to other clients
        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioSendData>()
            .Setup(_vehicleEntityId, newStation, newSong, currentTime);

        cm.SendPackage(package);
    }

    public override int GetLength()
    {
        return 0;
    }
}