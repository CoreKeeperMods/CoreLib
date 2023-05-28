using System;
using UnityEngine;

namespace EditorKit.Scripts
{
    public class TileGroup : IEquatable<TileGroup>
    {
        public readonly bool useQuad;
        public readonly Texture2D basetex;
        public readonly Texture2D toptex;
        public readonly Texture2D sidetex;

        public TileGroup(bool useQuad, Texture2D basetex, Texture2D toptex, Texture2D sidetex)
        {
            this.useQuad = useQuad;
            this.basetex = basetex;
            this.toptex = toptex;
            this.sidetex = sidetex;
        }

        public bool Equals(TileGroup other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return useQuad == other.useQuad && TextureEqual(basetex, other.basetex) && TextureEqual(toptex, other.toptex) && TextureEqual(sidetex, other.sidetex);
        }

        private bool TextureEqual(Texture2D a, Texture2D b)
        {
            if (a == null)
                return b == null;
            if (b == null)
                return a == null;

            return a.name.Equals(b.name);
        }

        private string TextureName(Texture2D tex)
        {
            if (tex == null)
                return "";

            return tex.name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TileGroup)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(useQuad, TextureName(basetex), TextureName(toptex), TextureName(sidetex));
        }

        public static bool operator ==(TileGroup left, TileGroup right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TileGroup left, TileGroup right)
        {
            return !Equals(left, right);
        }
    }
}