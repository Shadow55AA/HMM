using HMM.Shared;
using LogicWorld.Server.Circuitry;
using System.IO;
using System.IO.Compression;
using System.Timers;

namespace HMM.Server
{
    public class PixelDisplay : LogicComponent<IPixelDisplayData>
    {
        // ScreenUpdates
        Timer screenupdatetimer;
        bool timertick = false;
        bool ismemdirty = false;
        bool loadfromsave = false;
        MemoryStream memstream;

        int screenwidth = 48;
        int screenheight = 32;

        byte[] mem;

        protected override void Initialize()
        {
            memstream = new MemoryStream();
            mem = new byte[196608];
            screenupdatetimer = new Timer(100);
            screenupdatetimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
            screenupdatetimer.AutoReset = true;
            screenupdatetimer.Start();
            loadfromsave = true;
        }

        public override void Dispose()
        {
            memstream.Dispose();
            screenupdatetimer.Stop();
            screenupdatetimer.Dispose();
            base.Dispose();
        }

        public void OnTimerElapsed(object source, ElapsedEventArgs args)
        {
            if (ismemdirty)
            {
                timertick = true;
            }
        }

        protected override void DoLogicUpdate()
        {
            if (Inputs[40].On)
            {
                if (Inputs[41].On)
                {
                    int r = 0, g = 0, b = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        r += Inputs[i + 16].On ? 1 << i : 0;
                        g += Inputs[i + 24].On ? 1 << i : 0;
                        b += Inputs[i + 32].On ? 1 << i : 0;
                    }
                    for(int i=0;i< screenwidth; i++)
                    {
                        for (int j = 0; j < screenheight; j++)
                        {
                            mem[(i + j * screenwidth) * 3] = (byte)r;
                            mem[(i + j * screenwidth) * 3 + 1] = (byte)g;
                            mem[(i + j * screenwidth) * 3 + 2] = (byte)b;
                        }
                    }
                }
                else
                {
                    int addressx = 0;
                    int addressy = 0;
                    int r = 0, g = 0, b = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        addressx += Inputs[i].On ? 1 << i : 0;
                        addressy += Inputs[i + 8].On ? 1 << i : 0;
                        r += Inputs[i + 16].On ? 1 << i : 0;
                        g += Inputs[i + 24].On ? 1 << i : 0;
                        b += Inputs[i + 32].On ? 1 << i : 0;
                    }
                    mem[(addressx + addressy * screenwidth) * 3] = (byte)r;
                    mem[(addressx + addressy * screenwidth) * 3 + 1] = (byte)g;
                    mem[(addressx + addressy * screenwidth) * 3 + 2] = (byte)b;
                }
                ismemdirty = true;
            }
            if (ismemdirty)
                QueueLogicUpdate();
            if(ismemdirty&&timertick)
            {
                WriteScreenToData();
                timertick = false;
                ismemdirty = false;
            }
        }

        protected override void OnCustomDataUpdated()
        {
            if (screenwidth != Data.SizeX * 16 || screenheight != Data.SizeZ * 16)
            {
                screenwidth = Data.SizeX * 16;
                screenheight = Data.SizeZ * 16;
            }
            if(loadfromsave && Data.memdata!=null)
            {
                MemoryStream stream = new MemoryStream(Data.memdata);
                stream.Position = 0;
                DeflateStream decompressor = new DeflateStream(stream, CompressionMode.Decompress);
                int length = decompressor.Read(mem, 0, 196608);
                loadfromsave = false;
            }
        }

        protected override void SetDataDefaultValues()
        {
            Data.SizeX = 3;
            Data.SizeZ = 2;
            Data.memdata = null;
        }

        private void WriteScreenToData()
        {
            memstream.Position = 0;
            DeflateStream compressor = new DeflateStream(memstream, CompressionLevel.Fastest, true);
            compressor.Write(mem, 0, screenwidth * screenheight * 3);
            compressor.Flush();
            int length = (int)memstream.Position;
            memstream.Position = 0;
            byte[] bytes = new byte[length];
            memstream.Read(bytes, 0, length);
            Data.memdata = bytes;
        }
    }
}
