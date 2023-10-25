using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CoreLib.Util
{
    [CreateAssetMenu(fileName = "SubmodulesData", menuName = "CoreLib/New SubmodulesData", order = 2)]
    public class SubmodulesData : ScriptableObject
    {
        public string[] submoduleNames;
    }
}