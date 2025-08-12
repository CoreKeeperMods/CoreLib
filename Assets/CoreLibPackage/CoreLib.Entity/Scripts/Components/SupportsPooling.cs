using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Components
{
    /// <summary>
    /// A Unity MonoBehaviour component that designates whether an object supports pooling behavior.
    /// </summary>
    /// <remarks>
    /// This class is intended to be attached to GameObjects as a marker or indicator for pooling support.
    /// It may influence the behavior of other systems, particularly those related to object management or
    /// rendering.
    /// </remarks>
    public class SupportsPooling : MonoBehaviour
    {
    }
}