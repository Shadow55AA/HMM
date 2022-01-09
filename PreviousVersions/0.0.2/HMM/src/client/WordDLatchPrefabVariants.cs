using JimmysUnityUtilities;
using LogicWorld.Interfaces;
using LogicWorld.Rendering.Dynamics;
using LogicWorld.SharedCode.Components;
using System.Collections.Generic;
using UnityEngine;

namespace HMM.Client.ClientCode
{
    public class WordDLatch1PrefabVariantInfo : WordDLatchPrefabVariantBase
    {
        public override string ComponentTextID => "HMM.WordDLatch1Byte";
        public override byte wordSize => 1;
    }

    public class WordDLatch2PrefabVariantInfo : WordDLatchPrefabVariantBase
    {
        public override string ComponentTextID => "HMM.WordDLatch2Byte";
        public override byte wordSize => 2;
    }

    public class WordDLatch4PrefabVariantInfo : WordDLatchPrefabVariantBase
    {
        public override string ComponentTextID => "HMM.WordDLatch4Byte";
        public override byte wordSize => 4;
    }

    public class WordDLatch8PrefabVariantInfo : WordDLatchPrefabVariantBase
    {
        public override string ComponentTextID => "HMM.WordDLatch8Byte";
        public override byte wordSize => 8;
    }

    public abstract class WordDLatchPrefabVariantBase : PrefabVariantInfo
    {
        public override abstract string ComponentTextID { get; }
        public abstract byte wordSize { get; }

        public override PrefabVariantIdentifier GetDefaultComponentVariant()
        {
            return new PrefabVariantIdentifier(wordSize * 8 + 1, wordSize * 8);
        }

        public override ComponentVariant GenerateVariant(PrefabVariantIdentifier identifier)
        {
            List<Block> blocks = new List<Block>();
            blocks.Add(
                new Block
                {
                    RawColor = new Color24(0x349F16),
                    Position = new Vector3(-0.5f, 0, -0.5f),
                    Scale = new Vector3(2, 1, wordSize*8),
                    MeshName = "OriginCube"
                }
            );
            List<ComponentInput> inputs = new List<ComponentInput>();
            List<ComponentOutput> outputs = new List<ComponentOutput>();
            for(int i=0;i<wordSize*8;i++)
            {
                inputs.Add(
                    new ComponentInput
                    {
                        Position = new Vector3(-0.5f, 0.5f, i),
                        Rotation = new Vector3(0f, 0f, 90f),
                        Length = 0.6f
                    }
                    );
                outputs.Add(
                    new ComponentOutput
                    {
                        Position = new Vector3(1f, 1f, i)
                    }
                    );
            }
            inputs.Add(
                new ComponentInput
                {
                    Position = new Vector3(0f, 1f, wordSize * 4 - 0.5f),
                    Length = 0.6f
                }
                );

            return new ComponentVariant
            {
                VariantPrefab = new Prefab
                {
                    Blocks = blocks.ToArray(),
                    Inputs = inputs.ToArray(),
                    Outputs = outputs.ToArray()
                }
            };
        }
    }
}