using System;
using CoreLib.Util;
using System.Collections.Generic;
using UnityEngine;

namespace CoreLib.Components
{
    public class TemplateBlock : ModEntityMonoBehavior
    {
        public SpriteRenderer verticalRenderer;
        public SpriteRenderer horizontalRenderer;
        public SpriteRenderer verticalEmmisiveRenderer;
        public SpriteRenderer horizontalEmmisiveRenderer;
        public SpriteRenderer shadowSpriteRenderer;
        public GameObject lightGO;
        public Transform SRPivot;
        public void OnOccupied()
        {
        }

        public void OnFree()
        {
        }

        public void OnUse()
        {
        }
    }
}