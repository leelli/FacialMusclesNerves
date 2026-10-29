using System;
using System.Collections.Generic;
using UnityEngine;

namespace maleric
{
    public interface IMeshOwner
    {
        string GUID { get; }
        MeshObjectFactoryInputData[] MeshObjectInputData { get; }
        string Name { get; }
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        bool UseNewInstance { get; }
        GameObject GameObject { get; }
        bool UseLod { get; }

        IMeshObjectFactory GetMeshObjectFactory();
        Matrix4x4 InputParentTransformMatrix { get; }

        /// <summary>
        /// Returns Renderers Used for Input Data. Note: We can have Input Data without those renderers
        /// </summary>
        MeshRenderer[] InputRenderers { get; }
        void SetInputRenderers(MeshRenderer[] renderers);

        /// <summary>
        /// Returns Renderers used for Export Data
        /// </summary>
        MeshRenderer[] OutputRenderers { get; }
        void SetOutputRenderers(MeshRenderer[] renderers);

        /// <summary>
        /// Returns GameObject used as Parent in Merged Object
        /// </summary>
        GameObject OutputGameObject { get; }
        void SetOutputGameObject(GameObject outputGameObject);
    }

    public enum MeshObjectFactoryLodLevel
    {
        Level0,
        Level1,
        Level2,
    }
}

