using HMM.Shared;
using JimmysUnityUtilities;
using LogicWorld.ClientCode.Resizing;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Chunks;
using LogicWorld.Rendering.Components;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace HMM.Client
{
    public class PixelDisplay : ComponentClientCode<IPixelDisplayData>, IResizableX, IResizableZ, ICustomCubeArrowHeight
    {
        byte[] mem;

        // ResizableX
        private int previousSizeX;
        public int SizeX { get { return Data.SizeX; } set { Data.SizeX = value; } }
        public int MinX => 3;
        public int MaxX => 16;
        public float GridIntervalX => 1f;

        // ResizableZ
        private int previousSizeZ;
        public int SizeZ { get { return Data.SizeZ; } set { Data.SizeZ = value; } }
        public int MinZ => 2;
        public int MaxZ => 16;
        public float GridIntervalZ => 1f;

        // ICustomCubeArrowHeight
        public float CubeArrowHeight => 355f / (678f * (float)Math.PI);

        // Texture
        Texture2D screen;
        int screenwidth;
        int screenheight;

        protected override void Initialize()
        {
            mem = new byte[196608];
            screenwidth = 48;
            screenheight = 32;
            if (screen == null)
            {
                screen = new Texture2D(screenwidth, screenheight);
                screen.filterMode = FilterMode.Point;
            }
        }

        protected override void SetDataDefaultValues()
        {
            Data.SizeX = 3;
            Data.SizeZ = 2;
            Data.memdata = null;
        }

        protected override void DataUpdate()
        {
            QueueFrameUpdate();
            if (SizeX != previousSizeX || SizeZ != previousSizeZ)
            {
                setupInputBlock();
                // Panel
                SetBlockScale(0, new Vector3(SizeX, 0.333333343f, SizeZ));
                previousSizeX = SizeX;
                previousSizeZ = SizeZ;
                // Screen
                SetDecorationPosition(0, new Vector3((SizeX/2f-0.5f)*0.3f, 0.3334f*0.3f, (SizeZ/2f-0.5f)*0.3f));
                SetDecorationScale(0, new Vector3(SizeX * 0.3f, SizeZ*0.3f, 1));
                screenwidth = SizeX * 16;
                screenheight = SizeZ * 16;
                screen.Resize(screenwidth, screenheight);
            }
            void setupInputBlock()
            {
                int blocksizex = Math.Min(SizeX, 8);
                int blocksizez = Math.Min(SizeZ, 6);
                SetBlockScale(1, new Vector3(blocksizez - 0.1f, 5f / 6f, blocksizex - 0.1f));
                float xoffset = (0.45f - 2.9f/16f) * (8f - blocksizex) / 5f;
                float zoffset = (0.45f - 1.9f/12f) * (6f - blocksizez) / 4f;
                for (int i=0;i<8;i++)
                {
                    SetInputPosition((byte)i, new Vector3(i * (blocksizex - 0.1f) / 8f - xoffset, -5f / 6f, 0f * (blocksizez - 0.1f) / 6f - zoffset));
                    SetInputPosition((byte)(i + 8), new Vector3(i * (blocksizex - 0.1f) / 8f - xoffset, -5f / 6f, 1f * (blocksizez - 0.1f) / 6f - zoffset));
                    SetInputPosition((byte)(i + 16), new Vector3(i * (blocksizex - 0.1f) / 8f - xoffset, -5f / 6f, 2f * (blocksizez - 0.1f) / 6f - zoffset));
                    SetInputPosition((byte)(i + 24), new Vector3(i * (blocksizex - 0.1f) / 8f - xoffset, -5f / 6f, 3f * (blocksizez - 0.1f) / 6f - zoffset));
                    SetInputPosition((byte)(i + 32), new Vector3(i * (blocksizex - 0.1f) / 8f - xoffset, -5f / 6f, 4f * (blocksizez - 0.1f) / 6f - zoffset));
                }
                SetInputPosition(40, new Vector3(-xoffset, -5f / 6f, 5f * (blocksizez - 0.1f) / 6f - zoffset));
                SetInputPosition(41, new Vector3((blocksizex - 0.1f) / 8f  - xoffset, -5f / 6f, 5f * (blocksizez - 0.1f) / 6f - zoffset));
            }
        }

        protected override void FrameUpdate()
        {
            if(Data.memdata != null)
            {
                MemoryStream stream = new MemoryStream(Data.memdata);
                stream.Position = 0;
                DeflateStream decompressor = new DeflateStream(stream, CompressionMode.Decompress);
                int length = decompressor.Read(mem, 0, 196608);
                for (int i = 0; i < mem.Length; i += 3)
                {
                    int x = (i / 3) % screenwidth;
                    int y = (i / 3)/ screenwidth;
                    byte r = mem[i];
                    byte g = mem[i + 1];
                    byte b = mem[i + 2];
                    if (x < screenwidth && y < screenheight)
                        screen.SetPixel(x, y, new Color(r / 255f, g / 255f, b / 255f));
                }
                screen.Apply();
            }
        }

        public override PlacingRules GenerateDynamicPlacingRules()
        {
            return PlacingRules.FlippablePanelOfSize(SizeX, SizeZ);
        }
        
        protected override IList<IDecoration> GenerateDecorations()
        {
            if (screen == null)
            {
                screenwidth = 48;
                screenheight = 32;
                screen = new Texture2D(screenwidth, screenheight);
                screen.filterMode = FilterMode.Point;
            }
            Material material = new Material(Shader.Find("Unlit/Texture"));
            material.mainTexture = screen;
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gameObject.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            gameObject.GetComponent<Renderer>().material = material;
            return new Decoration[1]
            {
            new Decoration
            {
                LocalPosition = new Vector3(-0.5f, 0.0f, -0.5f) * 0.3f,
                LocalRotation = Quaternion.Euler(90f, 0f, 0f),
                DecorationObject = gameObject,
                AutoSetupColliders = true,
                IncludeInModels = true
            }
            };
        }
    }

    public class PixelDisplayVariantInfo : PrefabVariantInfo
    {
        public override string ComponentTextID => "HMM.PixelDisplay";

        public override PrefabVariantIdentifier GetDefaultComponentVariant()
        {
            return new PrefabVariantIdentifier(42, 0);
        }

        public override ComponentVariant GenerateVariant(PrefabVariantIdentifier identifier)
        {
            if (identifier.OutputCount != 0)
            {
                throw new Exception("Displays cannot have any outputs");
            }
            if (identifier.InputCount < 1)
            {
                throw new Exception("Displays must have at least one input");
            }
            ComponentInput[] array = new ComponentInput[identifier.InputCount];
            for (int i = 0; i < array.Length; i++)
            {
                int col = i % 8;
                int row = i / 8;
                float length = col/8f * 0.6f + 0.4f;
                array[i] = new ComponentInput
                {
                    Position = new Vector3(col, row, 0f),
                    Rotation = new Vector3(180f, 0f, 0f),
                    Length = length
                };
            }
            ComponentVariant componentVariant = new ComponentVariant();
            componentVariant.VariantPrefab = new Prefab
            {
                Blocks = new Block[2]
                {
                new Block
                {
                    Position = new Vector3(-0.5f, 0f, -0.5f),
                    MeshName = "OriginCube",
                    RawColor = Color24.Black
                },
                new Block
                {
                    Position = new Vector3(-0.45f, 0f, -0.45f),
                    Rotation = new Vector3(180f, 270f, 0f),
                    MeshName = "OriginCube_OpenBottom",
                    ColliderData = new ColliderData
                    {
                        Transform = new ColliderTransform
                        {
                            Scale = new Vector3(1f, 0.4f, 1f),
                            Position = new Vector3(0f, 0.6f, 0f)
                        }
                    }
                }
                },
                Inputs = array
            };
            return componentVariant;
        }
    }
}
