using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace maleric
{
	[Serializable]
	public struct MaterialRulePair
	{
		public string MaterialGuid;
		public BakeableMaterialRule Rule;
		public bool KeepForSplittable;
	}

	[Serializable]
	public struct BakeableOriginalMaterials
	{
		public string Guid;
		public List<string> Materials;
	}

	[CreateAssetMenu(fileName = "BakeableMaterialRulesDefinition", menuName = "maleric/Baking/BakeableMaterialRulesDefinition", order = 40)]
	public class BakeableMaterialRulesDefinition : ScriptableObject
	{
		/// <summary>
		/// Returns the rules definition, resolving it through Resources (with an
		/// editor-only AssetDatabase fallback) instead of caching it in a static
		/// field, so no static state survives between Play mode sessions.
		/// </summary>
		public static BakeableMaterialRulesDefinition Instance
		{
			get
			{
				var definitions = Resources.LoadAll<BakeableMaterialRulesDefinition>("");
				var instance = definitions != null && definitions.Length > 0 ? definitions[0] : null;
#if UNITY_EDITOR
				if (instance == null)
				{
					var guids = UnityEditor.AssetDatabase.FindAssets("t:BakeableMaterialRulesDefinition");
					if (guids != null && guids.Length > 0)
						instance = UnityEditor.AssetDatabase.LoadAssetAtPath<BakeableMaterialRulesDefinition>(
							UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
				}
#endif
				return instance;
			}
		}

		[HideInInspector][SerializeField] public List<MaterialRulePair> _defaultMaterialRules = new List<MaterialRulePair>();
		[HideInInspector][SerializeField] public List<string> _bakeableGuids = new List<string>();
		[HideInInspector][SerializeField] public List<BakeableOriginalMaterials> _bakeableMaterials = new List<BakeableOriginalMaterials>();

		private IBakeableMaterialRuleDefinition[] _fetchedRules;

		public Dictionary<string, IBakeableMaterialRule> GetRules(string[] materialGuids, bool includeMaterialsWithoutRules = false, MeshObjectFactoryLodLevel lodLevel = MeshObjectFactoryLodLevel.Level0)
		{
			return InternalGetRules(new List<string>(materialGuids), includeMaterialsWithoutRules, lodLevel);
		}

		public Dictionary<string, IBakeableMaterialRule> GetRules(List<string> materialGuids, bool includeMaterialsWithoutRules = false, MeshObjectFactoryLodLevel lodLevel = MeshObjectFactoryLodLevel.Level0)
		{
			return InternalGetRules(new List<string>(materialGuids), includeMaterialsWithoutRules, lodLevel);
		}

		private Dictionary<string, IBakeableMaterialRule> InternalGetRules(List<string> materialGuids, bool includeMaterialsWithoutRules = false, MeshObjectFactoryLodLevel lodLevel = MeshObjectFactoryLodLevel.Level0)
		{
			CheckAndFetchRules();
			Dictionary<string, IBakeableMaterialRule> rulesMap = new Dictionary<string, IBakeableMaterialRule>();

			for (int i = 0; i < _fetchedRules.Length; i++)
			{
				if (materialGuids.Count == 0)
					break;

				IBakeableMaterialRule rule = null;
				string materialGuid = _fetchedRules[i].MaterialGuid;
				if (materialGuids.Contains(materialGuid))
				{
					rule = _fetchedRules[i].GetRule();

					if (rule.GetPreferedLodLevel().Equals(lodLevel))
						materialGuids.Remove(materialGuid);

					CleanUnusedDefaultMaterialRules(materialGuid);
				}
				if (rule != null)
				{
					if (!rulesMap.ContainsKey(materialGuid))
					{
						rulesMap.Add(materialGuid, rule);
					}
					else
					{
						Debug.LogWarning("BakeableMaterialRulesDefinition: Found another rule for material with instance " + materialGuid);
						rulesMap[materialGuid] = rule;
					}
				}
			}

			if (includeMaterialsWithoutRules)
			{
				InternalUpdateDefaultRules(ref rulesMap, materialGuids);
			}

			return rulesMap;
		}

		public Dictionary<string, IBakeableMaterialRule> GetDefaultRules(List<string> originMaterialGuids)
		{
			CheckAndFetchRules();
			Dictionary<string, IBakeableMaterialRule> rulesMap = new Dictionary<string, IBakeableMaterialRule>();
			InternalUpdateDefaultRules(ref rulesMap, new List<string>(originMaterialGuids));
			return rulesMap;
		}

		private void InternalUpdateDefaultRules(ref Dictionary<string, IBakeableMaterialRule> rules, List<string> materialGuids)
		{
			for (int i = 0; i < materialGuids.Count; i++)
			{
				if (!rules.ContainsKey(materialGuids[i]) && CheckAndGetMaterialRulePair(materialGuids[i], out MaterialRulePair pair, out int foundIndex))
				{
					rules.Add(pair.MaterialGuid, pair.Rule);
				}
			}
		}

		public bool CheckAndGetMaterialRulePair(string materialGuid, out MaterialRulePair pair, out int foundIndex)
		{
			foundIndex = -1;
			pair = new MaterialRulePair();
			int count = _defaultMaterialRules.Count;
			for (int i = 0; i < count; i++)
			{
				if (_defaultMaterialRules[i].MaterialGuid.Contains(materialGuid))
				{
					foundIndex = i;
					pair = _defaultMaterialRules[i];
					break;
				}
			}
			return foundIndex != -1;
		}

		public IBakeableMaterialRule GetRule(string materialGuid, bool includeMaterialsWithoutRules = false)
		{
			IBakeableMaterialRule rule = null;

			if (CheckAndGetMaterialRulePair(materialGuid, out MaterialRulePair pair, out int foundIndex))
			{
				if (includeMaterialsWithoutRules)
					rule = pair.Rule;
			}
			if (rule == null && _bakeableGuids.Contains(materialGuid))
			{
				for (int i = 0; i < _fetchedRules.Length; i++)
				{
					if (materialGuid.Equals(_fetchedRules[i].MaterialGuid))
					{
						rule = _fetchedRules[i].GetRule();
						break;
					}
				}
			}

			return rule;
		}

		/// <summary>
		/// TODO Complete this, we have updated structure with bakeable-materials and we remove or add new one
		/// Still need to call it from OnValidate and similar from bakeable mesh and make sure we get guid from bakeable mesh
		/// </summary>
		/// <param name="meshOwner"></param>
		/// <param name="materialGuids"></param>
		public void UpdateMaterialsWithoutRules(IMeshOwner meshOwner, List<string> materialGuids, bool forceKeepRules = false)
		{
#if UNITY_EDITOR
			materialGuids = new List<string>(materialGuids);
			CheckAndFetchRules();

			if (!forceKeepRules)
			{
				int foundIndex = -1;
				if (_bakeableGuids.Contains(meshOwner.GUID))
				{
					for (int i = 0; i < _bakeableMaterials.Count; i++)
					{
						var materialsToCheckForUnused = new List<string>();
						if (_bakeableMaterials[i].Guid.Equals(meshOwner.GUID))
						{
							foundIndex = i;
							break;
						}
					}
				}
				List<string> removedMaterials = new List<string>();
				if (foundIndex > -1)
				{
					for (int i = 0; i < _bakeableMaterials[foundIndex].Materials.Count; i++)
					{
						if (!materialGuids.Contains(_bakeableMaterials[foundIndex].Materials[i]))
						{
							removedMaterials.Add(_bakeableMaterials[foundIndex].Materials[i]);
						}
					}

					for (int i = 0; i < _bakeableMaterials.Count; i++)
					{
						if (!_bakeableMaterials[i].Guid.Equals(meshOwner.GUID))
						{
							List<string> materialsToFind = new List<string>(removedMaterials);
							foreach (var mat in materialsToFind)
							{
								if (_bakeableMaterials[i].Materials.Contains(mat))
								{
									removedMaterials.Remove(mat);
								}
							}

							if (removedMaterials.Count == 0)
								break;
						}
					}

					_bakeableMaterials[foundIndex] = new BakeableOriginalMaterials()
					{
						Guid = meshOwner.GUID,
						Materials = materialGuids
					};

				}
				else
				{
					_bakeableMaterials.Add(new BakeableOriginalMaterials()
					{
						Guid = meshOwner.GUID,
						Materials = materialGuids
					});
					foundIndex = _bakeableMaterials.Count - 1;
				}

				for (int i = 0; i < removedMaterials.Count; i++)
				{
					CleanUnusedDefaultMaterialRules(removedMaterials[i]);
				}
			}


			for (int i = 0; i < materialGuids.Count; i++)
			{
				if (!CheckAndGetMaterialRulePair(materialGuids[i], out MaterialRulePair pair, out int foundIndex))
				{
					var material = ReferenceAttributeDrawer.GetObjectByGuid<Material>(materialGuids[i]);
					_defaultMaterialRules.Add(new MaterialRulePair()
					{
						MaterialGuid = materialGuids[i],
						Rule = new BakeableMaterialRule() { Material = material },
						KeepForSplittable = forceKeepRules
					});
				}
				else if (forceKeepRules)
				{
					_defaultMaterialRules[foundIndex] = new MaterialRulePair()
					{
						KeepForSplittable = forceKeepRules,
						MaterialGuid = _defaultMaterialRules[foundIndex].MaterialGuid,
						Rule = _defaultMaterialRules[foundIndex].Rule
					};
				}
			}


			for (int i = 0; i < _fetchedRules.Length; i++)
			{
				if (materialGuids.Contains(_fetchedRules[i].MaterialGuid))
				{
					CleanUnusedDefaultMaterialRules(_fetchedRules[i].MaterialGuid);
				}
			}
#endif
		}

		private void CleanUnusedDefaultMaterialRules(string materialGuid)
		{
			if (!Application.isPlaying)
			{
				if (CheckAndGetMaterialRulePair(materialGuid, out MaterialRulePair pair, out int foundIndex))
				{
					if (!pair.KeepForSplittable)
						_defaultMaterialRules.Remove(pair);
				}
			}
		}


		public List<Material> GetUniqueMaterialsFromRules(List<IBakeableMaterialRule> rules)
		{
			List<Material> materials = new List<Material>();

			int rulesCount = rules.Count;
			for (int i = 0; i < rulesCount; i++)
			{
				var ruleMaterial = rules[i].GetBakeableMaterial();

				if (!materials.Contains(ruleMaterial))
					materials.Add(ruleMaterial);
			}
			return materials;
		}

		private void CheckAndFetchRules()
		{
			if (_fetchedRules == null || _fetchedRules.Length == 0 || !Application.isPlaying)
			{
				_fetchedRules = Resources.LoadAll<ABakeableMaterialRuleDefinition>("");
#if UNITY_EDITOR
				if ((_fetchedRules == null || _fetchedRules.Length == 0) && !Application.isPlaying)
				{
					var guids = UnityEditor.AssetDatabase.FindAssets("t:ABakeableMaterialRuleDefinition");
					if (guids != null && guids.Length > 0)
					{
						_fetchedRules = new ABakeableMaterialRuleDefinition[guids.Length];
						for (int i = 0; i < guids.Length; i++)
							_fetchedRules[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<ABakeableMaterialRuleDefinition>(
								UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
					}
				}
#endif
			}
		}



	}


}
