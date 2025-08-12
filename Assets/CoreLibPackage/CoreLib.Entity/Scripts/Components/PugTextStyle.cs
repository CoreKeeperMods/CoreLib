using Pug.UnityExtensions;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity
{
    /// <summary>
    /// Represents a set of properties and configurations to define the style of text rendering
    /// within the application.
    /// </summary>
    public class PugTextStyle : MonoBehaviour
    {
        /// <summary>
        /// Represents the font face style to be applied to text elements.
        /// </summary>
        /// <remarks>
        /// The fontFace variable is used to define the specific font style, such as bold or large text,
        /// applied to a text element within the PugText system. It is part of the styling parameters that influence
        /// text appearance in the associated text component.
        /// </remarks>
        public TextManager.FontFace fontFace = TextManager.FontFace.boldLarge;

        /// <summary>
        /// Defines the capitalization style applied to the text, specifying
        /// how the text case is formatted (e.g., all uppercase, all lowercase, or as provided).
        /// </summary>
        /// <remarks>
        /// Used to enforce specific text casing rules during rendering in the
        /// PugTextStyle system, this property provides customization for text appearance
        /// and consistency across text elements.
        /// </remarks>
        [Header("String processing")] public global::PugTextStyle.Capitalization capitalization;

        /// <summary>
        /// Specifies the horizontal alignment of text within a text element.
        /// </summary>
        /// <remarks>
        /// The horizontalAlignment variable determines how text content is positioned horizontally
        /// relative to its bounding area. It can be set to align text to the left, center, or right,
        /// allowing for flexible formatting options for text layout in the PugText system.
        /// </remarks>
        [Header("Alignment")]
        public global::PugTextStyle.HorizontalAlignment horizontalAlignment = global::PugTextStyle.HorizontalAlignment.center;

        /// <summary>
        /// Specifies the vertical alignment of text elements within a layout or container.
        /// </summary>
        /// <remarks>
        /// The verticalAlignment variable determines how text content is positioned vertically,
        /// such as top, center, or bottom, relative to its containing area. It is used as a part of the text styling
        /// framework to control the vertical alignment aspects of the text layout.
        /// </remarks>
        public global::PugTextStyle.VerticalAlignment verticalAlignment = global::PugTextStyle.VerticalAlignment.center;

        /// <summary>
        /// Specifies additional spacing to be applied between individual characters in a text element.
        /// </summary>
        /// <remarks>
        /// The extraCharSpacing variable adjusts the spacing between characters,
        /// allowing for finer control over text appearance for better readability
        /// or stylistic purposes within the PugText system. This value can be used in
        /// scenarios where default text spacing needs customization.
        /// </remarks>
        [Header("Spacing")]
        public int extraCharSpacing;

        /// <summary>
        /// Specifies the additional width, in pixels, to be applied to spaces within text elements.
        /// </summary>
        /// <remarks>
        /// The extraSpaceWidth variable is used to adjust the spacing between words or characters
        /// by increasing or decreasing the default space width. It is useful for fine-tuning the layout
        /// of text for improved readability or design consistency.
        /// </remarks>
        public int extraSpaceWidth;

        /// <summary>
        /// Specifies the additional spacing between lines of text.
        /// </summary>
        /// <remarks>
        /// The extraLineSpacing variable is used to adjust the vertical distance between consecutive lines
        /// in a text block. This value is applied as an additional offset to the default line spacing,
        /// allowing for customization of text layout and improving readability based on stylistic or design requirements
        /// within the PugText system.
        /// </remarks>
        public int extraLineSpacing;

        /// <summary>
        /// Defines the additional spacing applied specifically to empty lines during text rendering.
        /// </summary>
        /// <remarks>
        /// The extraEmptyLineSpacing variable allows customization of spacing for empty lines, adding
        /// a defined amount of space beyond the default line spacing. This can assist in improving
        /// the visual flow and layout of multiline text content, particularly in scenarios where
        /// additional separation between blank lines is desired.
        /// </remarks>
        public int extraEmptyLineSpacing;

        /// <summary>
        /// Determines whether the text should be rendered using a monospace font style.
        /// </summary>
        /// <remarks>
        /// When set to true, the text will adopt a monospace format, ensuring that each character
        /// occupies an equal amount of horizontal space regardless of its individual width. This can
        /// be useful for aligning text in tabular formats or maintaining uniformity in character spacing.
        /// </remarks>
        public bool forceMonospace;

        /// <summary>
        /// Specifies the horizontal offset applied to text when rendering for right-to-left languages.
        /// </summary>
        /// <remarks>
        /// This variable is used to adjust the positioning of text elements to ensure proper alignment
        /// and layout for languages that are written and read from right to left, such as Arabic or Hebrew.
        /// The specified offset is applied to text to correct its horizontal placement within the layout.
        /// </remarks>
        [Header("Right To Left Languages")] public float rightToLeftXOffset;

        /// <summary>
        /// Determines whether the horizontal alignment of text should be inverted.
        /// </summary>
        /// <remarks>
        /// The invertHorizontalAlignment variable is used to reverse the horizontal alignment direction of rendered text.
        /// This feature is particularly useful for supporting layout adjustments, such as mirroring UI elements or accommodating
        /// languages with right-to-left text directionality.
        /// </remarks>
        public bool invertHorizontalAlignment;

        /// <summary>
        /// Represents the main color used by the SpriteRenderer for rendering text in the PugTextStyle component.
        /// </summary>
        /// <remarks>
        /// This property defines the coloration of the text, influencing how the text appears when rendered.
        /// It is configurable via the Unity Editor and defaults to white.
        /// </remarks>
        [Header("SpriteRenderer options")] public Color color = Color.white;

        /// <summary>
        /// Specifies the rendering sorting layer used to determine the draw order of the text element within a 2D or UI renderer context.
        /// </summary>
        /// <remarks>
        /// The sortingLayer variable defines which layer the text element should render on, relative to other elements.
        /// Layers with higher sorting values are rendered in front of layers with lower values, enabling control over visual stacking.
        /// This allows for proper ordering of overlapping graphical elements and ensures the text component appears in the desired render hierarchy.
        /// </remarks>
        [SortingLayer]
        public int sortingLayer = int.MinValue;

        /// <summary>
        /// Specifies the rendering order of the text object within its assigned sorting layer.
        /// Determines the draw order when multiple objects share the same sorting layer.
        /// </summary>
        /// <remarks>
        /// The orderInLayer variable allows precise control over the stacking or layering
        /// of text objects within a single sorting layer. A higher value places the object
        /// above others with lower values in terms of visual hierarchy.
        /// </remarks>
        public int orderInLayer = 9999;

        /// <summary>
        /// Specifies the interaction mode of the text with a SpriteMask in Unity.
        /// </summary>
        /// <remarks>
        /// The maskInteraction variable determines how the text element interacts with SpriteMasks,
        /// which can be used to hide or reveal certain parts of the text based on the mask settings.
        /// It is particularly useful for creating clipping effects or managing visibility in complex UIs.
        /// </remarks>
        public SpriteMaskInteraction maskInteraction;

        /// <summary>
        /// Called when the script instance is being loaded. Configures the PugTextStyle properties
        /// defined in the component and applies them to the associated PugText component, if available.
        /// </summary>
        public void Awake()
        {
            var text = GetComponent<PugText>();
            if (text == null) return;

            Apply(text);
        }

        /// <summary>
        /// Applies the current PugTextStyle settings to the specified PugText instance.
        /// </summary>
        /// <param name="text">The PugText instance to which the style settings will be applied.</param>
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