using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{


    [Serializable]
    public struct MeshObjectFactoryInputData
    {
        [HideInInspector] public string Name;
        public Mesh Mesh;
        [maleric.Reference(typeof(Material))]
        public string[] Materials;
        public Matrix4x4 Transform;
        public MeshObjectFactoryLodLevel LodLevel;

        public override bool Equals(object obj)
        {
            bool isEqual = false;
            if (obj is MeshObjectFactoryInputData)
            {
                MeshObjectFactoryInputData bakeable = (MeshObjectFactoryInputData)obj;
                isEqual = Name.Equals(bakeable.Name) && Mesh == bakeable.Mesh && Transform.Equals(bakeable.Transform);
                if (isEqual)
                {
                    if (Materials != null && bakeable.Materials != null)
                    {
                        if (Materials.Length == bakeable.Materials.Length)
                        {
                            for (int i = 0; i < Materials.Length; i++)
                            {
                                isEqual = Materials[i].Equals(bakeable.Materials[i]);
                                if (!isEqual)
                                    break;
                            }
                        }
                        else
                        {
                            isEqual = false;
                        }
                    }
                    else if (Materials == null && bakeable.Materials == null)
                    {
                        isEqual = true;
                    }
                    else
                    {
                        isEqual = false;
                    }
                }

            }
            return isEqual;
        }

        public override int GetHashCode()
        {
            int hash = Name != null ? Name.GetHashCode() : 0;
            hash = hash * 23 + (Mesh != null ? Mesh.GetHashCode() : 0);
            hash = hash * 23 + Transform.GetHashCode();
            hash = hash * 23 + LodLevel.GetHashCode();
            if (Materials != null)
                for (int i = 0; i < Materials.Length; i++)
                    hash = hash * 23 + (Materials[i] != null ? Materials[i].GetHashCode() : 0);
            return hash;
        }

    }
}