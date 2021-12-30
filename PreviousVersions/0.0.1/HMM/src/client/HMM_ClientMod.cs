using JimmysUnityUtilities;
using LogicAPI.Client;
using LogicUI;
using LogicWorld.Audio;
using LogicWorld.ClientCode;
using LogicWorld.ClientCode.Decorations;
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

    public class AsmROM8bit : ComponentClientCode<AsmROM8bit.IData>, IColorableClientCode, IPressableButton
    {
        public interface IData : Label.IData
        {
            byte[] Zdata { get; set; }
        }

        Assembler assembler = new Assembler();

        // Button
        private static readonly Vector3 Down = new Vector3(0f, -0.045f, 0f);
        protected Vector3 UpLocalPosition;
        protected Vector3 DownLocalPosition => UpLocalPosition + Down;
        protected MeshRenderer VisualButton;
        protected BoxCollider flatCollider;
        protected BoxCollider buttonShapeCollider;
        private bool ButtonDown = false;
        private bool? previousDown;
        private bool firstDataUpdateOver;
        private static Color24 ButtonColorD = new Color24(255, 0, 0);
        private static Color24 ButtonColorC = new Color24(0, 255, 0);
        private bool IsCompiled = false;

        public void MousePressDown()
        {
            if (previousDown == false)
            {
                ButtonDown = true;
                if(assembler.Assemble(Data.LabelText, Data.Zdata))
                    Logger.Info(assembler.errormessage);
                QueueFrameUpdate();
                Data.SizeX = Data.SizeX;
            }
        }

        public void MousePressUp()
        {
            ButtonDown = false;
            QueueFrameUpdate();
        }

        // Text
        private static Color24 DefaultColor = new Color24(38, 38, 38);

        private LabelTextManager TextManager;
        private RectTransform TextManagerRT;

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

        private float Height => base.CodeInfoFloats[0];

        string IColorableClientCode.ColorsFileKey => "LabelText";

        float IColorableClientCode.MinColorValue => 0f;

        protected override void DataUpdate()
        {
            TextManager.DataUpdate(base.Data);
            if (TextManagerRT != null)
            {
                TextManagerRT.sizeDelta = new Vector2(8, 7) * 0.3f;
            }
            IsCompiled = false;
            UpdateButtonMaterial();
        }

        protected override void FrameUpdate()
        {
            UpdateButtonMaterial();
            if (base.PlacedInMainWorld && previousDown != ButtonDown)
            {
                if (firstDataUpdateOver)
                {
                    SoundPlayer.PlaySoundAt(ButtonDown ? Sounds.ButtonDown : Sounds.ButtonUp, base.Address);
                }
                Vector3 newLocalPosition = (ButtonDown ? DownLocalPosition : UpLocalPosition);
                TweenDecorationPosition(1, newLocalPosition, 0.04f);
                previousDown = ButtonDown;
                firstDataUpdateOver = true;
            }
        }

        private void UpdateButtonMaterial()
        {
            if(IsCompiled)
                VisualButton.material = Materials.StandardColor(ButtonColorC);
            else
                VisualButton.material = Materials.StandardColor(ButtonColorD);
        }

        // Button
        protected override void OnComponentReRendered()
        {
            ButtonDown = false;
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
            base.Data.Zdata = new byte[65536];
        }

        protected override IList<IDecoration> GenerateDecorations()
        {
            // Text
            GameObject gameObject = Object.Instantiate(Prefabs.ComponentDecorations.LabelText);
            TextManager = gameObject.GetComponent<LabelTextManager>();
            TextManagerRT = TextManager.GetRectTransform();
            TextManagerRT.sizeDelta = new Vector2(8, 7)*0.3f;

            // Button
            Vector3 rawBlockScale = GetRawBlockScale();
            UpLocalPosition = new Vector3(rawBlockScale.x / 2f - 0.15f, rawBlockScale.y, rawBlockScale.z / 16f - 0.15f);
            GameObject gameObject2 = Object.Instantiate(Prefabs.ComponentDecorations.Button);
            VisualButton = gameObject2.GetComponentInChildren<MeshRenderer>();
            VisualButton.transform.localScale = new Vector3(rawBlockScale.x - 0.09f, 0.06f, rawBlockScale.z/8 - 0.09f);

            GameObject gameObject3 = new GameObject("button colliders");
            gameObject3.AddComponent<ButtonInteractable>().Button = this;
            flatCollider = gameObject3.AddComponent<BoxCollider>();
            flatCollider.size = new Vector3(rawBlockScale.x, 0.02f, rawBlockScale.z/8);
            buttonShapeCollider = gameObject3.AddComponent<BoxCollider>();
            buttonShapeCollider.size = VisualButton.transform.localScale;
            buttonShapeCollider.center = new Vector3(0f, buttonShapeCollider.size.y / 2f, 0f);

            return new Decoration[3]
            {
            new Decoration
            {
                LocalPosition = new Vector3(-0.5f, Height + 0.01f, 0.5f) * 0.3f,
                LocalRotation = Quaternion.Euler(90f, 0f, 0f),
                DecorationObject = gameObject,
                IncludeInModels = true
            },
            new Decoration
            {
                LocalPosition = UpLocalPosition,
                DecorationObject = gameObject2,
                IncludeInModels = true,
                AutoSetupColliders = true
            },
            new Decoration
            {
                LocalPosition = UpLocalPosition,
                DecorationObject = gameObject3,
                AutoSetupColliders = true
            }
            };
        }
    }
}
