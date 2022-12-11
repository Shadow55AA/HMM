namespace HMM.Shared.ComponentCustomData
{
    public interface IPixelDisplayData
    {
        int SizeX { get; set; }
        int SizeZ { get; set; }
        byte[] memdata { get; set; }
    }
}
