using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMM.Shared
{
    public interface IPixelDisplayData
    {
        int SizeX { get; set; }
        int SizeZ { get; set; }
        byte[] memdata { get; set; }
    }
}
