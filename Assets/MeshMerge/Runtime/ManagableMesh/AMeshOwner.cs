using System;
using UnityEngine;
using System.Collections.Generic;

namespace maleric
{
    public abstract class AMeshOwner : MonoBehaviour, IMeshOwner
    {
        [HideInInspector] [SerializeField] protected string _guid;
        public string GUID => _guid;

        [SerializeField] protected MeshObjectFactoryInputData[] _inputData;
        public MeshObjectFactoryInputData[] MeshObjectInputData => GetMeshesData();

        public string Name => name;

        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;

        [HideInInspector] [SerializeField] protected bool _useNewInstance = true;
        public bool UseNewInstance
        {
            get => _useNewInstance;
            set => _useNewInstance = value;
        }

        public GameObject GameObject => gameObject;
        public abstract bool UseLod { get; }

        public MeshRenderer[] InputRenderers => _inputMeshRenderers;
        [HideInInspector] [SerializeField] protected MeshRenderer[] _inputMeshRenderers;

        public MeshRenderer[] OutputRenderers => _outputMeshRenderers;
        [HideInInspector] [SerializeField] protected MeshRenderer[] _outputMeshRenderers;
        public GameObject OutputGameObject => _outputGameObject;
        public Matrix4x4 InputParentTransformMatrix => this.GetParentMatrix();

        [HideInInspector] [SerializeField] protected GameObject _outputGameObject;

        [HideInInspector] [SerializeField] public MeshRenderer[] EditModeMeshRenderers;

        protected virtual MeshObjectFactoryInputData[] GetMeshesData() { return _inputData; }

        public virtual IMeshObjectFactory GetMeshObjectFactory()
        {
            return this.GetDefaultFactory();
        }

        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(_guid))
                _guid = Guid.NewGuid().ToString();
        }

        protected virtual void Reset()
        {
            if (string.IsNullOrEmpty(_guid))
                _guid = Guid.NewGuid().ToString();
            this.UpdateMeshObjectInputData();
            this.UpdateMeshOwnerMaterialsWithoutRules();
        }

        protected virtual void OnValidate()
        {
            if (BakeableMaterialRulesDefinition.Instance != null)
                this.UpdateMeshOwnerMaterialsWithoutRules();
        }

        public void SetOutputRenderers(MeshRenderer[] renderers)
        {
            _outputMeshRenderers = renderers;
        }

        public void SetOutputGameObject(GameObject outputGameObject)
        {
            _outputGameObject = outputGameObject;
        }

        public void SetInputData(MeshObjectFactoryInputData[] inputData)
        {
            _inputData = inputData;
        }

        public void SetInputRenderers(MeshRenderer[] renderers)
        {
            _inputMeshRenderers = renderers;
		}
	}
}
