using UnityEngine;

#if UNITY_EDITOR
#endif


namespace maleric
{
    public interface IBakeableMaterialRuleDefinition
    {
        string MaterialGuid { get; }
        IBakeableMaterialRule GetRule();
    }

    public abstract class ABakeableMaterialRuleDefinition : ScriptableObject, IBakeableMaterialRuleDefinition
    {
        [Reference(typeof(Material))]
        [SerializeField] private string _materialGuid;
        public string MaterialGuid => _materialGuid;

        public void SetMaterialGUID(string guid)
        {
            _materialGuid = guid;
        }

        public abstract IBakeableMaterialRule GetRule();
    }

    public abstract class BakeableMaterialRuleDefinition<T> : ABakeableMaterialRuleDefinition where T : IBakeableMaterialRule
    {
        public T RuleData;

        public override IBakeableMaterialRule GetRule()
        {
            return RuleData;
        }
    }
}
