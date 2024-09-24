namespace Wasteland_Waves.Source.NetPackages;

public class NetPackageVehicleRadioRequestMute : NetPackage
{
    private int _vehicleEntityId;
    private bool _isMuted;

    public NetPackageVehicleRadioRequestMute Setup(int vehicleEntityId, bool isMuted)
    {
        _vehicleEntityId = vehicleEntityId;
        _isMuted = isMuted;
        
        return this;
    }
    
    public override void read(PooledBinaryReader reader)
    {
        _vehicleEntityId = reader.ReadInt32();
        _isMuted = reader.ReadBoolean();
    }

    public override void write(PooledBinaryWriter writer)
    {
        base.write(writer);
        
        writer.Write(_vehicleEntityId);
        writer.Write(_isMuted);
    }

    public override void ProcessPackage(World world, GameManager gameManager)
    {
        var rm = SingletonMonoBehaviour<RadioManager>.Instance;
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;

        if (world.GetEntity(_vehicleEntityId) is not EntityVehicle vehicleEntity)
            return;

        var vehicleRadio = vehicleEntity.GetComponent<VehicleRadioComponent>();

        //update locally
        vehicleRadio?.SetMute(true);
        
        //send to other clients
        var package = NetPackageManager.GetPackage<NetPackageVehicleRadioSetMute>()
            .Setup(_vehicleEntityId, _isMuted);
        
        cm.SendPackage(package);
    }

    public override int GetLength()
    {
        return 0;
    }
}