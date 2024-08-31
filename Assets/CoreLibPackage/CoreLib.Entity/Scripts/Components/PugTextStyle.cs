using UnityEngine;

namespace CoreLib.Submodules.ModEntity
{
    public class PugTextStyle : MonoBehaviour
    {
        public TextManager.FontFace fontFace = TextManager.FontFace.boldLarge;
        [Header("String processing")]
        public global::PugTextStyle.Capitalization capitalization;
        [Header("Alignment")]
        public global::PugTextStyle.HorizontalAlignment horizontalAlignment = global::PugTextStyle.HorizontalAlignment.center;
        public global::PugTextStyle.VerticalAlignment verticalAlignment = global::PugTextStyle.VerticalAlignment.center;
        [Header("Spacing")]
        public int extraCharSpacing;
        public int extraSpaceWidth;
        public int extraLineSpacing;
        public int extraEmptyLineSpacing;
        public bool forceMonospace;
        [Header("Right To Left Languages")]
        public float rightToLeftXOffset;
        public bool invertHorizontalAlignment;
        [Header("SpriteRenderer options")]
        public Color color = Color.white;
        [SortingLayer]
        public int sortingLayer = int.MinValue;
        public int orderInLayer = 9999;
        public SpriteMaskInteraction maskInteraction;
        
        public void Awake()
        {
            var text = GetComponent<PugText>();
            if (text == null) return;
            
            Apply(text);
        }

        protected void Apply(PugText text)
        {
            text.style.fontFace = fontFace;
            text.style.capitalization = capitalization;
            text.style.horizontalAlignment = horizontalAlignment;
            text.style.verticalAlignment = verticalAlignment;
            text.style.extraCharSpacing = extraCharSpacing;
            text.style.extraSpaceWidth = extraSpaceWidth;
            text.style.extraLineSpacing = extraLineSpacing;
            text.style.extraEmptyLineSpacing = extraEmptyLineSpacing;
            text.style.forceMonospace = forceMonospace;
            text.style.rightToLeftXOffset = rightToLeftXOffset;
            text.style.invertHorizontalAlignment = invertHorizontalAlignment;
            text.style.color = color;
            text.style.sortingLayer = sortingLayer;
            text.style.orderInLayer = orderInLayer;
            text.style.maskInteraction = maskInteraction;
        }
    }
}