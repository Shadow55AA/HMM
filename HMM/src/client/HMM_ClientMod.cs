using JimmysUnityUtilities;
using LogicAPI.Client;
using LogicUI;
using LogicWorld.ClientCode;
using LogicWorld.ClientCode.LabelAlignment;
using LogicWorld.Interfaces;
using LogicWorld.References;
using LogicWorld.Rendering.Chunks;
using LogicWorld.Rendering.Components;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HMM.Client
{
    public class HMM_ClientMod : ClientMod
    {
        protected override void Initialize()
        {
        }
    }

    public class HexROM8bit : ComponentClientCode<Label.IData>, IColorableClientCode
    {
        private static Color24 DefaultColor = new Color24(38, 38, 38);

        private LabelTextManager TextManager;

        public int SizeX
        {
            get
            {
                return base.Data.SizeX;
            }
            set
            {
                base.Data.SizeX = value;
            }
        }

        public int SizeZ
        {
            get
            {
                return base.Data.SizeZ;
            }
            set
            {
                base.Data.SizeZ = value;
            }
        }

        private float Height => base.CodeInfoFloats[0];

        Color24 IColorableClientCode.Color
        {
            get
            {
                return base.Data.LabelColor;
            }
            set
            {
                base.Data.LabelColor = value;
            }
        }

        string IColorableClientCode.ColorsFileKey => "LabelText";

        float IColorableClientCode.MinColorValue => 0f;

        protected override void DataUpdate()
        {
            TextManager.DataUpdate(base.Data);
        }

        protected override void SetDataDefaultValues()
        {
            base.Data.LabelText = "Enter only hexadecimal.";
            base.Data.LabelFontSizeMax = 0.8f;
            base.Data.LabelColor = DefaultColor;
            base.Data.LabelMonospace = false;
            base.Data.HorizontalAlignment = LabelAlignmentHorizontal.Center;
            base.Data.VerticalAlignment = LabelAlignmentVertical.Middle;
            base.Data.SizeX = 8;
            base.Data.SizeZ = 8;
        }

        protected override IList<IDecoration> GenerateDecorations()
        {
            GameObject gameObject = Object.Instantiate(Prefabs.ComponentDecorations.LabelText);
            TextManager = gameObject.GetComponent<LabelTextManager>();
            return new Decoration[1]
            {
            new Decoration
            {
                LocalPosition = new Vector3(-0.5f, Height + 0.01f, -0.5f) * 0.3f,
                LocalRotation = Quaternion.Euler(90f, 0f, 0f),
                DecorationObject = gameObject,
                IncludeInModels = true
            }
            };
        }
    }
}
