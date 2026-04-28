// ============================================================================
// PhoneContentGeneratorWindow.cs — Parody Data Generator
// Responsibility: Editor tool to generate parody brands and safe-parody phones
//                 as ScriptableObjects. Updates existing assets to ensure
//                 no duplicates.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using DeviceEmpire.Inventory;

namespace DeviceEmpire.EditorTools
{
    public class PhoneContentGeneratorWindow : EditorWindow
    {
        private const string RootPath = "Assets/_Project/Data";
        private const string BrandsPath = RootPath + "/Brands";
        private const string DevicesPath = RootPath + "/Devices";

        private Vector2 _scrollPos;

        [MenuItem("DeviceEmpire/Tools/Parody Phone Generator")]
        public static void ShowWindow()
        {
            GetWindow<PhoneContentGeneratorWindow>("Phone Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Parody Brand & Phone Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates parody brands and safe-parody phone models as ScriptableObjects. " +
                "Re-running will update existing assets without duplicating.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("1. Generate All Brands", GUILayout.Height(40)))
            {
                GenerateBrands();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("2. Generate All Phones", GUILayout.Height(40)))
            {
                GeneratePhones();
            }

            GUILayout.Space(20);
            GUILayout.Label("Individual Generation (Inspector Style):", EditorStyles.boldLabel);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, "box");
            foreach (var b in _brandDefs)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(b.name, GUILayout.Width(100));

                if (GUILayout.Button("Generate Brand", GUILayout.Width(130)))
                {
                    CreateOrUpdateBrand(b);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"Generated Brand: {b.name}");
                }

                if (GUILayout.Button("Generate Phones", GUILayout.Width(130)))
                {
                    GeneratePhonesForBrand(b.id);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();
        }

        private void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");

            if (!AssetDatabase.IsValidFolder(RootPath))
                AssetDatabase.CreateFolder("Assets/_Project", "Data");

            if (!AssetDatabase.IsValidFolder(BrandsPath))
                AssetDatabase.CreateFolder(RootPath, "Brands");

            if (!AssetDatabase.IsValidFolder(DevicesPath))
                AssetDatabase.CreateFolder(RootPath, "Devices");
        }

        private void GenerateBrands()
        {
            EnsureDirectories();

            foreach (var b in _brandDefs)
            {
                CreateOrUpdateBrand(b);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Generator] All brands generated successfully!");
        }

        private void GeneratePhones()
        {
            EnsureDirectories();

            foreach (var b in _brandDefs)
            {
                GeneratePhonesForBrand(b.id);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Generator] All phones generated successfully!");
        }

        private void GeneratePhonesForBrand(string brandId)
        {
            EnsureDirectories();

            string brandAssetPath = $"{BrandsPath}/Brand_{brandId}.asset";
            var brandAsset = AssetDatabase.LoadAssetAtPath<PhoneBrandData>(brandAssetPath);

            if (brandAsset == null)
            {
                Debug.LogWarning($"[Generator] Cannot generate phones for {brandId}. Generate the brand first!");
                return;
            }

            var phones = _phoneDefs.Where(p => p.brandId == brandId);
            int count = 0;

            foreach (var p in phones)
            {
                CreateOrUpdatePhone(p, brandAsset);
                count++;
            }

            Debug.Log($"[Generator] Generated {count} phones for {brandAsset.BrandName}.");
        }

        private void CreateOrUpdateBrand(BrandDef def)
        {
            string path = $"{BrandsPath}/Brand_{def.id}.asset";
            var brand = AssetDatabase.LoadAssetAtPath<PhoneBrandData>(path);
            bool isNew = false;

            if (brand == null)
            {
                brand = ScriptableObject.CreateInstance<PhoneBrandData>();
                isNew = true;
            }

            brand.BrandName = def.name;
            brand.Tagline = def.tagline;
            brand.Tier = def.tier;
            brand.Popularity = def.popularity;
            brand.TrustRating = def.trust;
            brand.PricePremiumMultiplier = def.pricePremium;
            brand.ResaleValueRetention = def.resaleRetention;

            if (isNew)
            {
                AssetDatabase.CreateAsset(brand, path);
            }
            else
            {
                EditorUtility.SetDirty(brand);
            }
        }

        private void CreateOrUpdatePhone(PhoneDef def, PhoneBrandData brandAsset)
        {
            string safeName = NormalizeAssetFileName(def.modelName);
            string path = $"{DevicesPath}/Phone_{def.brandId}_{safeName}.asset";

            var phone = AssetDatabase.LoadAssetAtPath<DeviceData>(path);
            bool isNew = false;

            if (phone == null)
            {
                phone = ScriptableObject.CreateInstance<DeviceData>();
                isNew = true;
            }

            phone.DeviceName = def.modelName;
            phone.Category = DeviceCategory.Phone;
            phone.ModelYear = def.year;
            phone.Brand = brandAsset;
            phone.BaseWholesalePrice = def.wholesale;
            phone.SuggestedRetailPrice = def.retail;
            phone.DepreciationPerDay = def.wholesale * 0.005f; // Approx 0.5% per day base

            // Condition multipliers
            phone.ConditionMultiplier_New = 1.0f;
            phone.ConditionMultiplier_Good = 0.85f;
            phone.ConditionMultiplier_Fair = 0.65f;
            phone.ConditionMultiplier_Poor = 0.40f;

            // Specs
            if (phone.Specs == null)
                phone.Specs = new PhoneSpecs();

            phone.Specs.RAM_GB = def.ram;
            phone.Specs.Storage_GB = def.storage;
            phone.Specs.BatteryCapacity_mAh = def.battery;
            phone.Specs.MainCamera_MP = def.camera;
            phone.Specs.DisplaySize_inches = def.displaySize;
            phone.Specs.DisplayType = def.displayType;
            phone.Specs.RefreshRate_Hz = def.refreshRate;
            phone.Specs.PerformanceScore = def.perfScore;
            phone.Specs.Has5G = def.has5g;
            phone.Specs.HasNFC = def.hasNfc;
            phone.Specs.BackMaterial = def.backMaterial;

            // Default color variants
            if (phone.AvailableColors == null || phone.AvailableColors.Length == 0)
            {
                phone.AvailableColors = new PhoneColorVariant[]
                {
                    new PhoneColorVariant
                    {
                        ColorName = "Phantom Black",
                        DisplayColor = Color.black,
                        PriceMultiplier = 1.0f
                    },
                    new PhoneColorVariant
                    {
                        ColorName = "Glacier White",
                        DisplayColor = Color.white,
                        PriceMultiplier = 1.0f
                    }
                };
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(phone, path);
            }
            else
            {
                EditorUtility.SetDirty(phone);
            }
        }

        private string NormalizeAssetFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Unnamed";

            string result = input.Trim();

            // Remove/replace characters that are unsafe or awkward for filenames
            result = result.Replace(" ", "_");
            result = result.Replace(".", "_");
            result = result.Replace("/", "_");
            result = result.Replace("\\", "_");
            result = result.Replace(":", "_");
            result = result.Replace("*", "_");
            result = result.Replace("?", "_");
            result = result.Replace("\"", "_");
            result = result.Replace("<", "_");
            result = result.Replace(">", "_");
            result = result.Replace("|", "_");

            return result;
        }

        // ── Data Definitions ───────────────────────────────────────────────────

        private class BrandDef
        {
            public string id;
            public string name;
            public string tagline;
            public BrandTier tier;
            public float popularity;
            public float trust;
            public float pricePremium;
            public float resaleRetention;
        }

        private class PhoneDef
        {
            public string brandId;
            public string modelName;
            public int year;
            public float wholesale;
            public float retail;
            public int ram;
            public int storage;
            public int battery;
            public int camera;
            public float displaySize;
            public DisplayType displayType;
            public int refreshRate;
            public float perfScore;
            public bool has5g;
            public bool hasNfc;
            public BackMaterial backMaterial;
        }

        // ── Parody Brand Database ──────────────────────────────────────────────

        private readonly BrandDef[] _brandDefs = new BrandDef[]
        {
            new BrandDef
            {
                id = "mapple",
                name = "Mapple",
                tagline = "Think Differently.",
                tier = BrandTier.Premium,
                popularity = 0.95f,
                trust = 0.90f,
                pricePremium = 1.4f,
                resaleRetention = 0.85f
            },
            new BrandDef
            {
                id = "tamsung",
                name = "Tamsung",
                tagline = "Do What You Can't.",
                tier = BrandTier.Premium,
                popularity = 0.90f,
                trust = 0.85f,
                pricePremium = 1.2f,
                resaleRetention = 0.75f
            },
            new BrandDef
            {
                id = "moogle",
                name = "Moogle",
                tagline = "Built by Moogle.",
                tier = BrandTier.Premium,
                popularity = 0.70f,
                trust = 0.88f,
                pricePremium = 1.1f,
                resaleRetention = 0.65f
            },
            new BrandDef
            {
                id = "twoplus",
                name = "TwoPlus",
                tagline = "Never Settle.",
                tier = BrandTier.MidRange,
                popularity = 0.60f,
                trust = 0.75f,
                pricePremium = 0.9f,
                resaleRetention = 0.60f
            },
            new BrandDef
            {
                id = "meomi",
                name = "Meomi",
                tagline = "Innovation for Everyone.",
                tier = BrandTier.Budget,
                popularity = 0.85f,
                trust = 0.70f,
                pricePremium = 0.8f,
                resaleRetention = 0.50f
            },
            new BrandDef
            {
                id = "somy",
                name = "Somy",
                tagline = "Be Moved.",
                tier = BrandTier.Premium,
                popularity = 0.40f,
                trust = 0.80f,
                pricePremium = 1.15f,
                resaleRetention = 0.55f
            },
            new BrandDef
            {
                id = "notorola",
                name = "Notorola",
                tagline = "Hello Moto.",
                tier = BrandTier.Budget,
                popularity = 0.65f,
                trust = 0.70f,
                pricePremium = 0.85f,
                resaleRetention = 0.45f
            },
            new BrandDef
            {
                id = "pegasus",
                name = "PegaSus",
                tagline = "In Search of Incredible.",
                tier = BrandTier.MidRange,
                popularity = 0.45f,
                trust = 0.85f,
                pricePremium = 1.0f,
                resaleRetention = 0.60f
            }
        };

        // ── Safe-Parody Phone Database ────────────────────────────────────────
        // Note: model names are intentionally not identical to the real-world names.

        private readonly PhoneDef[] _phoneDefs = new PhoneDef[]
        {
            // Mapple
            new PhoneDef
            {
                brandId = "mapple",
                modelName = "Mapple One 14",
                year = 2022,
                wholesale = 500,
                retail = 799,
                ram = 6,
                storage = 128,
                battery = 3279,
                camera = 12,
                displaySize = 6.1f,
                displayType = DisplayType.OLED,
                refreshRate = 60,
                perfScore = 80,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "mapple",
                modelName = "Mapple One 14 Pro",
                year = 2022,
                wholesale = 650,
                retail = 999,
                ram = 6,
                storage = 128,
                battery = 3200,
                camera = 48,
                displaySize = 6.1f,
                displayType = DisplayType.LTPO_AMOLED,
                refreshRate = 120,
                perfScore = 85,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "mapple",
                modelName = "Mapple One 15 Max",
                year = 2023,
                wholesale = 800,
                retail = 1199,
                ram = 8,
                storage = 256,
                battery = 4422,
                camera = 48,
                displaySize = 6.7f,
                displayType = DisplayType.LTPO_AMOLED,
                refreshRate = 120,
                perfScore = 90,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "mapple",
                modelName = "Mapple One 16 Pro",
                year = 2024,
                wholesale = 750,
                retail = 1099,
                ram = 8,
                storage = 256,
                battery = 3500,
                camera = 48,
                displaySize = 6.1f,
                displayType = DisplayType.LTPO_AMOLED,
                refreshRate = 120,
                perfScore = 95,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },

            // Tamsung
            new PhoneDef
            {
                brandId = "tamsung",
                modelName = "Nebula S21 Ultra",
                year = 2021,
                wholesale = 550,
                retail = 850,
                ram = 12,
                storage = 256,
                battery = 5000,
                camera = 108,
                displaySize = 6.8f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 75,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "tamsung",
                modelName = "Nebula S22",
                year = 2022,
                wholesale = 450,
                retail = 699,
                ram = 8,
                storage = 128,
                battery = 3700,
                camera = 50,
                displaySize = 6.1f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 80,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "tamsung",
                modelName = "Nebula S23 Ultra",
                year = 2023,
                wholesale = 750,
                retail = 1199,
                ram = 12,
                storage = 512,
                battery = 5000,
                camera = 200,
                displaySize = 6.8f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 92,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "tamsung",
                modelName = "Nebula Flex X4",
                year = 2022,
                wholesale = 1000,
                retail = 1499,
                ram = 12,
                storage = 256,
                battery = 4400,
                camera = 50,
                displaySize = 7.6f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 88,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },

            // Moogle
            new PhoneDef
            {
                brandId = "moogle",
                modelName = "Dot 6a",
                year = 2022,
                wholesale = 250,
                retail = 449,
                ram = 6,
                storage = 128,
                battery = 4410,
                camera = 12,
                displaySize = 6.1f,
                displayType = DisplayType.OLED,
                refreshRate = 60,
                perfScore = 65,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Plastic
            },
            new PhoneDef
            {
                brandId = "moogle",
                modelName = "Dot 7 Pro",
                year = 2022,
                wholesale = 500,
                retail = 899,
                ram = 12,
                storage = 128,
                battery = 5000,
                camera = 50,
                displaySize = 6.7f,
                displayType = DisplayType.LTPO_AMOLED,
                refreshRate = 120,
                perfScore = 82,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "moogle",
                modelName = "Dot 8 Pro",
                year = 2023,
                wholesale = 600,
                retail = 999,
                ram = 12,
                storage = 256,
                battery = 5050,
                camera = 50,
                displaySize = 6.7f,
                displayType = DisplayType.LTPO_AMOLED,
                refreshRate = 120,
                perfScore = 88,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },

            // TwoPlus
            new PhoneDef
            {
                brandId = "twoplus",
                modelName = "TwoPlus 10 Pro",
                year = 2022,
                wholesale = 450,
                retail = 799,
                ram = 8,
                storage = 128,
                battery = 5000,
                camera = 48,
                displaySize = 6.7f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 85,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "twoplus",
                modelName = "TwoPlus 11 Core",
                year = 2023,
                wholesale = 500,
                retail = 899,
                ram = 16,
                storage = 256,
                battery = 5000,
                camera = 50,
                displaySize = 6.7f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 90,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "twoplus",
                modelName = "TwoPlus N2",
                year = 2021,
                wholesale = 200,
                retail = 399,
                ram = 8,
                storage = 128,
                battery = 4500,
                camera = 50,
                displaySize = 6.4f,
                displayType = DisplayType.AMOLED,
                refreshRate = 90,
                perfScore = 60,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Plastic
            },

            // Meomi
            new PhoneDef
            {
                brandId = "meomi",
                modelName = "Meomi Note X10",
                year = 2021,
                wholesale = 120,
                retail = 249,
                ram = 4,
                storage = 64,
                battery = 5000,
                camera = 48,
                displaySize = 6.4f,
                displayType = DisplayType.AMOLED,
                refreshRate = 60,
                perfScore = 35,
                has5g = false,
                hasNfc = false,
                backMaterial = BackMaterial.Plastic
            },
            new PhoneDef
            {
                brandId = "meomi",
                modelName = "Meomi 12 Pro",
                year = 2022,
                wholesale = 400,
                retail = 749,
                ram = 8,
                storage = 256,
                battery = 4600,
                camera = 50,
                displaySize = 6.7f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 84,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "meomi",
                modelName = "Poka X4",
                year = 2022,
                wholesale = 180,
                retail = 349,
                ram = 6,
                storage = 128,
                battery = 5000,
                camera = 64,
                displaySize = 6.6f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 55,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Plastic
            },

            // Somy
            new PhoneDef
            {
                brandId = "somy",
                modelName = "Xperion 1 Mark III",
                year = 2021,
                wholesale = 600,
                retail = 1099,
                ram = 12,
                storage = 256,
                battery = 4500,
                camera = 12,
                displaySize = 6.5f,
                displayType = DisplayType.OLED,
                refreshRate = 120,
                perfScore = 82,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "somy",
                modelName = "Xperion 5 Mark IV",
                year = 2022,
                wholesale = 550,
                retail = 999,
                ram = 8,
                storage = 128,
                battery = 5000,
                camera = 12,
                displaySize = 6.1f,
                displayType = DisplayType.OLED,
                refreshRate = 120,
                perfScore = 84,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },

            // Notorola
            new PhoneDef
            {
                brandId = "notorola",
                modelName = "Moti G Power",
                year = 2022,
                wholesale = 100,
                retail = 199,
                ram = 4,
                storage = 64,
                battery = 5000,
                camera = 50,
                displaySize = 6.5f,
                displayType = DisplayType.IPS_LCD,
                refreshRate = 90,
                perfScore = 30,
                has5g = false,
                hasNfc = false,
                backMaterial = BackMaterial.Plastic
            },
            new PhoneDef
            {
                brandId = "notorola",
                modelName = "Moti Edge 30",
                year = 2022,
                wholesale = 300,
                retail = 499,
                ram = 8,
                storage = 128,
                battery = 4020,
                camera = 50,
                displaySize = 6.5f,
                displayType = DisplayType.AMOLED,
                refreshRate = 144,
                perfScore = 75,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Plastic
            },

            // PegaSus
            new PhoneDef
            {
                brandId = "pegasus",
                modelName = "ROGX Phone 6",
                year = 2022,
                wholesale = 650,
                retail = 1099,
                ram = 16,
                storage = 512,
                battery = 6000,
                camera = 50,
                displaySize = 6.7f,
                displayType = DisplayType.AMOLED,
                refreshRate = 165,
                perfScore = 92,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Glass
            },
            new PhoneDef
            {
                brandId = "pegasus",
                modelName = "ZenFone X9",
                year = 2022,
                wholesale = 400,
                retail = 699,
                ram = 8,
                storage = 128,
                battery = 4300,
                camera = 50,
                displaySize = 5.9f,
                displayType = DisplayType.AMOLED,
                refreshRate = 120,
                perfScore = 86,
                has5g = true,
                hasNfc = true,
                backMaterial = BackMaterial.Polycarbonate
            }
        };
    }
}
#endif