using LogicAPI.Server.Components;
using LogicWorld.Server.Circuitry;
using System;

namespace HMM.Server.LogicCode
{
    public class Memory8bit : LogicComponent<Memory8bit.IData>
    {
        public interface IData
        {
            byte[] mem { get; set; }
        }

        protected override void DoLogicUpdate()
        {
            int address = 0;
            for (int i = 0; i < 16; i++)
            {
                address += Inputs[i].On ? 1 << i : 0;
            }
            byte tdata = Data.mem[address];
            if (Inputs[24].On)
            {
                tdata = 0;
                for (int i = 0; i < 8; i++)
                {
                    tdata += Inputs[16 + i].On ? (byte)(1 << i) : (byte)0;
                }
                Data.mem[address] = tdata;
            }
            for (int i = 0; i < 8; i++)
            {
                Outputs[i].On = (tdata & (1 << i)) > 0;
            }
        }

        private bool _HasPersistentValues = true;

        public override bool HasPersistentValues => _HasPersistentValues;

        protected override void SetDataDefaultValues()
        {
            Data.mem = new byte[65536];
        }
    }

    public class HexROM8bit : LogicComponent
    {
        // Label Text length : ComponentData.CustomData[12]
        // Label Text : ComponentData.CustomData[16+i];

        protected override void DoLogicUpdate()
        {
            int address = 0;
            for (int i = 0; i < 16; i++)
            {
                address += Inputs[i].On ? 1 << i : 0;
            }
            byte output = 0;
            if (ComponentData.CustomData != null)
            {
                int strlen = BitConverter.ToInt32(ComponentData.CustomData, 12);
                if (address * 2 + 1 < strlen)
                {
                    string tstr = "";
                    for (int i = 0; i < 2; i++)
                        tstr += (char)ComponentData.CustomData[16 + i + address * 2];
                    output = HexToByte(tstr, address);
                }
            }
            for (int i = 0; i < 8; i++)
            {
                Outputs[i].On = (output & (1 << i)) > 0;
            }
        }

        private byte HexToByte(string istr, int addr)
        {
            int number;
            if (istr.Contains("\n"))
            {
                Logger.Info("Unexpected new line(\\n) in HexROM, at " + addr + ".");
                return 0;
            }
            if (!int.TryParse(istr, System.Globalization.NumberStyles.HexNumber, null, out number))
                Logger.Info("Unexpected character in HexROM: \'" + istr + "\' at " + addr + ".");
            return (byte)number;
        }
    }

    public class AsmROM8bit : LogicComponent
    {
        protected override void DoLogicUpdate()
        {
            int address = 0;
            for (int i = 0; i < 16; i++)
            {
                address += Inputs[i].On ? 1 << i : 0;
            }
            byte output = 0;
            if (ComponentData.CustomData != null)
            {
                int dataoffset = BitConverter.ToInt32(ComponentData.CustomData, 12) + 32;
                if (address + dataoffset < ComponentData.CustomData.Length)
                {
                    output = ComponentData.CustomData[dataoffset + address];
                }
            }
            for (int i = 0; i < 8; i++)
            {
                Outputs[i].On = (output & (1 << i)) > 0;
            }
        }

        protected override void OnCustomDataUpdated()
        {
            QueueLogicUpdate();
        }
    }

    public class WordDLatch : LogicComponent
    {
        protected override void DoLogicUpdate()
        {
            if(Inputs[Inputs.Count-1].On)
            {
                for(int i=0;i<Inputs.Count-1;i++)
                {
                    Outputs[i].On = Inputs[i].On;
                }
            }
        }
    }

    public class WordRelay : LogicComponent
    {
        private bool PreviouslyOpen;

        protected override void DoLogicUpdate()
        {
            bool openNow = Inputs[Inputs.Count - 1].On;
            if(openNow != PreviouslyOpen)
            {
                int nbits = (Inputs.Count - 1) / 2;
                if(openNow)
                {
                    for(int i=0;i<nbits;i++)
                    {
                        Inputs[i].AddPhasicLinkWithUnsafe(Inputs[i + nbits]);
                    }
                }
                else
                {
                    for (int i = 0; i < nbits; i++)
                    {
                        Inputs[i].RemovePhasicLinkWithUnsafe(Inputs[i + nbits]);
                    }
                }
                PreviouslyOpen = openNow;
            }
        }

        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex)
        {
            return inputIndex == Inputs.Count - 1;
        }
    }
}