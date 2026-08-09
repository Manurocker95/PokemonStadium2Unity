#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VirtualPhenix.PokemonSnap3DS;
using System.Reflection;

namespace VirtualPhenix.PokemonStadium.EditorTools
{
    public sealed class PS3DS_PokemonStadiumModelImporter : EditorWindow
    {
        private const string RomPref = "VP.PokemonStadiumImporter.RomPath";
        private const string OutputPref = "VP.PokemonStadiumImporter.OutputPath";
        private const string ImportModePref = "VP.PokemonStadiumImporter.ImportMode";
        private const string SingleIndexPref = "VP.PokemonStadiumImporter.SingleIndex";
        private const string RangeStartPref = "VP.PokemonStadiumImporter.RangeStart";
        private const string RangeEndPref = "VP.PokemonStadiumImporter.RangeEnd";
        private const string ContentModePref = "VP.PokemonStadiumImporter.ContentModeV2";
        private const string CreatePrefabPref = "VP.PokemonStadiumImporter.CreatePrefab";
        private const string PrefabRendererPref = "VP.PokemonStadiumImporter.PrefabRenderer";
        private const string FlipTexturesYPref = "VP.PokemonStadiumImporter.FlipTexturesY";
        private const string FlipTexturesXPref = "VP.PokemonStadiumImporter.FlipTexturesX";
        private const string MirrorTexturesPref = "VP.PokemonStadiumImporter.MirrorTextures";
        private const string AnimationSystemPref = "VP.PokemonStadiumImporter.AnimationSystem";
        private const string InstantiatePrefabPref = "VP.PokemonStadiumImporter.InstantiatePrefab";
        private const string CombinePartsPref = "VP.PokemonStadiumImporter.CombineParts";
        private const string PokemonNamePref = "VP.PokemonStadiumImporter.PokemonName";
        private const string MaterialShaderPref = "VP.PokemonStadiumImporter.MaterialShader";
        private const string VertexColorsGrayscalePref = "VP.PokemonStadiumImporter.VertexColorsGrayscale";
        private const string ImportVertexColorsPref = "VP.PokemonStadiumImporter.ImportVertexColors";
        private const string VertexColorLuminancePref = "VP.PokemonStadiumImporter.VertexColorLuminance";
        private const string AnimatedVertexColorStrengthPref = "VP.PokemonStadiumImporter.AnimatedVertexColorStrength";
        private const string MaterialAnimationModePref = "VP.PokemonStadiumImporter.MaterialAnimationMode";
        private const string ExportShinyPref = "VP.PokemonStadiumImporter.ExportShiny";
        private const string ApplyShinyMaterialsPref = "VP.PokemonStadiumImporter.ApplyShinyMaterials";
        private const string ExportColorVariantPref = "VP.PokemonStadiumImporter.ExportColorVariant";
        private const string ApplyColorVariantPref = "VP.PokemonStadiumImporter.ApplyColorVariant";
        private const string ColorVariantModePref = "VP.PokemonStadiumImporter.ColorVariantMode";
        private const string VariantTrainerIdPref = "VP.PokemonStadiumImporter.VariantTrainerId";
        private const string VariantTrainerNamePref = "VP.PokemonStadiumImporter.VariantTrainerName";
        private const string VariantNicknamePref = "VP.PokemonStadiumImporter.VariantNickname";
        private const string VariantManualHuePref = "VP.PokemonStadiumImporter.VariantManualHue";
        private const string OverridePreviousVariantPref = "VP.PokemonStadiumImporter.OverridePreviousVariant";
        private const string VerboseVariantsPref = "VP.PokemonStadiumImporter.VerboseVariants";

        private enum ImportMode
        {
            All,
            MainPokemon,
            SpecialModels,
            ByPokemonName,
            SingleIndex,
            IndexRange
        }

        internal enum ContentMode
        {
            MeshesOnly,
            TexturesOnly,
            AnimationsOnly,
            MeshesAndTextures,
            Everything
        }

        internal enum PrefabRendererMode
        {
            SkinnedRenderer,
            StaticRenderer
        }

        internal enum AnimationSystemMode
        {
            Mecanim,
            Legacy
        }

        internal enum MaterialAnimationMode
        {
            ByAnimation,
            ByCallback
        }

        internal enum ColorVariantMode
        {
            Random,
            TrainerData,
            ManualHue
        }

        private string _romPath;
        private string _outputPath = "Assets/PokemonStadium1/Exported/Models";
        private bool _overwrite = true;
        private ImportMode _importMode = ImportMode.All;
        private int _singleIndex;
        private int _rangeStart;
        private int _rangeEnd = 15;
        private int _pokemonSpecies = 1;
        private ContentMode _contentMode = ContentMode.Everything;
        private bool _createPrefab = true;
        private PrefabRendererMode _prefabRendererMode = PrefabRendererMode.SkinnedRenderer;
        private bool _flipTexturesY = true;
        private bool _flipTexturesX = false;
        private bool _mirrorTextures = true;
        private AnimationSystemMode _animationSystemMode = AnimationSystemMode.Mecanim;
        private MaterialAnimationMode _materialAnimationMode = MaterialAnimationMode.ByAnimation;
        private bool _instantiatePrefab;
        private bool _combineParts;
        private Shader _materialShader;
        private bool _vertexColorsAsGrayscale = true;
        private bool _importVertexColors = true;
        private float _vertexColorLuminance = 1.0f;
        private float _animatedVertexColorStrength = 1.0f;
        private bool _exportShiny;
        private bool _applyShinyMaterials;
        private bool _exportColorVariant;
        private bool _applyColorVariant;
        private ColorVariantMode _colorVariantMode = ColorVariantMode.Random;
        private int _variantTrainerId;
        private string _variantTrainerName = string.Empty;
        private string _variantNickname = string.Empty;
        private int _variantManualHue;
        private bool _overridePreviousVariant = true;
        private bool _verboseVariants = false;

        private static readonly string[] TrainerPresetNames =
        {
            "Rojo", "Azul", "Verde", "Brock", "Misty", "Lt. Surge", "Erika",
            "Koga", "Sabrina", "Blaine", "Giovanni", "Chano", "Lorelei",
            "Bruno", "Agatha", "Lance", "Oak", "Jessie", "James"
        };

        private static readonly string[] TrainerPresetNicknames =
        {
            "ACE", "BUDDY", "CHAMP", "LUCKY", "NOVA", "SPARK", "PIXEL",
            "COMET", "FLASH", "STAR", "N64", "STADIUM"
        };

        private Vector2 _scroll;

        [MenuItem("Stadium2Unity/Model Importer")]
        private static void OpenWindow()
        {
            GetWindow<PS3DS_PokemonStadiumModelImporter>(false, "Stadium2Unity Importer", true).minSize = new Vector2(620f, 430f);
        }

        private void OnEnable()
        {
            _romPath = EditorPrefs.GetString(RomPref, string.Empty);
            _outputPath = EditorPrefs.GetString(OutputPref, "Assets/PokemonStadium1/Exported/Models");
            _importMode = (ImportMode)EditorPrefs.GetInt(ImportModePref, (int)ImportMode.All);
            _singleIndex = EditorPrefs.GetInt(SingleIndexPref, 0);
            _rangeStart = EditorPrefs.GetInt(RangeStartPref, 0);
            _rangeEnd = EditorPrefs.GetInt(RangeEndPref, 15);
            _pokemonSpecies = Mathf.Clamp(EditorPrefs.GetInt(PokemonNamePref, 1), 1, 151);
            _contentMode = (ContentMode)EditorPrefs.GetInt(ContentModePref, (int)ContentMode.Everything);
            _createPrefab = EditorPrefs.GetBool(CreatePrefabPref, true);
            _prefabRendererMode = (PrefabRendererMode)EditorPrefs.GetInt(PrefabRendererPref, (int)PrefabRendererMode.SkinnedRenderer);
            _flipTexturesY = EditorPrefs.GetBool(FlipTexturesYPref, true);
            _flipTexturesX = EditorPrefs.GetBool(FlipTexturesXPref, false);
            _mirrorTextures = EditorPrefs.GetBool(MirrorTexturesPref, true);
            _animationSystemMode = (AnimationSystemMode)EditorPrefs.GetInt(AnimationSystemPref, (int)AnimationSystemMode.Mecanim);
            _materialAnimationMode = (MaterialAnimationMode)EditorPrefs.GetInt(MaterialAnimationModePref, (int)MaterialAnimationMode.ByAnimation);
            _instantiatePrefab = EditorPrefs.GetBool(InstantiatePrefabPref, false);
            _combineParts = EditorPrefs.GetBool(CombinePartsPref, false);
            _vertexColorsAsGrayscale = EditorPrefs.GetBool(VertexColorsGrayscalePref, true);
            _importVertexColors = EditorPrefs.GetBool(ImportVertexColorsPref, true);
            _vertexColorLuminance = Mathf.Clamp(EditorPrefs.GetFloat(VertexColorLuminancePref, 0.63f), 0.01f, 1.0f);
            _animatedVertexColorStrength = Mathf.Clamp(EditorPrefs.GetFloat(AnimatedVertexColorStrengthPref, 0.4f), 0.01f, 1.0f);
            _exportShiny = EditorPrefs.GetBool(ExportShinyPref, false);
            _applyShinyMaterials = EditorPrefs.GetBool(ApplyShinyMaterialsPref, false);
            _exportColorVariant = EditorPrefs.GetBool(ExportColorVariantPref, false);
            _applyColorVariant = EditorPrefs.GetBool(ApplyColorVariantPref, false);
            _colorVariantMode = (ColorVariantMode)EditorPrefs.GetInt(ColorVariantModePref, (int)ColorVariantMode.Random);
            _variantTrainerId = Mathf.Clamp(EditorPrefs.GetInt(VariantTrainerIdPref, 0), 0, 65535);
            _variantTrainerName = EditorPrefs.GetString(VariantTrainerNamePref, string.Empty);
            _variantNickname = EditorPrefs.GetString(VariantNicknamePref, string.Empty);
            _variantManualHue = Mathf.Clamp(EditorPrefs.GetInt(VariantManualHuePref, 0), -180, 180);
            _overridePreviousVariant = EditorPrefs.GetBool(OverridePreviousVariantPref, true);
            _verboseVariants = EditorPrefs.GetBool(VerboseVariantsPref, true);

            string shaderName = EditorPrefs.GetString(MaterialShaderPref, "N3DS/N64_StadiumLit");
            _materialShader = Shader.Find(shaderName);
            if (_materialShader == null)
                _materialShader = Shader.Find("N3DS/N64_StadiumLit");
            if (_materialShader == null)
                _materialShader = GetDefaultMaterialShader();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("PS3DS_PokemonStadiumModelImporter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Imports Pokemon Stadium 1 battle models directly from an N64 ROM. The importer creates native Unity meshes, textures, materials, skeletons, animation clips and prefabs. No Python or external converter is used.",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawPathField("ROM path", ref _romPath, false);
            DrawPathField("Output folder", ref _outputPath, true);
            _overwrite = EditorGUILayout.ToggleLeft("Overwrite existing model folders", _overwrite);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Import selection", EditorStyles.boldLabel);
            _importMode = (ImportMode)EditorGUILayout.EnumPopup("Mode", _importMode);

            if (_importMode == ImportMode.MainPokemon)
            {
                EditorGUILayout.HelpBox("Imports models whose internal species ID is between 001 (Bulbasaur) and 151 (Mew), including model variants.", MessageType.None);
            }
            else if (_importMode == ImportMode.SpecialModels)
            {
                EditorGUILayout.HelpBox("Imports special models whose internal species ID is between 152 and 213, including model variants.", MessageType.None);
            }
            else if (_importMode == ImportMode.ByPokemonName)
            {
                string[] pokemonNames = SpeciesNames.GetMainPokemonDisplayNames();
                _pokemonSpecies = EditorGUILayout.Popup("Pokemon", Mathf.Clamp(_pokemonSpecies - 1, 0, pokemonNames.Length - 1), pokemonNames) + 1;
                EditorGUILayout.HelpBox("Imports every archive entry whose internal species ID matches the selected Pokemon.", MessageType.None);
            }
            else if (_importMode == ImportMode.SingleIndex)
            {
                _singleIndex = Mathf.Max(0, EditorGUILayout.IntField("Model index", _singleIndex));
                EditorGUILayout.HelpBox("Imports only the selected archive index, for example index 37.", MessageType.None);
            }
            else if (_importMode == ImportMode.IndexRange)
            {
                _rangeStart = Mathf.Max(0, EditorGUILayout.IntField("First index", _rangeStart));
                _rangeEnd = Mathf.Max(0, EditorGUILayout.IntField("Last index", _rangeEnd));
                EditorGUILayout.HelpBox("The range is inclusive. A range from 0 to 15 imports 16 archive entries.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Imports every entry found in the Pokemon model archive.", MessageType.None);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Import content", EditorStyles.boldLabel);
            _contentMode = (ContentMode)EditorGUILayout.EnumPopup("Content", _contentMode);

            if (_contentMode == ContentMode.MeshesOnly)
                EditorGUILayout.HelpBox("Creates only mesh assets.", MessageType.None);
            else if (_contentMode == ContentMode.TexturesOnly)
                EditorGUILayout.HelpBox("Creates only decoded PNG textures.", MessageType.None);
            else if (_contentMode == ContentMode.AnimationsOnly)
                EditorGUILayout.HelpBox("Creates only legacy AnimationClip assets. No meshes, textures, materials or prefab are created.", MessageType.None);
            else if (_contentMode == ContentMode.MeshesAndTextures)
                EditorGUILayout.HelpBox("Creates mesh assets and decoded PNG textures. Materials are also created when a prefab is requested.", MessageType.None);
            else
                EditorGUILayout.HelpBox("Creates meshes, textures, materials and legacy animation clips.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            _materialShader = (Shader)EditorGUILayout.ObjectField("Material shader", _materialShader, typeof(Shader), false);
            _importVertexColors = EditorGUILayout.ToggleLeft("Import vertex colors", _importVertexColors);
            EditorGUI.BeginDisabledGroup(!_importVertexColors);
            _vertexColorsAsGrayscale = EditorGUILayout.ToggleLeft("Vertex Colors as Grayscale", _vertexColorsAsGrayscale);
            EditorGUI.BeginDisabledGroup(!_vertexColorsAsGrayscale);
            _vertexColorLuminance = EditorGUILayout.Slider("Vertex color strength", _vertexColorLuminance, 0.01f, 1.0f);
            _animatedVertexColorStrength = EditorGUILayout.Slider("Animated material strength", _animatedVertexColorStrength, 0.01f, 1.0f);
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox(
                "Defaults to N3DS/N64_StadiumLit when available. The first strength controls normal parts. Animated material strength controls texture-animated parts such as eyes. Both preserve the original alpha.",
                MessageType.None);
            _exportShiny = EditorGUILayout.ToggleLeft("Export shiny", _exportShiny);
            EditorGUI.BeginDisabledGroup(!_exportShiny);
            _applyShinyMaterials = EditorGUILayout.ToggleLeft("Apply shiny materials", _applyShinyMaterials);
            EditorGUI.EndDisabledGroup();
            if (!_exportShiny)
                _applyShinyMaterials = false;
            EditorGUILayout.HelpBox(
                "Exports the Stadium shiny recolor for species 001-151. Most species use Stadium's HSL hue/saturation/lightness slide; Clefairy, Clefable, Jigglypuff, Wigglytuff and Gyarados use exact color LUTs. Apply shiny materials makes the generated prefab and material animations reference the shiny assets.",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stadium color variant", EditorStyles.boldLabel);
            _exportColorVariant = EditorGUILayout.ToggleLeft("Export color variant", _exportColorVariant);
            EditorGUI.BeginDisabledGroup(!_exportColorVariant);
            _overridePreviousVariant = EditorGUILayout.ToggleLeft("Override previous variant", _overridePreviousVariant);
            _verboseVariants = EditorGUILayout.ToggleLeft("Verbose Variants offsets", _verboseVariants);
            _applyColorVariant = EditorGUILayout.ToggleLeft("Apply color variant materials", _applyColorVariant);
            EditorGUILayout.BeginHorizontal();
            _colorVariantMode = (ColorVariantMode)EditorGUILayout.EnumPopup("Variant mode", _colorVariantMode);
            if (GUILayout.Button("Randomize Trainer Data", GUILayout.Width(175f)))
                RandomizeTrainerData();
            EditorGUILayout.EndHorizontal();
            if (_colorVariantMode == ColorVariantMode.TrainerData)
            {
                _variantTrainerId = Mathf.Clamp(EditorGUILayout.IntField("Trainer ID", _variantTrainerId), 0, 65535);
                _variantTrainerName = EditorGUILayout.TextField("Trainer name", _variantTrainerName);
                _variantNickname = EditorGUILayout.TextField("Pokemon nickname", _variantNickname);
            }
            else if (_colorVariantMode == ColorVariantMode.ManualHue)
            {
                _variantManualHue = EditorGUILayout.IntSlider("Manual hue", _variantManualHue, -180, 180);
                EditorGUILayout.HelpBox("Applies the selected hue shift directly. This mode is not limited to the species Stadium nickname hue range, so it can also be used for custom color experiments.", MessageType.None);
            }
            EditorGUI.EndDisabledGroup();
            if (!_exportColorVariant)
                _applyColorVariant = false;
            if (_applyColorVariant)
                _applyShinyMaterials = false;
            EditorGUILayout.HelpBox(
                "Random chooses one hue inside the species' real Stadium hue range for each imported model. Trainer Data reproduces Stadium's deterministic nickname coloration from Trainer ID + Trainer name + Pokemon nickname. Manual Hue applies a direct -180..180 degree hue shift. Color variant materials take precedence over shiny materials when applied.",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation system", EditorStyles.boldLabel);
            _animationSystemMode = (AnimationSystemMode)EditorGUILayout.EnumPopup("Animation type", _animationSystemMode);
            _materialAnimationMode = (MaterialAnimationMode)EditorGUILayout.EnumPopup("Material animation", _materialAnimationMode);

            if (_animationSystemMode == AnimationSystemMode.Mecanim)
                EditorGUILayout.HelpBox("Creates non-legacy clips and, for a skinned Everything prefab, an Animator Controller with one state per imported clip.", MessageType.None);
            else
                EditorGUILayout.HelpBox("Creates legacy clips and, for a skinned Everything prefab, an Animation component containing all imported clips.", MessageType.None);

            if (_materialAnimationMode == MaterialAnimationMode.ByAnimation)
                EditorGUILayout.HelpBox("Stores material changes directly as object-reference keys in each AnimationClip.", MessageType.None);
            else
                EditorGUILayout.HelpBox("Creates AnimationEvents that call PS3DS_TextureSwapper.SwapSet(float). The prefab receives all generated texture swap sets.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);
            bool prefabSupported = _contentMode == ContentMode.MeshesOnly ||
                                   _contentMode == ContentMode.MeshesAndTextures ||
                                   _contentMode == ContentMode.Everything;
            EditorGUI.BeginDisabledGroup(!prefabSupported);
            _createPrefab = EditorGUILayout.ToggleLeft("Create prefab", _createPrefab);
            EditorGUI.BeginDisabledGroup(!_createPrefab);
            _prefabRendererMode = (PrefabRendererMode)EditorGUILayout.EnumPopup("Prefab is", _prefabRendererMode);
            _instantiatePrefab = EditorGUILayout.ToggleLeft("Instantiate prefab in the current scene", _instantiatePrefab);
            _combineParts = EditorGUILayout.ToggleLeft("Combine parts into one", _combineParts);
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();

            if (!prefabSupported)
                EditorGUILayout.HelpBox("This content mode does not create meshes, so prefab creation is unavailable.", MessageType.None);
            else if (_createPrefab && _prefabRendererMode == PrefabRendererMode.StaticRenderer)
                EditorGUILayout.HelpBox("Creates MeshFilter and MeshRenderer components. Skinning and animation are not applied, so the original decoded vertex positions are preserved.", MessageType.None);
            else if (_createPrefab)
                EditorGUILayout.HelpBox("Creates a bone hierarchy and SkinnedMeshRenderer components. Animation clips are attached only when importing Everything.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
            _flipTexturesY = EditorGUILayout.ToggleLeft("Flip textures vertically (Y)", _flipTexturesY);
            _flipTexturesX = EditorGUILayout.ToggleLeft("Flip textures horizontally (X)", _flipTexturesX);
            _mirrorTextures = EditorGUILayout.ToggleLeft("Mirror textures when required by the model", _mirrorTextures);
            EditorGUILayout.HelpBox(
                "Normal textures remain clamped. When mirror is enabled, G_SETTILE mirror flags create native per-axis mirror on Unity 2017.1+ or baked mirror variants on Unity 5.6.",
                MessageType.None);
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_romPath) || string.IsNullOrEmpty(_outputPath));
            if (GUILayout.Button(GetImportButtonLabel(), GUILayout.Height(38f)))
            {
                ImportSelection();
            }

            EditorGUI.BeginDisabledGroup(!_exportColorVariant);
            if (GUILayout.Button("Import Variant", GUILayout.Height(32f)))
            {
                ImportVariantOnly();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox(
                "Import Variant adds only a new numbered color variant to an already imported model. It never overwrites or recreates the normal prefab, normal textures, shiny assets, meshes or base animations.",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Expected ROM: Pokemon Stadium (USA). Other revisions may use different archive offsets and are rejected unless their layout still matches.",
                MessageType.Warning);
            EditorGUILayout.EndScrollView();
        }

        private void RandomizeTrainerData()
        {
            _colorVariantMode = ColorVariantMode.TrainerData;
            _variantTrainerName = TrainerPresetNames[UnityEngine.Random.Range(0, TrainerPresetNames.Length)];
            _variantTrainerId = UnityEngine.Random.Range(0, 65536);

            if (_importMode == ImportMode.ByPokemonName && _pokemonSpecies >= 1 && _pokemonSpecies <= 151)
                _variantNickname = SpeciesNames.Get(_pokemonSpecies).ToUpperInvariant();
            else
                _variantNickname = TrainerPresetNicknames[UnityEngine.Random.Range(0, TrainerPresetNicknames.Length)];

            Repaint();
        }

        private static void DrawPathField(string label, ref string value, bool folder)
        {
            EditorGUILayout.BeginHorizontal();
            value = EditorGUILayout.TextField(label, value);
            if (GUILayout.Button("Browse...", GUILayout.Width(90f)))
            {
                string selected = folder
                    ? EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty)
                    : EditorUtility.OpenFilePanel(label, string.IsNullOrEmpty(value) ? string.Empty : Path.GetDirectoryName(value), "z64,n64,v64");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (folder && selected.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/') + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        value = "Assets" + selected.Substring(Application.dataPath.Length).Replace('\\', '/');
                    }
                    else
                    {
                        value = selected.Replace('\\', '/');
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private string GetImportButtonLabel()
        {
            if (_importMode == ImportMode.MainPokemon)
                return "Import main Pokemon (001-151)";
            if (_importMode == ImportMode.SpecialModels)
                return "Import special models (152-213)";
            if (_importMode == ImportMode.ByPokemonName)
                return "Import " + SpeciesNames.Get(_pokemonSpecies);
            if (_importMode == ImportMode.SingleIndex)
                return "Import model index " + _singleIndex;
            if (_importMode == ImportMode.IndexRange)
                return "Import models " + _rangeStart + " to " + _rangeEnd;
            return "Import all models";
        }

        private void ImportSelection()
        {
            EditorPrefs.SetString(RomPref, _romPath);
            EditorPrefs.SetString(OutputPref, _outputPath);
            EditorPrefs.SetInt(ImportModePref, (int)_importMode);
            EditorPrefs.SetInt(SingleIndexPref, _singleIndex);
            EditorPrefs.SetInt(RangeStartPref, _rangeStart);
            EditorPrefs.SetInt(RangeEndPref, _rangeEnd);
            EditorPrefs.SetInt(PokemonNamePref, _pokemonSpecies);
            EditorPrefs.SetInt(ContentModePref, (int)_contentMode);
            EditorPrefs.SetBool(CreatePrefabPref, _createPrefab);
            EditorPrefs.SetInt(PrefabRendererPref, (int)_prefabRendererMode);
            EditorPrefs.SetBool(FlipTexturesYPref, _flipTexturesY);
            EditorPrefs.SetBool(FlipTexturesXPref, _flipTexturesX);
            EditorPrefs.SetBool(MirrorTexturesPref, _mirrorTextures);
            EditorPrefs.SetInt(AnimationSystemPref, (int)_animationSystemMode);
            EditorPrefs.SetInt(MaterialAnimationModePref, (int)_materialAnimationMode);
            EditorPrefs.SetBool(InstantiatePrefabPref, _instantiatePrefab);
            EditorPrefs.SetBool(CombinePartsPref, _combineParts);
            EditorPrefs.SetString(MaterialShaderPref, _materialShader != null ? _materialShader.name : string.Empty);
            EditorPrefs.SetBool(VertexColorsGrayscalePref, _vertexColorsAsGrayscale);
            EditorPrefs.SetBool(ImportVertexColorsPref, _importVertexColors);
            EditorPrefs.SetFloat(VertexColorLuminancePref, _vertexColorLuminance);
            EditorPrefs.SetFloat(AnimatedVertexColorStrengthPref, _animatedVertexColorStrength);
            EditorPrefs.SetBool(ExportShinyPref, _exportShiny);
            EditorPrefs.SetBool(ApplyShinyMaterialsPref, _applyShinyMaterials);
            EditorPrefs.SetBool(ExportColorVariantPref, _exportColorVariant);
            EditorPrefs.SetBool(ApplyColorVariantPref, _applyColorVariant);
            EditorPrefs.SetInt(ColorVariantModePref, (int)_colorVariantMode);
            EditorPrefs.SetInt(VariantTrainerIdPref, _variantTrainerId);
            EditorPrefs.SetString(VariantTrainerNamePref, _variantTrainerName ?? string.Empty);
            EditorPrefs.SetString(VariantNicknamePref, _variantNickname ?? string.Empty);
            EditorPrefs.SetInt(VariantManualHuePref, _variantManualHue);
            EditorPrefs.SetBool(OverridePreviousVariantPref, _overridePreviousVariant);
            EditorPrefs.SetBool(VerboseVariantsPref, _verboseVariants);

            try
            {
                if (!_outputPath.Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal) && _outputPath != "Assets")
                    throw new InvalidOperationException("The output folder must be inside Assets.");
                if (!File.Exists(_romPath))
                    throw new FileNotFoundException("ROM file not found.", _romPath);
                if (_importMode == ImportMode.IndexRange && _rangeStart > _rangeEnd)
                    throw new InvalidOperationException("The first range index cannot be greater than the last index.");

                PokemonStadiumRom rom = new PokemonStadiumRom(_romPath);
                List<byte[]> files = rom.ReadPokemonModelArchive();
                EnsureAssetFolder(_outputPath);

                int firstIndex = 0;
                int lastIndex = files.Count - 1;

                if (_importMode == ImportMode.SingleIndex)
                {
                    firstIndex = _singleIndex;
                    lastIndex = _singleIndex;
                }
                else if (_importMode == ImportMode.IndexRange)
                {
                    firstIndex = _rangeStart;
                    lastIndex = _rangeEnd;
                }

                if (firstIndex < 0 || firstIndex >= files.Count ||
                    lastIndex < 0 || lastIndex >= files.Count)
                {
                    EditorUtility.DisplayDialog(
                        "Stadium2Unity Importer",
                        "The selected index or range is outside the archive. Valid indices are 0 to " + (files.Count - 1) + ".",
                        "OK");
                    return;
                }

                int selectedCount = lastIndex - firstIndex + 1;
                int imported = 0;
                int skipped = 0;
                bool cancelled = false;

                for (int i = firstIndex; i <= lastIndex; i++)
                {
                    int selectionOffset = i - firstIndex;
                    float progress = selectedCount == 0 ? 1f : (float)selectionOffset / selectedCount;
                    if (EditorUtility.DisplayCancelableProgressBar("Pokemon Stadium model import", "Parsing archive file " + i + " (" + (selectionOffset + 1) + "/" + selectedCount + ")", progress))
                    {
                        cancelled = true;
                        break;
                    }

                    try
                    {
                        FragmentModel model = FragmentParser.Parse(files[i], i);
                        rom.AssignPreferredAuxAnimations(model);
                        if (model == null || model.Primitives.Count == 0)
                        {
                            skipped++;
                            continue;
                        }

                        if (!MatchesSpeciesPreset(model.Species))
                            continue;

                        UnityModelWriter.Write(
                            model,
                            _outputPath,
                            i,
                            _overwrite,
                            _contentMode,
                            _createPrefab,
                            _prefabRendererMode,
                            false,
                            _flipTexturesY,
                            _flipTexturesX,
                            _mirrorTextures,
                            _animationSystemMode,
                            _materialAnimationMode,
                            _instantiatePrefab,
                            _combineParts,
                            _materialShader,
                            _vertexColorsAsGrayscale,
                            _importVertexColors,
                            _vertexColorLuminance,
                            _animatedVertexColorStrength,
                            _exportShiny,
                            _applyShinyMaterials,
                            _exportColorVariant,
                            _applyColorVariant,
                            _colorVariantMode,
                            _variantTrainerId,
                            _variantTrainerName,
                            _variantNickname,
                            _variantManualHue,
                            _overridePreviousVariant,
                            _verboseVariants);
                        imported++;
                    }
                    catch (Exception ex)
                    {
                        skipped++;
                        Debug.LogWarning("[VP Pokemon Stadium Importer] File " + i + " skipped: " + ex.Message);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string result = "Imported " + imported + " model files.";
                if (skipped > 0)
                    result += "\nSkipped " + skipped + " entries.";
                if (cancelled)
                    result += "\nThe operation was cancelled before completing the selection.";
                result += "\nArchive contains " + files.Count + " entries (indices 0 to " + (files.Count - 1) + ").";

                EditorUtility.DisplayDialog("Stadium2Unity Importer", result, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Stadium2Unity Importer", ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ImportVariantOnly()
        {
            EditorPrefs.SetString(RomPref, _romPath);
            EditorPrefs.SetString(OutputPref, _outputPath);
            EditorPrefs.SetInt(ImportModePref, (int)_importMode);
            EditorPrefs.SetInt(SingleIndexPref, _singleIndex);
            EditorPrefs.SetInt(RangeStartPref, _rangeStart);
            EditorPrefs.SetInt(RangeEndPref, _rangeEnd);
            EditorPrefs.SetInt(PokemonNamePref, _pokemonSpecies);
            EditorPrefs.SetBool(InstantiatePrefabPref, _instantiatePrefab);
            EditorPrefs.SetInt(AnimationSystemPref, (int)_animationSystemMode);
            EditorPrefs.SetInt(MaterialAnimationModePref, (int)_materialAnimationMode);
            EditorPrefs.SetBool(FlipTexturesYPref, _flipTexturesY);
            EditorPrefs.SetBool(FlipTexturesXPref, _flipTexturesX);
            EditorPrefs.SetBool(MirrorTexturesPref, _mirrorTextures);
            EditorPrefs.SetString(MaterialShaderPref, _materialShader != null ? _materialShader.name : string.Empty);
            EditorPrefs.SetBool(ExportColorVariantPref, _exportColorVariant);
            EditorPrefs.SetBool(ApplyColorVariantPref, _applyColorVariant);
            EditorPrefs.SetInt(ColorVariantModePref, (int)_colorVariantMode);
            EditorPrefs.SetInt(VariantTrainerIdPref, _variantTrainerId);
            EditorPrefs.SetString(VariantTrainerNamePref, _variantTrainerName ?? string.Empty);
            EditorPrefs.SetString(VariantNicknamePref, _variantNickname ?? string.Empty);
            EditorPrefs.SetInt(VariantManualHuePref, _variantManualHue);
            EditorPrefs.SetBool(VerboseVariantsPref, _verboseVariants);

            try
            {
                if (!_outputPath.Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal) && _outputPath != "Assets")
                    throw new InvalidOperationException("The output folder must be inside Assets.");
                if (!File.Exists(_romPath))
                    throw new FileNotFoundException("ROM file not found.", _romPath);
                if (_importMode == ImportMode.IndexRange && _rangeStart > _rangeEnd)
                    throw new InvalidOperationException("The first range index cannot be greater than the last index.");

                PokemonStadiumRom rom = new PokemonStadiumRom(_romPath);
                List<byte[]> files = rom.ReadPokemonModelArchive();

                int firstIndex = 0;
                int lastIndex = files.Count - 1;
                if (_importMode == ImportMode.SingleIndex)
                {
                    firstIndex = _singleIndex;
                    lastIndex = _singleIndex;
                }
                else if (_importMode == ImportMode.IndexRange)
                {
                    firstIndex = _rangeStart;
                    lastIndex = _rangeEnd;
                }

                if (firstIndex < 0 || firstIndex >= files.Count || lastIndex < 0 || lastIndex >= files.Count)
                {
                    EditorUtility.DisplayDialog(
                        "Stadium2Unity Importer",
                        "The selected index or range is outside the archive. Valid indices are 0 to " + (files.Count - 1) + ".",
                        "OK");
                    return;
                }

                int selectedCount = lastIndex - firstIndex + 1;
                int imported = 0;
                int missingBase = 0;
                int skipped = 0;
                bool cancelled = false;

                for (int i = firstIndex; i <= lastIndex; i++)
                {
                    int selectionOffset = i - firstIndex;
                    float progress = selectedCount == 0 ? 1f : (float)selectionOffset / selectedCount;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Pokemon Stadium variant import",
                        "Creating variant for archive file " + i + " (" + (selectionOffset + 1) + "/" + selectedCount + ")",
                        progress))
                    {
                        cancelled = true;
                        break;
                    }

                    try
                    {
                        FragmentModel model = FragmentParser.Parse(files[i], i);
                        rom.AssignPreferredAuxAnimations(model);
                        if (model == null || model.Primitives.Count == 0)
                        {
                            skipped++;
                            continue;
                        }
                        if (!MatchesSpeciesPreset(model.Species))
                            continue;

                        if (UnityModelWriter.ImportVariantOnly(
                            model,
                            _outputPath,
                            i,
                            _flipTexturesY,
                            _flipTexturesX,
                            _mirrorTextures,
                            _materialShader,
                            _colorVariantMode,
                            _variantTrainerId,
                            _variantTrainerName,
                            _variantNickname,
                            _variantManualHue,
                            _animationSystemMode,
                            _materialAnimationMode,
                            _instantiatePrefab,
                            _applyColorVariant,
                            _verboseVariants))
                        {
                            imported++;
                        }
                        else
                        {
                            missingBase++;
                            Debug.LogWarning("[Stadium2Unity Variant] Base normal prefab was not found for " + model.Species.ToString("000") + " " + model.Name + ". Import the model normally once before using Import Variant.");
                        }
                    }
                    catch (Exception ex)
                    {
                        skipped++;
                        Debug.LogWarning("[VP Pokemon Stadium Importer] Variant for file " + i + " skipped: " + ex.Message);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string result = "Created " + imported + " new variant(s).";
                if (missingBase > 0)
                    result += "\n" + missingBase + " model(s) had no existing normal prefab.";
                if (skipped > 0)
                    result += "\nSkipped " + skipped + " entries.";
                if (cancelled)
                    result += "\nThe operation was cancelled before completing the selection.";
                EditorUtility.DisplayDialog("Stadium2Unity Import Variant", result, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Stadium2Unity Import Variant", ex.Message, "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private bool MatchesSpeciesPreset(int species)
        {
            if (_importMode == ImportMode.MainPokemon)
                return species >= 1 && species <= 151;
            if (_importMode == ImportMode.SpecialModels)
                return species >= 152 && species <= 213;
            if (_importMode == ImportMode.ByPokemonName)
                return species == _pokemonSpecies;
            return true;
        }

        private static Shader GetDefaultMaterialShader()
        {
#if UNITY_2017_1_OR_NEWER
            Shader shader = Shader.Find("Standard");
#else
            Shader shader = Shader.Find("Legacy Shaders/VertexLit");
#endif
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");
            return shader;
        }

        internal static void EnsureAssetFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path))
                return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    internal sealed class PokemonStadiumRom
    {
        private const int PokemonModelsOffset = 0x920000;
        private const int BattleDataOffset = 0x70D3A0;
        private const int MainRomOffset = 0x1000;
        private const int MainVram = unchecked((int)0x80000400);
        private const int PointerTableVram = unchecked((int)0x80075BD0);
        private const int BattleTableStride = 0xB90;
        private const int BattleEntrySize = 0x10;
        private const string ExpectedMd5 = "ed1378bc12115f71209a77844965ba50";
        private readonly byte[] _data;

        public PokemonStadiumRom(string path)
        {
            _data = File.ReadAllBytes(path);
            if (_data.Length < 4)
                throw new InvalidDataException("The selected file is too small to be an N64 ROM.");
            uint magic = BigEndian.U32(_data, 0);
            if (magic == 0x37804012)
            {
                for (int i = 0; i + 1 < _data.Length; i += 2)
                {
                    byte value = _data[i];
                    _data[i] = _data[i + 1];
                    _data[i + 1] = value;
                }
            }
            else if (magic == 0x40123780)
            {
                for (int i = 0; i + 3 < _data.Length; i += 4)
                {
                    byte a = _data[i];
                    byte b = _data[i + 1];
                    _data[i] = _data[i + 3];
                    _data[i + 1] = _data[i + 2];
                    _data[i + 2] = b;
                    _data[i + 3] = a;
                }
            }
            else if (magic != 0x80371240)
            {
                throw new InvalidDataException("The selected file is not a supported .z64, .n64 or .v64 ROM.");
            }

            string md5;
            using (MD5 hash = MD5.Create())
            {
                byte[] digest = hash.ComputeHash(_data);
                md5 = BitConverter.ToString(digest).Replace("-", string.Empty).ToLowerInvariant();
            }
            if (md5 != ExpectedMd5)
                Debug.LogWarning("[VP Pokemon Stadium Importer] ROM MD5 differs from the expected USA revision: " + md5);
        }

        public List<byte[]> ReadPokemonModelArchive()
        {
            if (_data.Length <= PokemonModelsOffset + 16)
                throw new InvalidDataException("The ROM does not contain the expected model archive offset.");
            if ((BigEndian.U32(_data, PokemonModelsOffset) & 0xFFFFFF00u) != 0u || BigEndian.U32(_data, PokemonModelsOffset + 4) != 0u)
                throw new InvalidDataException("The model archive header was not found. This ROM revision is not supported.");

            int count = checked((int)BigEndian.U32(_data, PokemonModelsOffset + 12));
            if (count <= 0 || count >= 4096)
                throw new InvalidDataException("Invalid model archive entry count: " + count);

            List<byte[]> result = new List<byte[]>(count);
            for (int i = 0; i < count; i++)
            {
                int record = PokemonModelsOffset + 0x10 + i * 0x10;
                int start = checked((int)BigEndian.U32(_data, record));
                int size = checked((int)BigEndian.U32(_data, record + 4));
                if (start < 0 || size < 0 || PokemonModelsOffset + start + size > _data.Length)
                    throw new InvalidDataException("Invalid archive entry " + i + ".");
                byte[] blob = new byte[size];
                Buffer.BlockCopy(_data, PokemonModelsOffset + start, blob, 0, size);
                result.Add(Decompress(blob));
            }
            return result;
        }


        public void AssignPreferredAuxAnimations(FragmentModel model)
        {
            if (model == null || model.Species < 1 || model.Species > 151 || model.Animations.Count == 0)
                return;

            int pointerTable = MainRomOffset + (PointerTableVram - MainVram);
            int pointerOffset = pointerTable + (model.Species - 1) * 4;
            if (pointerOffset < 0 || pointerOffset + 4 > _data.Length)
                return;

            int relative = (int)(BigEndian.U32(_data, pointerOffset) & 0x00FFFFFFu);
            int table = BattleDataOffset + relative;
            if (table < 0 || table + BattleTableStride > _data.Length)
                return;

            Dictionary<int, Dictionary<int, int>> counts = new Dictionary<int, Dictionary<int, int>>();
            int entries = BattleTableStride / BattleEntrySize;
            for (int i = 0; i < entries; i++)
            {
                int offset = table + i * BattleEntrySize;
                int animationIndex = _data[offset];
                int auxIndex = _data[offset + 1] == 0xFF ? -1 : _data[offset + 1];
                if (animationIndex < 0 || animationIndex >= model.Animations.Count ||
                    auxIndex < 0 || auxIndex >= model.AuxAnimations.Count)
                    continue;

                Dictionary<int, int> auxCounts;
                if (!counts.TryGetValue(animationIndex, out auxCounts))
                {
                    auxCounts = new Dictionary<int, int>();
                    counts.Add(animationIndex, auxCounts);
                }

                int value;
                auxCounts.TryGetValue(auxIndex, out value);
                auxCounts[auxIndex] = value + 1;
            }

            foreach (KeyValuePair<int, Dictionary<int, int>> pair in counts)
            {
                int bestAux = -1;
                int bestCount = -1;
                foreach (KeyValuePair<int, int> candidate in pair.Value)
                {
                    if (candidate.Value > bestCount)
                    {
                        bestAux = candidate.Key;
                        bestCount = candidate.Value;
                    }
                }
                model.Animations[pair.Key].AuxAnimation = bestAux;
            }
        }
        private static byte[] Decompress(byte[] blob)
        {
            if (blob.Length >= 12 && Match(blob, 0, "PERS-SZP"))
            {
                int header = checked((int)BigEndian.U32(blob, 8));
                byte[] yay = new byte[blob.Length - header];
                Buffer.BlockCopy(blob, header, yay, 0, yay.Length);
                return Yay0(yay);
            }
            if (blob.Length >= 16 && Match(blob, 0, "Yay0"))
                return Yay0(blob);
            return blob;
        }

        private static bool Match(byte[] data, int offset, string value)
        {
            if (offset + value.Length > data.Length)
                return false;
            for (int i = 0; i < value.Length; i++)
                if (data[offset + i] != (byte)value[i])
                    return false;
            return true;
        }

        private static byte[] Yay0(byte[] source)
        {
            if (!Match(source, 0, "Yay0"))
                throw new InvalidDataException("Invalid Yay0 stream.");
            int outputSize = checked((int)BigEndian.U32(source, 4));
            int linkOffset = checked((int)BigEndian.U32(source, 8));
            int chunkOffset = checked((int)BigEndian.U32(source, 12));
            byte[] output = new byte[outputSize];
            int maskOffset = 0x10;
            int outputPosition = 0;
            uint mask = 0;
            int bits = 0;
            while (outputPosition < outputSize)
            {
                if (bits == 0)
                {
                    mask = BigEndian.U32(source, maskOffset);
                    maskOffset += 4;
                    bits = 32;
                }
                if ((mask & 0x80000000u) != 0u)
                {
                    output[outputPosition++] = source[chunkOffset++];
                }
                else
                {
                    ushort link = BigEndian.U16(source, linkOffset);
                    linkOffset += 2;
                    int distance = link & 0x0FFF;
                    int count = link >> 12;
                    if (count == 0)
                        count = source[chunkOffset++] + 0x12;
                    else
                        count += 2;
                    int copy = outputPosition - distance - 1;
                    for (int i = 0; i < count && outputPosition < outputSize; i++)
                        output[outputPosition++] = output[copy++];
                }
                mask <<= 1;
                bits--;
            }
            return output;
        }
    }

    internal static class BigEndian
    {
        public static ushort U16(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }
        public static short S16(byte[] data, int offset)
        {
            return unchecked((short)U16(data, offset));
        }
        public static uint U32(byte[] data, int offset)
        {
            return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
        }
        public static int S32(byte[] data, int offset)
        {
            return unchecked((int)U32(data, offset));
        }
    }

    internal sealed class FragmentReader
    {
        public const int BaseAddress = unchecked((int)0x8FF00000);
        public readonly byte[] Data;
        public FragmentReader(byte[] data)
        {
            Data = data;
            if (data.Length < 0x20 || System.Text.Encoding.ASCII.GetString(data, 8, 8) != "FRAGMENT")
                throw new InvalidDataException("Not a Pokemon Stadium FRAGMENT module.");
        }
        public byte U8(int o) { return Data[o]; }
        public sbyte S8(int o) { return unchecked((sbyte)Data[o]); }
        public ushort U16(int o) { return BigEndian.U16(Data, o); }
        public short S16(int o) { return BigEndian.S16(Data, o); }
        public uint U32(int o) { return BigEndian.U32(Data, o); }
        public int S32(int o) { return BigEndian.S32(Data, o); }
        public int Ptr(int o)
        {
            uint p = U32(o);
            return p == 0 ? -1 : unchecked((int)p) - BaseAddress;
        }
        public int Root()
        {
            for (int o = 0x20; o < 0x80; o += 4)
            {
                uint w = U32(o);
                if ((w >> 26) != 0x0F) continue;
                int register = (int)((w >> 16) & 0x1F);
                uint w2 = U32(o + 4);
                if ((w2 >> 26) == 0x09 && ((w2 >> 21) & 0x1F) == register)
                    return unchecked(((int)U16(o + 2) << 16) + S16(o + 6) - BaseAddress);
            }
            throw new InvalidDataException("Could not locate the model root structure.");
        }
        public List<int> PtrList(int offset)
        {
            List<int> list = new List<int>();
            while (offset >= 0 && offset + 4 <= Data.Length)
            {
                int pointer = Ptr(offset);
                if (pointer < 0) break;
                list.Add(pointer);
                offset += 4;
            }
            return list;
        }
    }

    internal sealed class BoneData
    {
        public int Parent;
        public int BoneId;
        public int Channel;
        public byte Flags;
        public Vector3 Translation;
        public Vector3 RotationUnits;
        public Vector3 Scale;
    }

    internal sealed class TextureRecord
    {
        public int Format, Size, Width, Height, DataOffset;
    }

    internal sealed class TlutRecord
    {
        public int Count, DataOffset, DisplayList;
    }

    internal sealed class VertexData
    {
        public Vector3 Position;
        public Vector2 UVRaw;
        public Vector3 Normal;
        public Color32 Color;
        public int Bone;
    }

    internal sealed class PrimitiveData
    {
        public int Texture = -1;
        public int Tlut = -1;
        public int MaterialDisplayList = -1;
        public int TextureAnimation = -1;
        public int Cull;
        public int Palette;
        public bool MirrorS;
        public bool MirrorT;
        public bool ClampS;
        public bool ClampT;
        public readonly List<VertexData> Vertices = new List<VertexData>();
        public readonly List<int> Indices = new List<int>();
        public readonly Dictionary<string, int> Remap = new Dictionary<string, int>();
    }

    internal sealed class TileState
    {
        public int Palette;
        public bool MirrorS;
        public bool MirrorT;
        public bool ClampS;
        public bool ClampT;
    }

    internal sealed class TrackData
    {
        public Vector3[] Translation;
        public Vector3[] RotationUnits;
        public Vector3[] Scale;
    }

    internal sealed class AuxAnimationData
    {
        public int Index;
        public int FrameCount;
        public int LoopStart;
        public byte Flags;
        public int[][] Channels;
    }

    internal sealed class AnimationData
    {
        public int Index;
        public int AuxAnimation = -1;
        public int FrameCount;
        public int LoopStart;
        public TrackData[] Tracks;
    }

    internal sealed class DecodedTexture
    {
        public int Width, Height;
        public Color32[] Pixels;
    }

    internal sealed class FragmentModel
    {
        public int Species;
        public string Name;
        public Vector3 RootScale = Vector3.one;
        public readonly List<BoneData> Bones = new List<BoneData>();
        public readonly List<TextureRecord> Textures = new List<TextureRecord>();
        public readonly List<TlutRecord> Tluts = new List<TlutRecord>();
        public readonly List<PrimitiveData> Primitives = new List<PrimitiveData>();
        public readonly List<AnimationData> Animations = new List<AnimationData>();
        public readonly List<AuxAnimationData> AuxAnimations = new List<AuxAnimationData>();
        public FragmentReader Reader;
    }

    internal static class FragmentParser
    {
        private static readonly int[] CommandSizes =
        {
            0x08,0x04,0x08,0x08,0x04,0x04,0x04,0x08,0x0C,0x04,0x08,0x18,0x04,0x04,0x04,0x04,
            0x04,0x04,0x04,0x08,0x0C,0x0C,0x04,0x14,0x08,0x08,0x04,0x10,0x10,0x1C,0x08,0x18,
            0x14,0x10,0x08,0x10,0x04,0x04,0x14
        };

        public static FragmentModel Parse(byte[] data, int fileIndex)
        {
            FragmentReader f = new FragmentReader(data);
            int root = f.Root();
            FragmentModel model = new FragmentModel();
            model.Reader = f;
            model.Species = f.U16(root);
            model.Name = SpeciesNames.Get(model.Species);
            List<int> layouts = f.PtrList(f.Ptr(root + 8));
            List<int> animations = f.PtrList(f.Ptr(root + 0x0C));
            List<int> auxAnimations = f.PtrList(f.Ptr(root + 0x10));
            if (layouts.Count == 0)
                return model;

            ParserState state = new ParserState(model);
            state.Walk(layouts[0], 0);
            for (int i = 0; i < animations.Count; i++)
                model.Animations.Add(ParseAnimation(model, animations[i], i));
            for (int i = 0; i < auxAnimations.Count; i++)
                model.AuxAnimations.Add(ParseAuxAnimation(model.Reader, auxAnimations[i], i));
            return model;
        }

        private sealed class ParserState
        {
            private readonly FragmentModel _model;
            private readonly FragmentReader _f;
            private readonly List<int> _stack = new List<int>();
            private readonly VertexData[] _vertexCache = new VertexData[64];
            private readonly Dictionary<int, int> _boneById = new Dictionary<int, int>();
            private readonly Dictionary<string, PrimitiveData> _primitiveMap = new Dictionary<string, PrimitiveData>();
            private int _currentTexture = -1;
            private int _currentTlut = -1;
            private int _currentMaterial = -1;
            private int _currentTextureAnimation = -1;
            private int _currentPalette;
            private readonly TileState[] _tiles = new TileState[8];
            private int _currentTile;

            public ParserState(FragmentModel model)
            {
                _model = model;
                _f = model.Reader;
                _stack.Add(-1);

                for (int i = 0; i < _tiles.Length; i++)
                    _tiles[i] = new TileState();
            }

            private int CurrentBone()
            {
                return _stack.Count >= 2 ? _stack[_stack.Count - 2] : -1;
            }

            public void Walk(int offset, int depth)
            {
                if (offset < 0 || depth > 32) return;
                while (offset >= 0 && offset < _f.Data.Length)
                {
                    int command = _f.U8(offset);
                    if (command < 0 || command >= CommandSizes.Length) return;
                    int size = CommandSizes[command];
                    if (command == 0x01 || command == 0x04) return;
                    if (command == 0x00 || command == 0x03) Walk(_f.Ptr(offset + 4), depth + 1);
                    else if (command == 0x02) { offset = _f.Ptr(offset + 4); continue; }
                    else if (command == 0x05) _stack.Add(_stack[_stack.Count - 1]);
                    else if (command == 0x06) { if (_stack.Count > 1) _stack.RemoveAt(_stack.Count - 1); }
                    else if (command == 0x17) ReadModelHeader(offset);
                    else if (command == 0x1C) _model.RootScale = new Vector3(_f.S32(offset + 4) / 65536f, _f.S32(offset + 8) / 65536f, _f.S32(offset + 0x0C) / 65536f);
                    else if (command == 0x1D) ReadBone(offset);
                    else if (command == 0x23)
                    {
                        _currentTextureAnimation = _f.S16(offset + 2);
                        _currentTexture = _f.S16(offset + 8);
                        _currentTlut = _f.S16(offset + 0x0A);
                        _currentMaterial = _f.Ptr(offset + 4);
                        _currentPalette = ReadMaterialPalette(_currentMaterial);
                        ApplyMaterialDisplayList(_currentMaterial);
                    }
                    else if (command == 0x22) RunDisplayList(_f.Ptr(offset + 4), CurrentBone(), 0);
                    else if (command == 0x1E)
                    {
                        int bone;
                        if (!_boneById.TryGetValue(_f.S16(offset + 2), out bone)) bone = CurrentBone();
                        RunDisplayList(_f.Ptr(offset + 4), bone, 0);
                    }
                    else if (command == 0x20) RunDisplayList(_f.Ptr(offset + 0x10), CurrentBone(), 0);
                    else if (command == 0x21) RunDisplayList(_f.Ptr(offset + 0x0C), CurrentBone(), 0);
                    offset += size;
                }
            }

            private void ReadModelHeader(int o)
            {
                int textureCount = _f.S16(o + 2);
                int tlutCount = _f.S16(o + 4);
                int textureTable = _f.Ptr(o + 8);
                int tlutTable = _f.Ptr(o + 0x0C);
                for (int i = 0; i < textureCount; i++)
                {
                    int p = textureTable + i * 0x0C;
                    TextureRecord t = new TextureRecord();
                    t.Format = _f.U8(p); t.Size = _f.U8(p + 1); t.Width = _f.S16(p + 2); t.Height = _f.U16(p + 4); t.DataOffset = _f.Ptr(p + 8);
                    _model.Textures.Add(t);
                }
                if (tlutTable >= 0)
                {
                    for (int i = 0; i < tlutCount; i++)
                    {
                        int p = tlutTable + i * 0x0C;
                        TlutRecord t = new TlutRecord();
                        t.Count = _f.U16(p + 2); t.DataOffset = _f.Ptr(p + 4); t.DisplayList = _f.Ptr(p + 8);
                        ResolveTlut(t);
                        _model.Tluts.Add(t);
                    }
                }
            }

            private void ResolveTlut(TlutRecord record)
            {
                int dl = record.DisplayList;
                if (dl < 0) return;
                for (int i = 0; i < 16 && dl + 8 <= _f.Data.Length; i++, dl += 8)
                {
                    uint w0 = _f.U32(dl), w1 = _f.U32(dl + 4);
                    int op = (int)(w0 >> 24);
                    if (op == 0xFD) record.DataOffset = unchecked((int)w1) - FragmentReader.BaseAddress;
                    else if (op == 0xF0) record.Count = (int)((w1 >> 14) & 0x3FF) + 1;
                    else if (op == 0xDF) break;
                }
            }

            private void ReadBone(int o)
            {
                BoneData b = new BoneData();
                b.Parent = CurrentBone(); b.BoneId = _f.U8(o + 1); b.Flags = _f.U8(o + 2); b.Channel = _f.S8(o + 3);
                b.Translation = new Vector3(_f.S16(o + 4), _f.S16(o + 6), _f.S16(o + 8));
                b.RotationUnits = new Vector3(_f.S16(o + 0x0A), _f.S16(o + 0x0C), _f.S16(o + 0x0E));
                b.Scale = new Vector3(_f.S32(o + 0x10) / 65536f, _f.S32(o + 0x14) / 65536f, _f.S32(o + 0x18) / 65536f);
                int index = _model.Bones.Count;
                _model.Bones.Add(b);
                _boneById[b.BoneId] = index;
                _stack[_stack.Count - 1] = index;
            }

            private int ReadMaterialPalette(int offset)
            {
                if (offset < 0)
                    return 0;

                int palette = 0;
                int guard = 0;

                while (offset >= 0 && offset + 8 <= _f.Data.Length && guard++ < 16)
                {
                    uint w0 = _f.U32(offset);
                    uint w1 = _f.U32(offset + 4);
                    int op = (int)(w0 >> 24);

                    if (op == 0xF5 && ((w1 >> 24) & 0x07) == 0)
                        palette = (int)((w1 >> 20) & 0x0F);
                    else if (op == 0xDF)
                        break;

                    offset += 8;
                }

                return palette;
            }

            private void ApplyMaterialDisplayList(int offset)
            {
                if (offset < 0)
                    return;

                int currentTile = _currentTile;
                int guard = 0;

                while (offset >= 0 && offset + 8 <= _f.Data.Length && guard++ < 256)
                {
                    uint w0 = _f.U32(offset);
                    uint w1 = _f.U32(offset + 4);
                    int op = (int)(w0 >> 24);
                    offset += 8;

                    if (op == 0xDF)
                        break;

                    if (op == 0xD7)
                    {
                        currentTile = (int)((w0 >> 8) & 0x07);
                        _currentTile = currentTile;
                    }
                    else if (op == 0xF5)
                    {
                        int tileIndex = (int)((w1 >> 24) & 0x07);
                        int cmt = (int)((w1 >> 18) & 0x03);
                        int cms = (int)((w1 >> 8) & 0x03);

                        TileState tile = _tiles[tileIndex];
                        tile.Palette = (int)((w1 >> 20) & 0x0F);
                        tile.MirrorS = (cms & 1) != 0;
                        tile.ClampS = (cms & 2) != 0;
                        tile.MirrorT = (cmt & 1) != 0;
                        tile.ClampT = (cmt & 2) != 0;
                    }
                    else if (op == 0xDE)
                    {
                        int nested = unchecked((int)w1) - FragmentReader.BaseAddress;
                        ApplyMaterialDisplayList(nested);
                        if (((w0 >> 16) & 0xFF) != 0)
                            break;
                    }
                }
            }

            private void RunDisplayList(int o, int bone, int depth)
            {
                if (o < 0 || depth > 8) return;
                int cull = 0x400;
                while (o >= 0 && o + 8 <= _f.Data.Length)
                {
                    uint w0 = _f.U32(o), w1 = _f.U32(o + 4);
                    int op = (int)(w0 >> 24);
                    o += 8;
                    if (op == 0xDF) return;
                    if (op == 0xDE)
                    {
                        RunDisplayList(unchecked((int)w1) - FragmentReader.BaseAddress, bone, depth + 1);
                        if (((w0 >> 16) & 0xFF) != 0) return;
                    }
                    else if (op == 0x01)
                    {
                        int count = (int)((w0 >> 12) & 0xFF);
                        int first = (int)(((w0 & 0xFFF) >> 1) - count);
                        int address = unchecked((int)w1) - FragmentReader.BaseAddress;
                        for (int i = 0; i < count; i++)
                        {
                            int p = address + i * 0x10;
                            int slot = first + i;
                            if (slot < 0 || slot >= _vertexCache.Length || p < 0 || p + 16 > _f.Data.Length) continue;
                            VertexData v = new VertexData();
                            v.Position = new Vector3(_f.S16(p), _f.S16(p + 2), _f.S16(p + 4));
                            v.UVRaw = new Vector2(_f.S16(p + 8) / 32f, _f.S16(p + 10) / 32f);
                            bool lightingEnabled = (cull & 0x00020000) != 0;
                            if (lightingEnabled)
                            {
                                v.Normal = new Vector3(_f.S8(p + 12) / 127f, _f.S8(p + 13) / 127f, _f.S8(p + 14) / 127f);
                                v.Color = new Color32(255, 255, 255, _f.U8(p + 15));
                            }
                            else
                            {
                                v.Normal = Vector3.up;
                                v.Color = new Color32(_f.U8(p + 12), _f.U8(p + 13), _f.U8(p + 14), _f.U8(p + 15));
                            }
                            v.Bone = bone;
                            _vertexCache[slot] = v;
                        }
                    }
                    else if (op == 0xD9)
                    {
                        cull = (cull & (int)(w0 & 0xFFFFFF)) | unchecked((int)w1);
                    }
                    else if (op == 0xD7)
                    {
                        _currentTile = (int)((w0 >> 8) & 0x07);
                    }
                    else if (op == 0xF5)
                    {
                        int tileIndex = (int)((w1 >> 24) & 0x07);
                        int cmt = (int)((w1 >> 18) & 0x03);
                        int cms = (int)((w1 >> 8) & 0x03);

                        TileState tile = _tiles[tileIndex];
                        tile.Palette = (int)((w1 >> 20) & 0x0F);
                        tile.MirrorS = (cms & 1) != 0;
                        tile.ClampS = (cms & 2) != 0;
                        tile.MirrorT = (cmt & 1) != 0;
                        tile.ClampT = (cmt & 2) != 0;
                    }
                    else if (op == 0x05 || op == 0x06)
                    {
                        PrimitiveData primitive = GetPrimitive(cull & 0x600);
                        EmitTriangle(primitive, (int)(((w0 >> 16) & 0xFF) / 2), (int)(((w0 >> 8) & 0xFF) / 2), (int)((w0 & 0xFF) / 2), cull);
                        if (op == 0x06)
                            EmitTriangle(primitive, (int)(((w1 >> 16) & 0xFF) / 2), (int)(((w1 >> 8) & 0xFF) / 2), (int)((w1 & 0xFF) / 2), cull);
                    }
                }
            }

            private PrimitiveData GetPrimitive(int cull)
            {
                TileState tile = _tiles[Mathf.Clamp(_currentTile, 0, _tiles.Length - 1)];
                string key = _currentTexture + ":" + _currentTlut + ":" + _currentMaterial + ":" + _currentTextureAnimation + ":" + cull + ":" +
                             _currentPalette + ":" + tile.MirrorS + ":" + tile.MirrorT + ":" + tile.ClampS + ":" + tile.ClampT;

                PrimitiveData primitive;
                if (!_primitiveMap.TryGetValue(key, out primitive))
                {
                    primitive = new PrimitiveData();
                    primitive.Texture = _currentTexture;
                    primitive.Tlut = _currentTlut;
                    primitive.MaterialDisplayList = _currentMaterial;
                    primitive.TextureAnimation = _currentTextureAnimation;
                    primitive.Cull = cull;
                    primitive.Palette = _currentPalette;
                    primitive.MirrorS = tile.MirrorS;
                    primitive.MirrorT = tile.MirrorT;
                    primitive.ClampS = tile.ClampS;
                    primitive.ClampT = tile.ClampT;

                    _primitiveMap.Add(key, primitive);
                    _model.Primitives.Add(primitive);
                }

                return primitive;
            }

            private void EmitTriangle(PrimitiveData p, int a, int b, int c, int cull)
            {
                int[] source = { a, b, c };
                if ((cull & 0x200) != 0 && (cull & 0x400) == 0) { int temp = source[0]; source[0] = source[2]; source[2] = temp; }
                for (int i = 0; i < 3; i++)
                {
                    int slot = source[i];
                    if (slot < 0 || slot >= _vertexCache.Length || _vertexCache[slot] == null) return;
                }
                for (int i = 0; i < 3; i++)
                {
                    VertexData v = _vertexCache[source[i]];
                    string key = v.Position.x + "," + v.Position.y + "," + v.Position.z + "," +
                                 v.UVRaw.x + "," + v.UVRaw.y + "," +
                                 v.Normal.x + "," + v.Normal.y + "," + v.Normal.z + "," +
                                 v.Color.r + "," + v.Color.g + "," + v.Color.b + "," + v.Color.a + "," +
                                 v.Bone;
                    int index;
                    if (!p.Remap.TryGetValue(key, out index))
                    {
                        index = p.Vertices.Count; p.Remap.Add(key, index); p.Vertices.Add(v);
                    }
                    p.Indices.Add(index);
                }
            }
        }

        private sealed class AnimationReader
        {
            public FragmentReader F;
            public int Flags, LoopStart, ChannelCount, FrameCount, ChannelTable, ScaleData, RotationData, TranslationData;
            public AnimationReader(FragmentReader f, int o)
            {
                F = f; Flags = f.U8(o); LoopStart = f.U16(o + 6); ChannelCount = f.U16(o + 8); FrameCount = f.U16(o + 0x0A);
                ChannelTable = f.Ptr(o + 0x0C); ScaleData = f.Ptr(o + 0x10); RotationData = f.Ptr(o + 0x14); TranslationData = f.Ptr(o + 0x18);
            }
            private Channel ReadChannel(int i)
            {
                int o = ChannelTable + i * 0x0A;
                Channel c = new Channel(); c.NScale = F.U8(o); c.NRotation = F.U8(o + 1); c.NTranslation = F.U8(o + 2); c.Interpolation = F.U8(o + 3); c.OScale = F.U16(o + 4); c.ORotation = F.U16(o + 6); c.OTranslation = F.U16(o + 8); return c;
            }
            public bool Sample(int channelIndex, int frame, BoneData bind, out Vector3 t, out Vector3 r, out Vector3 s)
            {
                t = bind.Translation; r = bind.RotationUnits; s = bind.Scale;
                int baseIndex = channelIndex * 3;
                if (baseIndex < 0 || baseIndex + 2 >= ChannelCount) return false;
                Channel cx = ReadChannel(baseIndex), cy = ReadChannel(baseIndex + 1), cz = ReadChannel(baseIndex + 2);
                Channel[] channels = { cx, cy, cz };
                for (int axis = 0; axis < 3; axis++)
                {
                    Channel c = channels[axis];
                    float tv = ReadTranslation(c, frame, Get(t, axis));
                    float rv = ReadRotation(c, frame, Get(r, axis));
                    float sv = ReadScale(c, frame, Get(s, axis));
                    Set(ref t, axis, tv); Set(ref r, axis, rv); Set(ref s, axis, sv);
                }
                return true;
            }
            private float ReadTranslation(Channel c, int frame, float fallback)
            {
                if ((Flags & 8) != 0)
                {
                    if (c.NTranslation < 2) return Sign16(c.OTranslation);
                    return Hermite(TranslationData + c.OTranslation * 2, c.NTranslation, frame, (c.Interpolation & 1) != 0);
                }
                if (c.NTranslation == 0) return fallback;
                if (c.NTranslation == 1) return (Flags & 4) != 0 ? c.OTranslation : (short)(c.OTranslation * 16) >> 4;
                int bits = (Flags & 4) != 0 ? 16 : 12;
                return BitField(TranslationData, c.OTranslation + Math.Min(frame, c.NTranslation - 1), bits);
            }
            private float ReadRotation(Channel c, int frame, float fallback)
            {
                if ((Flags & 8) != 0)
                {
                    float degrees = c.NRotation < 2 ? Sign16(c.ORotation) / 10f : Hermite(RotationData + c.ORotation * 2, c.NRotation, frame, (c.Interpolation & 2) != 0) / 10f;
                    degrees %= 360f; if (degrees < 0f) degrees += 360f;
                    return degrees / 360f * 65536f;
                }
                if (c.NRotation == 0) return fallback;
                if (c.NRotation == 1) return unchecked((short)(c.ORotation * 16));
                return unchecked((short)(BitField(RotationData, c.ORotation + Math.Min(frame, c.NRotation - 1), 12) * 16));
            }
            private float ReadScale(Channel c, int frame, float fallback)
            {
                if ((Flags & 8) != 0)
                {
                    if (c.NScale < 2) return Sign16(c.OScale) / 100f;
                    return Hermite(ScaleData + c.OScale * 2, c.NScale, frame, (c.Interpolation & 4) != 0) / 100f;
                }
                if (c.NScale == 0) return fallback;
                if (c.NScale == 1) return c.OScale / 1000f;
                return F.S16(ScaleData + (c.OScale + Math.Min(frame, c.NScale - 1)) * 2) / 1000f;
            }
            private int BitField(int baseOffset, int index, int bits)
            {
                int bitPosition = index * bits;
                int word = bitPosition >= 0 ? bitPosition / 16 : -((-bitPosition) / 16);
                int remainder = bitPosition - word * 16;
                int o = baseOffset + word * 2;
                uint value = ((uint)F.U16(o) << 16) | F.U16(o + 2);
                value <<= remainder & 31;
                int raw = (int)(value >> (32 - bits));
                int marker = 1 << (bits - 1);
                return (raw ^ marker) - marker;
            }
            private float Hermite(int baseOffset, int count, int frame, bool wide)
            {
                Key a = ReadKey(baseOffset, 0, wide), last = ReadKey(baseOffset, count - 1, wide);
                if (a.Frame >= frame) return a.Value;
                if (frame >= last.Frame) return last.Value;
                int index = 0;
                while (index < count - 2 && frame >= ReadKey(baseOffset, index + 1, wide).Frame) index++;
                a = ReadKey(baseOffset, index, wide); Key b = ReadKey(baseOffset, index + 1, wide);
                float x = (frame - a.Frame) / 30f; float y = 30f / (b.Frame - a.Frame); float x2 = x * x; float x3 = x2 * x; float y2 = y * y; float y3 = y2 * y;
                return a.Value * (2f * x3 * y3 - 3f * x2 * y2 + 1f) + b.Value * (-2f * x3 * y3 + 3f * x2 * y2) + a.OutTangent * (x3 * y2 - 2f * x2 * y + x) + b.InTangent * (x3 * y2 - x2 * y);
            }
            private Key ReadKey(int baseOffset, int index, bool wide)
            {
                int stride = wide ? 8 : 6; int o = baseOffset + index * stride;
                Key k = new Key(); k.Frame = F.S16(o); k.Value = F.S16(o + 2); k.InTangent = F.S16(o + 4); k.OutTangent = wide ? F.S16(o + 6) : k.InTangent; return k;
            }
            private static short Sign16(int value)
            {
                return unchecked((short)(value & 0xFFFF));
            }

            private static float Sign16(ushort v) { return unchecked((short)v); }
            private static float Get(Vector3 v, int axis) { return axis == 0 ? v.x : axis == 1 ? v.y : v.z; }
            private static void Set(ref Vector3 v, int axis, float value) { if (axis == 0) v.x = value; else if (axis == 1) v.y = value; else v.z = value; }
            private sealed class Channel { public int NScale, NRotation, NTranslation, Interpolation, OScale, ORotation, OTranslation; }
            private sealed class Key { public int Frame; public float Value, InTangent, OutTangent; }
        }

        private static AuxAnimationData ParseAuxAnimation(FragmentReader reader, int offset, int index)
        {
            AuxAnimationData animation = new AuxAnimationData();
            animation.Index = index;
            animation.Flags = reader.U8(offset);
            animation.LoopStart = reader.U16(offset + 6);
            int channelCount = reader.U16(offset + 8);
            animation.FrameCount = Math.Max(1, (int)reader.U16(offset + 0x0A));
            int channelTable = reader.Ptr(offset + 0x0C);
            int data = reader.Ptr(offset + 0x10);
            animation.Channels = new int[channelCount][];

            if (channelTable < 0 || data < 0)
                return animation;

            for (int channel = 0; channel < channelCount; channel++)
            {
                int record = channelTable + channel * 4;
                int count = reader.U16(record);
                int baseIndex = reader.U16(record + 2);
                int[] values = new int[animation.FrameCount];
                for (int frame = 0; frame < values.Length; frame++)
                {
                    if (count == 0)
                        values[frame] = -1;
                    else
                        values[frame] = reader.U8(data + baseIndex + Math.Min(frame, count - 1));
                }
                animation.Channels[channel] = values;
            }

            return animation;
        }

        private static AnimationData ParseAnimation(FragmentModel model, int offset, int index)
        {
            AnimationReader source = new AnimationReader(model.Reader, offset);
            AnimationData animation = new AnimationData(); animation.Index = index; animation.FrameCount = Math.Max(1, source.FrameCount); animation.LoopStart = source.LoopStart; animation.Tracks = new TrackData[model.Bones.Count];
            for (int i = 0; i < model.Bones.Count; i++)
            {
                BoneData bone = model.Bones[i];
                if (bone.Channel < 0) continue;
                TrackData track = new TrackData(); track.Translation = new Vector3[animation.FrameCount]; track.RotationUnits = new Vector3[animation.FrameCount]; track.Scale = new Vector3[animation.FrameCount];
                bool valid = false;
                for (int frame = 0; frame < animation.FrameCount; frame++)
                {
                    Vector3 t, r, s;
                    if (source.Sample(bone.Channel, frame, bone, out t, out r, out s)) valid = true;
                    track.Translation[frame] = t; track.RotationUnits[frame] = r; track.Scale[frame] = s;
                }
                if (valid) animation.Tracks[i] = track;
            }
            return animation;
        }
    }

    internal static class UnityModelWriter
    {
        private const float PositionScale = 0.01f;

        private sealed class PartBuildData
        {
            public int PrimitiveIndex;
            public PrimitiveData Primitive;
            public Mesh Mesh;
            public Material Material;
            public GameObject Object;
            public Renderer Renderer;
            public bool IsMaterialAnimated;
        }

        private sealed class MaterialCallbackContext
        {
            public PS3DS_TextureSwapper Swapper;
            public readonly List<PS3DS_TextureSwapper.TextureSwapSet> Sets =
                new List<PS3DS_TextureSwapper.TextureSwapSet>();
            public readonly Dictionary<string, int> SetIndices =
                new Dictionary<string, int>();
        }

        private sealed class MaterialStateEntry
        {
            public PartBuildData Part;
            public Texture Texture;
        }

        public static void Write(FragmentModel model, string rootPath, int fileIndex, bool overwrite,
            PS3DS_PokemonStadiumModelImporter.ContentMode contentMode, bool createPrefab,
            PS3DS_PokemonStadiumModelImporter.PrefabRendererMode prefabRendererMode, bool generateJson, bool flipTexturesY, bool _flipTexturesX,
            bool mirrorTextures, PS3DS_PokemonStadiumModelImporter.AnimationSystemMode animationSystemMode,
            PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode materialAnimationMode, bool instantiatePrefab, bool combineParts,
            Shader materialShader, bool vertexColorsAsGrayscale, bool importVertexColors, float vertexColorLuminance,
            float animatedVertexColorStrength, bool exportShiny, bool applyShinyMaterials,
            bool exportColorVariant, bool applyColorVariant,
            PS3DS_PokemonStadiumModelImporter.ColorVariantMode colorVariantMode,
            int variantTrainerId, string variantTrainerName, string variantNickname, int variantManualHue,
            bool overridePreviousVariant, bool verboseVariants)
        {
            string safeName = Sanitize(model.Name);
            string folderName = model.Species.ToString("000") + "_" + safeName;
            string folder = rootPath.TrimEnd('/') + "/" + folderName;

            if (verboseVariants && exportColorVariant)
            {
                LogShinyDiagnostics(model, folder+"/verbose");
            }

            if (AssetDatabase.IsValidFolder(folder) && exportColorVariant && !overridePreviousVariant)
            {
                if (TryAppendNumberedVariant(
                    model, folder, folderName, fileIndex, flipTexturesY, _flipTexturesX, mirrorTextures,
                    materialShader, colorVariantMode, variantTrainerId, variantTrainerName, variantNickname,
                    variantManualHue, animationSystemMode, materialAnimationMode, instantiatePrefab, applyColorVariant))
                    return;
            }

            if (AssetDatabase.IsValidFolder(folder))
            {
                if (!overwrite) return;
                AssetDatabase.DeleteAsset(folder);
            }

            bool exportMeshes = contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.MeshesOnly ||
                                contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.MeshesAndTextures ||
                                contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.Everything;
            bool exportTextures = contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.TexturesOnly ||
                                  contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.MeshesAndTextures ||
                                  contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.Everything;
            bool exportAnimations = contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.AnimationsOnly ||
                                    contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.Everything;
            bool prefabRequested = createPrefab && exportMeshes;
            bool skinnedPrefab = prefabRequested && prefabRendererMode == PS3DS_PokemonStadiumModelImporter.PrefabRendererMode.SkinnedRenderer;
            bool staticPrefab = prefabRequested && prefabRendererMode == PS3DS_PokemonStadiumModelImporter.PrefabRendererMode.StaticRenderer;
            bool createMaterials = prefabRequested && exportTextures;

            PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder);
            if (generateJson)
            {
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/JSON");
                PipelineJsonWriter.Write(model, folder + "/JSON/" + folderName + ".json", fileIndex);
            }
            if (exportMeshes)
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/Meshes");
            if (exportTextures)
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/Textures");
            if (createMaterials)
            {
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/Materials");
                if (exportShiny)
                    PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/Materials/Shiny");
            }
            if (exportTextures && exportShiny)
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/Textures/Shiny");
            if (createMaterials && exportColorVariant)
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/Materials/Variants");
            if (exportTextures && exportColorVariant)
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/Textures/Variants");
            if (exportAnimations)
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/Animations");

            if (contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.TexturesOnly)
            {
                CreateTextures(model, folder, false, flipTexturesY, _flipTexturesX, mirrorTextures, materialShader, exportShiny, applyShinyMaterials, exportColorVariant, applyColorVariant, colorVariantMode, variantTrainerId, variantTrainerName, variantNickname, variantManualHue, fileIndex);
                return;
            }

            GameObject root = new GameObject(folderName);
            try
            {
                root.transform.localScale = model.RootScale;
                Transform[] pivots = new Transform[model.Bones.Count];
                Transform[] joints = new Transform[model.Bones.Count];

                if (prefabRequested || exportAnimations)
                    CreateSkeleton(model, root.transform, pivots, joints);

                Dictionary<string, Material> materials = null;
                if (exportTextures)
                    materials = CreateTextures(model, folder, createMaterials, flipTexturesY, _flipTexturesX, mirrorTextures, materialShader, exportShiny, applyShinyMaterials, exportColorVariant, applyColorVariant, colorVariantMode, variantTrainerId, variantTrainerName, variantNickname, variantManualHue, fileIndex);

                List<PartBuildData> builtParts = new List<PartBuildData>();

                if (exportMeshes)
                {
                    for (int p = 0; p < model.Primitives.Count; p++)
                    {
                        PrimitiveData primitive = model.Primitives[p];
                        if (primitive.Indices.Count == 0) continue;

                        Mesh mesh = CreateMesh(model, primitive, joints, root.transform, skinnedPrefab, mirrorTextures, vertexColorsAsGrayscale, importVertexColors, vertexColorLuminance, animatedVertexColorStrength);
                        mesh.name = "Mesh_" + p.ToString("00");
                        string meshPath = folder + "/Meshes/" + mesh.name + ".asset";
                        AssetDatabase.CreateAsset(mesh, meshPath);

                        if (skinnedPrefab)
                        {
                            GameObject part = new GameObject("Part_" + p.ToString("00"));
                            part.transform.SetParent(root.transform, false);
                            SkinnedMeshRenderer renderer = part.AddComponent<SkinnedMeshRenderer>();
                            renderer.sharedMesh = mesh;
                            renderer.bones = joints;
                            renderer.rootBone = root.transform;
                            AssignMaterial(renderer, primitive, materials);
                            renderer.updateWhenOffscreen = false;
                        }
                        else if (staticPrefab)
                        {
                            GameObject part = new GameObject("Part_" + p.ToString("00"));
                            part.transform.SetParent(root.transform, false);
                            MeshFilter filter = part.AddComponent<MeshFilter>();
                            filter.sharedMesh = mesh;
                            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
                            AssignMaterial(renderer, primitive, materials);
                        }

                        GameObject createdPart = root.transform.Find("Part_" + p.ToString("00")) != null
                            ? root.transform.Find("Part_" + p.ToString("00")).gameObject
                            : null;
                        Renderer createdRenderer = createdPart != null ? createdPart.GetComponent<Renderer>() : null;
                        PartBuildData partData = new PartBuildData();
                        partData.PrimitiveIndex = p;
                        partData.Primitive = primitive;
                        partData.Mesh = mesh;
                        partData.Object = createdPart;
                        partData.Renderer = createdRenderer;
                        partData.Material = createdRenderer != null ? createdRenderer.sharedMaterial : null;
                        partData.IsMaterialAnimated = primitive.TextureAnimation >= 0;
                        builtParts.Add(partData);
                    }

                    if (prefabRequested && combineParts)
                        CreateCombinedPart(model, folder, root.transform, joints, skinnedPrefab, builtParts, vertexColorsAsGrayscale, importVertexColors, vertexColorLuminance);
                }

                MaterialCallbackContext callbackContext = null;
                if (exportAnimations &&
                    materialAnimationMode == PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode.ByCallback &&
                    prefabRequested)
                {
                    callbackContext = new MaterialCallbackContext();
                    callbackContext.Swapper = root.AddComponent<PS3DS_TextureSwapper>();
                }

                if (exportAnimations)
                {
                    bool attachAnimationSystem = skinnedPrefab &&
                                                 contentMode == PS3DS_PokemonStadiumModelImporter.ContentMode.Everything;
                    UnityEngine.Animation legacyAnimation = null;
                    Animator mecanimAnimator = null;
                    List<AnimationClip> createdClips = new List<AnimationClip>();

                    if (attachAnimationSystem)
                    {
                        if (animationSystemMode == PS3DS_PokemonStadiumModelImporter.AnimationSystemMode.Legacy)
                            legacyAnimation = root.AddComponent<UnityEngine.Animation>();
                        else
                            mecanimAnimator = root.AddComponent<Animator>();
                    }

                    for (int i = 0; i < model.Animations.Count; i++)
                    {
                        bool legacyClip = animationSystemMode == PS3DS_PokemonStadiumModelImporter.AnimationSystemMode.Legacy;
                        AnimationClip clip = CreateClip(
                            model,
                            model.Animations[i],
                            root.transform,
                            pivots,
                            joints,
                            legacyClip,
                            builtParts,
                            materials,
                            mirrorTextures,
                            materialAnimationMode,
                            callbackContext);
                        clip.name = "Animation_" + i.ToString("00");
                        string clipPath = folder + "/Animations/" + clip.name + ".anim";
                        AssetDatabase.CreateAsset(clip, clipPath);
                        createdClips.Add(clip);

                        if (legacyAnimation != null)
                        {
                            legacyAnimation.AddClip(clip, clip.name);
                            if (i == 0)
                                legacyAnimation.clip = clip;
                        }
                    }

                    if (mecanimAnimator != null && createdClips.Count > 0)
                    {
                        string controllerPath = folder + "/Animations/" + folderName + ".controller";
                        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

                        for (int i = 0; i < createdClips.Count; i++)
                        {
                            AnimatorState state = stateMachine.AddState(createdClips[i].name);
                            state.motion = createdClips[i];
                            if (i == 0)
                                stateMachine.defaultState = state;
                        }

                        mecanimAnimator.runtimeAnimatorController = controller;
                    }
                }

                if (callbackContext != null && callbackContext.Swapper != null)
                    AssignTextureSwapSets(callbackContext);

                if (staticPrefab)
                    DestroySkeletonRoots(model, root.transform, pivots);

                if (prefabRequested)
                {
                    string prefabPath = folder + "/" + folderName + ".prefab";
                    GameObject prefabAsset = PrefabUtility.CreatePrefab(prefabPath, root, ReplacePrefabOptions.ReplaceNameBased);

                    GameObject shinyPrefabAsset = null;
                    if (exportShiny && model.Species >= 1 && model.Species <= 151)
                    {
                        shinyPrefabAsset = CreateShinyCompanionPrefab(
                            root,
                            folder,
                            folderName,
                            animationSystemMode,
                            materialAnimationMode);
                    }

                    int resolvedVariantHue = 0;
                    bool hasResolvedVariant = false;
                    if (exportColorVariant && model.Species >= 1 && model.Species <= 151)
                    {
                        if (colorVariantMode == PS3DS_PokemonStadiumModelImporter.ColorVariantMode.TrainerData)
                            hasResolvedVariant = TryCalculateTrainerVariantHue(model.Species, variantTrainerId, variantTrainerName, variantNickname, out resolvedVariantHue);
                        else if (colorVariantMode == PS3DS_PokemonStadiumModelImporter.ColorVariantMode.ManualHue)
                        {
                            resolvedVariantHue = Mathf.Clamp(variantManualHue, -180, 180);
                            hasResolvedVariant = true;
                        }
                        else
                            hasResolvedVariant = TryCalculateRandomVariantHue(model.Species, fileIndex, out resolvedVariantHue);
                    }

                    GameObject variantPrefabAsset = null;
                    if (hasResolvedVariant)
                    {
                        variantPrefabAsset = CreateVariantCompanionPrefab(
                            root,
                            folder,
                            folderName,
                            resolvedVariantHue,
                            animationSystemMode,
                            materialAnimationMode);
                    }

                    if (instantiatePrefab)
                    {
                        bool usesShinyLut = PS3DS_PokemonStadiumShinyData.GetLut(model.Species) != null;

                        if (usesShinyLut && applyShinyMaterials && shinyPrefabAsset != null &&
                            applyColorVariant && variantPrefabAsset != null)
                        {
                            GameObject shinyInstance = InstantiatePrefabAsset(
                                shinyPrefabAsset,
                                folderName + "_Shiny",
                                "Instantiate shiny Pokemon Stadium model");

                            GameObject variantInstance = InstantiatePrefabAsset(
                                variantPrefabAsset,
                                folderName + "_Variant",
                                "Instantiate variant Pokemon Stadium model");

                            if (variantInstance != null)
                            {
                                if (shinyInstance != null)
                                    variantInstance.transform.position += Vector3.right * 2.0f;
                                Selection.activeGameObject = variantInstance;
                            }
                            else if (shinyInstance != null)
                            {
                                Selection.activeGameObject = shinyInstance;
                            }
                        }
                        else
                        {
                            GameObject prefabToInstantiate = prefabAsset;
                            string instanceName = folderName;
                            string undoName = "Instantiate Pokemon Stadium model";

                            if (applyColorVariant && variantPrefabAsset != null)
                            {
                                prefabToInstantiate = variantPrefabAsset;
                                instanceName = folderName + "_Variant";
                                undoName = "Instantiate variant Pokemon Stadium model";
                            }
                            else if (applyShinyMaterials && shinyPrefabAsset != null)
                            {
                                prefabToInstantiate = shinyPrefabAsset;
                                instanceName = folderName + "_Shiny";
                                undoName = "Instantiate shiny Pokemon Stadium model";
                            }

                            GameObject instance = InstantiatePrefabAsset(prefabToInstantiate, instanceName, undoName);
                            if (instance != null)
                                Selection.activeGameObject = instance;
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject InstantiatePrefabAsset(GameObject prefabAsset, string instanceName, string undoName)
        {
            if (prefabAsset == null)
                return null;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            if (instance == null)
                return null;

            Undo.RegisterCreatedObjectUndo(instance, undoName);
            instance.name = instanceName;
            return instance;
        }

        private static GameObject CreateShinyCompanionPrefab(
            GameObject sourceRoot,
            string folder,
            string folderName,
            PS3DS_PokemonStadiumModelImporter.AnimationSystemMode animationSystemMode,
            PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode materialAnimationMode)
        {
            if (sourceRoot == null)
                return null;

            GameObject shinyRoot = UnityEngine.Object.Instantiate(sourceRoot) as GameObject;
            if (shinyRoot == null)
                return null;

            shinyRoot.name = folderName + "_Shiny";

            try
            {
                ReplaceRendererMaterialsWithShiny(shinyRoot, folder);

                if (materialAnimationMode == PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode.ByCallback)
                    ReplaceTextureSwapperTexturesWithShiny(shinyRoot, folder);
                else
                    ReplaceAnimationMaterialCurvesWithShiny(shinyRoot, folder, folderName, animationSystemMode);

                string shinyPrefabPath = folder + "/" + folderName + "_Shiny.prefab";
                return PrefabUtility.CreatePrefab(shinyPrefabPath, shinyRoot, ReplacePrefabOptions.ReplaceNameBased);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shinyRoot);
            }
        }

        private static void ReplaceRendererMaterialsWithShiny(GameObject root, string folder)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] current = renderers[r].sharedMaterials;
                bool changed = false;

                for (int i = 0; i < current.Length; i++)
                {
                    Material shiny = FindShinyMaterialForMaterial(current[i], folder);
                    if (shiny == null)
                        continue;

                    current[i] = shiny;
                    changed = true;
                }

                if (changed)
                    renderers[r].sharedMaterials = current;
            }
        }

        private static Material FindShinyMaterialForMaterial(Material source, string folder)
        {
            if (source == null)
                return null;

            string baseName = source.name;
            int hueIndex = baseName.LastIndexOf("_Hue", StringComparison.Ordinal);
            if (hueIndex >= 0)
                baseName = baseName.Substring(0, hueIndex);
            if (baseName.EndsWith("_Shiny", StringComparison.Ordinal))
                return source;

            string path = folder + "/Materials/Shiny/" + baseName + "_Shiny.mat";
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static Texture FindShinyTextureForTexture(Texture source, string folder)
        {
            if (source == null)
                return null;

            string baseName = source.name;
            int hueIndex = baseName.LastIndexOf("_Hue", StringComparison.Ordinal);
            if (hueIndex >= 0)
                baseName = baseName.Substring(0, hueIndex);
            if (baseName.EndsWith("_Shiny", StringComparison.Ordinal))
                return source;

            string path = folder + "/Textures/Shiny/" + baseName + "_Shiny.png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ReplaceTextureSwapperTexturesWithShiny(GameObject root, string folder)
        {
            PS3DS_TextureSwapper swapper = root.GetComponent<PS3DS_TextureSwapper>();
            if (swapper == null)
                return;

            FieldInfo setsField = typeof(PS3DS_TextureSwapper).GetField(
                "m_sets",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (setsField == null)
                return;

            PS3DS_TextureSwapper.TextureSwapSet[] sets =
                setsField.GetValue(swapper) as PS3DS_TextureSwapper.TextureSwapSet[];
            if (sets == null)
                return;

            for (int s = 0; s < sets.Length; s++)
            {
                PS3DS_TextureSwapper.TextureSwapSet set = sets[s];
                if (set == null || set.Entries == null)
                    continue;

                for (int e = 0; e < set.Entries.Length; e++)
                {
                    PS3DS_TextureSwapper.TextureSwapEntry entry = set.Entries[e];
                    if (entry == null || entry.Texture == null)
                        continue;

                    Texture shiny = FindShinyTextureForTexture(entry.Texture, folder);
                    if (shiny != null)
                        entry.Texture = shiny;
                }
            }

            setsField.SetValue(swapper, sets);
            EditorUtility.SetDirty(swapper);
        }

        private static void ReplaceAnimationMaterialCurvesWithShiny(
            GameObject root,
            string folder,
            string folderName,
            PS3DS_PokemonStadiumModelImporter.AnimationSystemMode animationSystemMode)
        {
            if (animationSystemMode == PS3DS_PokemonStadiumModelImporter.AnimationSystemMode.Legacy)
            {
                UnityEngine.Animation animation = root.GetComponent<UnityEngine.Animation>();
                if (animation == null)
                    return;

                List<AnimationClip> clips = new List<AnimationClip>();
                foreach (AnimationState state in animation)
                    if (state != null && state.clip != null && !clips.Contains(state.clip))
                        clips.Add(state.clip);

                for (int i = 0; i < clips.Count; i++)
                {
                    AnimationClip shinyClip = CreateShinyAnimationClip(clips[i], folder, folderName);
                    if (shinyClip == null)
                        continue;
                    animation.RemoveClip(clips[i]);
                    animation.AddClip(shinyClip, shinyClip.name);
                    if (i == 0)
                        animation.clip = shinyClip;
                }
            }
            else
            {
                Animator animator = root.GetComponent<Animator>();
                AnimatorController sourceController = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
                if (animator == null || sourceController == null)
                    return;

                string controllerPath = folder + "/Animations/" + folderName + "_Shiny.controller";
                AnimatorController shinyController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                AnimatorStateMachine shinyStateMachine = shinyController.layers[0].stateMachine;
                AnimatorStateMachine sourceStateMachine = sourceController.layers[0].stateMachine;
                ChildAnimatorState[] states = sourceStateMachine.states;

                for (int i = 0; i < states.Length; i++)
                {
                    AnimatorState sourceState = states[i].state;
                    AnimationClip sourceClip = sourceState != null ? sourceState.motion as AnimationClip : null;
                    if (sourceClip == null)
                        continue;

                    AnimationClip shinyClip = CreateShinyAnimationClip(sourceClip, folder, folderName);
                    AnimatorState shinyState = shinyStateMachine.AddState(sourceState.name);
                    shinyState.motion = shinyClip != null ? shinyClip : sourceClip;
                    if (sourceStateMachine.defaultState == sourceState)
                        shinyStateMachine.defaultState = shinyState;
                }

                animator.runtimeAnimatorController = shinyController;
            }
        }

        private static AnimationClip CreateShinyAnimationClip(AnimationClip source, string folder, string folderName)
        {
            if (source == null)
                return null;

            AnimationClip clip = UnityEngine.Object.Instantiate(source) as AnimationClip;
            if (clip == null)
                return null;

            clip.name = source.name + "_Shiny";
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            for (int b = 0; b < bindings.Length; b++)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(source, bindings[b]);
                bool changed = false;
                for (int k = 0; k < keys.Length; k++)
                {
                    Material material = keys[k].value as Material;
                    if (material == null)
                        continue;

                    Material shiny = FindShinyMaterialForMaterial(material, folder);
                    if (shiny == null)
                        continue;

                    keys[k].value = shiny;
                    changed = true;
                }

                if (changed)
                    AnimationUtility.SetObjectReferenceCurve(clip, bindings[b], keys);
            }

            string path = folder + "/Animations/" + clip.name + ".anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        internal static bool ImportVariantOnly(
            FragmentModel model,
            string rootPath,
            int fileIndex,
            bool flipTexturesY,
            bool flipTexturesX,
            bool mirrorTextures,
            Shader materialShader,
            PS3DS_PokemonStadiumModelImporter.ColorVariantMode colorVariantMode,
            int variantTrainerId,
            string variantTrainerName,
            string variantNickname,
            int variantManualHue,
            PS3DS_PokemonStadiumModelImporter.AnimationSystemMode animationSystemMode,
            PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode materialAnimationMode,
            bool instantiatePrefab,
            bool applyColorVariant,
            bool verboseVariants)
        {
            if (model == null)
                return false;

            string safeName = Sanitize(model.Name);
            string folderName = model.Species.ToString("000") + "_" + safeName;
            string folder = rootPath.TrimEnd('/') + "/" + folderName;

            if (verboseVariants)
            {
                string verboseFolder = folder + "/verbose";
                PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(verboseFolder);
                LogShinyDiagnostics(model, verboseFolder);
            }

            return TryAppendNumberedVariant(
                model,
                folder,
                folderName,
                fileIndex,
                flipTexturesY,
                flipTexturesX,
                mirrorTextures,
                materialShader,
                colorVariantMode,
                variantTrainerId,
                variantTrainerName,
                variantNickname,
                variantManualHue,
                animationSystemMode,
                materialAnimationMode,
                instantiatePrefab,
                applyColorVariant);
        }

        private static bool TryAppendNumberedVariant(
            FragmentModel model,
            string folder,
            string folderName,
            int fileIndex,
            bool flipTexturesY,
            bool flipTexturesX,
            bool mirrorTextures,
            Shader materialShader,
            PS3DS_PokemonStadiumModelImporter.ColorVariantMode colorVariantMode,
            int variantTrainerId,
            string variantTrainerName,
            string variantNickname,
            int variantManualHue,
            PS3DS_PokemonStadiumModelImporter.AnimationSystemMode animationSystemMode,
            PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode materialAnimationMode,
            bool instantiatePrefab,
            bool applyColorVariant)
        {
            string basePrefabPath = folder + "/" + folderName + ".prefab";
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            if (basePrefab == null)
                return false;

            int hue;
            if (!TryResolveVariantHue(model.Species, fileIndex, colorVariantMode, variantTrainerId,
                variantTrainerName, variantNickname, variantManualHue, out hue))
                return false;

            int variantIndex = FindNextVariantIndex(folder, folderName);
            string number = variantIndex.ToString("00");
            string variantLabel = "Variant_" + number;
            string variantRoot = folder + "/VariantHistory/" + variantLabel;
            string textureFolder = variantRoot + "/Textures";
            string materialFolder = variantRoot + "/Materials";
            string animationFolder = variantRoot + "/Animations";

            PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(folder + "/VariantHistory");
            PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(variantRoot);
            PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(textureFolder);
            PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(materialFolder);
            PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(animationFolder);

            CreateNumberedVariantAssets(model, textureFolder, materialFolder, hue, flipTexturesY,
                flipTexturesX, mirrorTextures, materialShader);

            GameObject variantRootObject = UnityEngine.Object.Instantiate(basePrefab) as GameObject;
            if (variantRootObject == null)
                return false;

            string variantObjectName = folderName + "_Variant_" + number;
            variantRootObject.name = variantObjectName;
            try
            {
                ReplaceRendererMaterialsWithNumberedVariant(variantRootObject, materialFolder, hue);

                if (materialAnimationMode == PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode.ByCallback)
                    ReplaceTextureSwapperTexturesWithNumberedVariant(variantRootObject, textureFolder, hue);
                else
                {
                    ReplaceAnimationMaterialCurvesWithNumberedVariant(variantRootObject, materialFolder,
                        animationFolder, variantObjectName, hue, animationSystemMode);
                    ApplyInitialVariantMaterialsFromAnimation(variantRootObject);
                }

                string prefabPath = folder + "/" + variantObjectName + ".prefab";
                GameObject variantPrefab = PrefabUtility.CreatePrefab(prefabPath, variantRootObject,
                    ReplacePrefabOptions.ReplaceNameBased);

                Debug.Log("[Stadium2Unity Variant] Created " + variantObjectName +
                    " with hue " + FormatSignedInt(hue) + " degrees.");

                if (instantiatePrefab && applyColorVariant && variantPrefab != null)
                {
                    GameObject instance = InstantiatePrefabAsset(variantPrefab, variantObjectName,
                        "Instantiate numbered Pokemon Stadium variant");
                    if (instance != null)
                        Selection.activeGameObject = instance;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(variantRootObject);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        private static bool TryResolveVariantHue(int species, int fileIndex,
            PS3DS_PokemonStadiumModelImporter.ColorVariantMode mode, int trainerId,
            string trainerName, string nickname, int manualHue, out int hue)
        {
            if (mode == PS3DS_PokemonStadiumModelImporter.ColorVariantMode.TrainerData)
                return TryCalculateTrainerVariantHue(species, trainerId, trainerName, nickname, out hue);
            if (mode == PS3DS_PokemonStadiumModelImporter.ColorVariantMode.ManualHue)
            {
                hue = Mathf.Clamp(manualHue, -180, 180);
                return true;
            }
            return TryCalculateRandomVariantHue(species, fileIndex + Environment.TickCount, out hue);
        }

        private static int FindNextVariantIndex(string folder, string folderName)
        {
            for (int i = 1; i < 10000; i++)
            {
                string path = folder + "/" + folderName + "_Variant_" + i.ToString("00") + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    return i;
            }
            return 9999;
        }

        private static void CreateNumberedVariantAssets(FragmentModel model, string textureFolder,
            string materialFolder, int hue, bool flipTexturesY, bool flipTexturesX,
            bool mirrorTextures, Shader materialShader)
        {
            Shader shader = materialShader;
            if (shader == null) shader = Shader.Find("N3DS/N64_StadiumLit");
#if UNITY_2017_1_OR_NEWER
            if (shader == null) shader = Shader.Find("Standard");
#else
            if (shader == null) shader = Shader.Find("Legacy Shaders/VertexLit");
#endif
            if (shader == null) shader = Shader.Find("Unlit/Texture");

            Dictionary<string, PrimitiveData> variants = new Dictionary<string, PrimitiveData>();
            for (int i = 0; i < model.Textures.Count; i++)
            {
                PrimitiveData v = new PrimitiveData();
                v.Texture = i; v.Tlut = FindTlutForTexture(model, i); v.Palette = 0;
                v.ClampS = true; v.ClampT = true;
                variants[GetTextureVariantKey(v)] = v;
            }
            for (int i = 0; i < model.Primitives.Count; i++)
            {
                PrimitiveData source = model.Primitives[i];
                if (source.Texture < 0 || source.Texture >= model.Textures.Count) continue;
                AddTextureVariant(variants, source.Texture, source.Tlut, 0,
                    mirrorTextures && source.MirrorS, mirrorTextures && source.MirrorT);

                if (source.TextureAnimation >= 0)
                {
                    for (int a = 0; a < model.AuxAnimations.Count; a++)
                    {
                        AuxAnimationData aux = model.AuxAnimations[a];
                        if (source.TextureAnimation >= aux.Channels.Length) continue;
                        int[] track = aux.Channels[source.TextureAnimation];
                        if (track == null) continue;
                        for (int f = 0; f < track.Length; f++)
                        {
                            int textureIndex = track[f];
                            if (textureIndex < 0 || textureIndex >= model.Textures.Count) continue;
                            AddTextureVariant(variants, textureIndex, source.Tlut, 0,
                                mirrorTextures && source.MirrorS, mirrorTextures && source.MirrorT);
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, PrimitiveData> pair in variants)
            {
                PrimitiveData primitive = pair.Value;
                int textureIndex = primitive.Texture;
                DecodedTexture decoded = DecodeTexture(model, textureIndex, primitive.Tlut, 0);
                if (flipTexturesY) FlipTextureY(decoded);
                if (flipTexturesX) FlipTextureX(decoded);
#if !UNITY_2017_1_OR_NEWER
                if (mirrorTextures && (primitive.MirrorS || primitive.MirrorT))
                    decoded = BakeMirroredTexture(decoded, primitive.MirrorS, primitive.MirrorT);
#endif
                ApplyStadiumHueVariant(decoded, hue);
                string suffix = BuildTextureVariantSuffix(primitive) + "_Hue" + FormatSignedInt(hue);
                string textureName = "Texture_" + textureIndex.ToString("00") + suffix;
                string texturePath = textureFolder + "/" + textureName + ".png";
                WriteTextureAsset(decoded, textureName, texturePath, mirrorTextures, primitive);

                string materialName = "Material_" + textureIndex.ToString("00") + suffix;
                string materialPath = materialFolder + "/" + materialName + ".mat";
                Material material = new Material(shader);
                material.name = materialName;
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                AssetDatabase.CreateAsset(material, materialPath);
            }
        }

        private static Material FindNumberedVariantMaterial(Material source, string materialFolder, int hue)
        {
            if (source == null) return null;
            string baseName = StripColorVariantSuffixes(source.name);
            string suffix = "_Hue" + FormatSignedInt(hue);
            Material result = AssetDatabase.LoadAssetAtPath<Material>(materialFolder + "/" + baseName + suffix + ".mat");
            if (result != null) return result;
            if (source.mainTexture != null)
            {
                string textureBase = StripColorVariantSuffixes(source.mainTexture.name);
                if (textureBase.StartsWith("Texture_", StringComparison.Ordinal))
                {
                    string materialBase = "Material_" + textureBase.Substring("Texture_".Length);
                    return AssetDatabase.LoadAssetAtPath<Material>(materialFolder + "/" + materialBase + suffix + ".mat");
                }
            }
            return null;
        }

        private static void ReplaceRendererMaterialsWithNumberedVariant(GameObject root, string materialFolder, int hue)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] current = renderers[r].sharedMaterials;
                bool changed = false;
                for (int i = 0; i < current.Length; i++)
                {
                    Material variant = FindNumberedVariantMaterial(current[i], materialFolder, hue);
                    if (variant == null) continue;
                    current[i] = variant; changed = true;
                }
                if (changed) renderers[r].sharedMaterials = current;
            }
        }

        private static Texture FindNumberedVariantTexture(Texture source, string textureFolder, int hue)
        {
            if (source == null) return null;
            string baseName = StripColorVariantSuffixes(source.name);
            string suffix = "_Hue" + FormatSignedInt(hue);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/" + baseName + suffix + ".png");
        }

        private static void ReplaceTextureSwapperTexturesWithNumberedVariant(GameObject root, string textureFolder, int hue)
        {
            PS3DS_TextureSwapper swapper = root.GetComponent<PS3DS_TextureSwapper>();
            if (swapper == null) return;
            FieldInfo setsField = typeof(PS3DS_TextureSwapper).GetField("m_sets", BindingFlags.Instance | BindingFlags.NonPublic);
            if (setsField == null) return;
            PS3DS_TextureSwapper.TextureSwapSet[] sets = setsField.GetValue(swapper) as PS3DS_TextureSwapper.TextureSwapSet[];
            if (sets == null) return;
            for (int s = 0; s < sets.Length; s++)
            {
                if (sets[s] == null || sets[s].Entries == null) continue;
                for (int e = 0; e < sets[s].Entries.Length; e++)
                {
                    PS3DS_TextureSwapper.TextureSwapEntry entry = sets[s].Entries[e];
                    if (entry == null || entry.Texture == null) continue;
                    Texture variant = FindNumberedVariantTexture(entry.Texture, textureFolder, hue);
                    if (variant != null) entry.Texture = variant;
                }
            }
            setsField.SetValue(swapper, sets);
            EditorUtility.SetDirty(swapper);
        }

        private static AnimationClip CreateNumberedVariantAnimationClip(AnimationClip source,
            string materialFolder, string animationFolder, int hue, string label)
        {
            if (source == null) return null;
            AnimationClip clip = UnityEngine.Object.Instantiate(source) as AnimationClip;
            if (clip == null) return null;
            clip.name = source.name + "_" + label;
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            for (int b = 0; b < bindings.Length; b++)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(source, bindings[b]);
                bool changed = false;
                for (int k = 0; k < keys.Length; k++)
                {
                    Material material = keys[k].value as Material;
                    if (material == null) continue;
                    Material variant = FindNumberedVariantMaterial(material, materialFolder, hue);
                    if (variant == null) continue;
                    keys[k].value = variant; changed = true;
                }
                if (changed) AnimationUtility.SetObjectReferenceCurve(clip, bindings[b], keys);
            }
            string path = animationFolder + "/" + clip.name + ".anim";
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void ReplaceAnimationMaterialCurvesWithNumberedVariant(GameObject root,
            string materialFolder, string animationFolder, string variantObjectName, int hue,
            PS3DS_PokemonStadiumModelImporter.AnimationSystemMode animationSystemMode)
        {
            if (animationSystemMode == PS3DS_PokemonStadiumModelImporter.AnimationSystemMode.Legacy)
            {
                UnityEngine.Animation animation = root.GetComponent<UnityEngine.Animation>();
                if (animation == null) return;
                List<AnimationClip> clips = new List<AnimationClip>();
                foreach (AnimationState state in animation)
                    if (state != null && state.clip != null && !clips.Contains(state.clip)) clips.Add(state.clip);
                for (int i = 0; i < clips.Count; i++)
                {
                    AnimationClip variantClip = CreateNumberedVariantAnimationClip(clips[i], materialFolder,
                        animationFolder, hue, variantObjectName);
                    if (variantClip == null) continue;
                    animation.RemoveClip(clips[i]); animation.AddClip(variantClip, variantClip.name);
                    if (i == 0) animation.clip = variantClip;
                }
            }
            else
            {
                Animator animator = root.GetComponent<Animator>();
                AnimatorController sourceController = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
                if (animator == null || sourceController == null) return;
                string controllerPath = animationFolder + "/" + variantObjectName + ".controller";
                AnimatorController variantController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                AnimatorStateMachine target = variantController.layers[0].stateMachine;
                AnimatorStateMachine source = sourceController.layers[0].stateMachine;
                ChildAnimatorState[] states = source.states;
                for (int i = 0; i < states.Length; i++)
                {
                    AnimatorState sourceState = states[i].state;
                    AnimationClip sourceClip = sourceState != null ? sourceState.motion as AnimationClip : null;
                    if (sourceClip == null) continue;
                    AnimationClip variantClip = CreateNumberedVariantAnimationClip(sourceClip, materialFolder,
                        animationFolder, hue, variantObjectName);
                    AnimatorState state = target.AddState(sourceState.name);
                    state.motion = variantClip != null ? variantClip : sourceClip;
                    if (source.defaultState == sourceState) target.defaultState = state;
                }
                animator.runtimeAnimatorController = variantController;
            }
        }

        private static GameObject CreateVariantCompanionPrefab(
            GameObject sourceRoot,
            string folder,
            string folderName,
            int hue,
            PS3DS_PokemonStadiumModelImporter.AnimationSystemMode animationSystemMode,
            PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode materialAnimationMode)
        {
            if (sourceRoot == null)
                return null;

            GameObject variantRoot = UnityEngine.Object.Instantiate(sourceRoot) as GameObject;
            if (variantRoot == null)
                return null;

            variantRoot.name = folderName + "_Variant";

            try
            {
                ReplaceRendererMaterialsWithVariant(variantRoot, folder, hue);

                if (materialAnimationMode == PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode.ByCallback)
                {
                    ReplaceTextureSwapperTexturesWithVariant(variantRoot, folder, hue);
                }
                else
                {
                    ReplaceAnimationMaterialCurvesWithVariant(variantRoot, folder, folderName, hue, animationSystemMode);
                    ApplyInitialVariantMaterialsFromAnimation(variantRoot);
                }

                string variantPrefabPath = folder + "/" + folderName + "_Variant.prefab";
                return PrefabUtility.CreatePrefab(variantPrefabPath, variantRoot, ReplacePrefabOptions.ReplaceNameBased);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(variantRoot);
            }
        }

        private static void ReplaceRendererMaterialsWithVariant(GameObject root, string folder, int hue)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] current = renderers[r].sharedMaterials;
                bool changed = false;

                for (int i = 0; i < current.Length; i++)
                {
                    Material variant = FindVariantMaterialForMaterial(current[i], folder, hue);
                    if (variant == null)
                        continue;

                    current[i] = variant;
                    changed = true;
                }

                if (changed)
                    renderers[r].sharedMaterials = current;
            }
        }

        private static Material FindVariantMaterialForMaterial(Material source, string folder, int hue)
        {
            if (source == null)
                return null;

            string suffix = "_Hue" + FormatSignedInt(hue);
            string baseName = StripColorVariantSuffixes(source.name);
            string path = folder + "/Materials/Variants/" + baseName + suffix + ".mat";
            Material variant = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (variant != null)
                return variant;

            if (source.mainTexture != null)
            {
                string textureBaseName = StripColorVariantSuffixes(source.mainTexture.name);
                if (textureBaseName.StartsWith("Texture_", StringComparison.Ordinal))
                {
                    string materialBaseName = "Material_" + textureBaseName.Substring("Texture_".Length);
                    path = folder + "/Materials/Variants/" + materialBaseName + suffix + ".mat";
                    variant = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (variant != null)
                        return variant;
                }
            }

            return null;
        }

        private static string StripColorVariantSuffixes(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            string result = value;
            int hueIndex = result.LastIndexOf("_Hue", StringComparison.Ordinal);
            if (hueIndex >= 0)
                result = result.Substring(0, hueIndex);
            if (result.EndsWith("_Shiny", StringComparison.Ordinal))
                result = result.Substring(0, result.Length - "_Shiny".Length);
            if (result.EndsWith(" (Instance)", StringComparison.Ordinal))
                result = result.Substring(0, result.Length - " (Instance)".Length);
            if (result.EndsWith("(Clone)", StringComparison.Ordinal))
                result = result.Substring(0, result.Length - "(Clone)".Length).TrimEnd();
            return result;
        }

        private static Texture FindVariantTextureForTexture(Texture source, string folder, int hue)
        {
            if (source == null)
                return null;

            string baseName = source.name;
            int hueIndex = baseName.LastIndexOf("_Hue", StringComparison.Ordinal);
            if (hueIndex >= 0)
                baseName = baseName.Substring(0, hueIndex);
            if (baseName.EndsWith("_Shiny", StringComparison.Ordinal))
                baseName = baseName.Substring(0, baseName.Length - "_Shiny".Length);

            string suffix = "_Hue" + FormatSignedInt(hue);
            string path = folder + "/Textures/Variants/" + baseName + suffix + ".png";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ReplaceTextureSwapperTexturesWithVariant(GameObject root, string folder, int hue)
        {
            PS3DS_TextureSwapper swapper = root.GetComponent<PS3DS_TextureSwapper>();
            if (swapper == null)
                return;

            FieldInfo setsField = typeof(PS3DS_TextureSwapper).GetField(
                "m_sets",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (setsField == null)
                return;

            PS3DS_TextureSwapper.TextureSwapSet[] sets =
                setsField.GetValue(swapper) as PS3DS_TextureSwapper.TextureSwapSet[];
            if (sets == null)
                return;

            for (int s = 0; s < sets.Length; s++)
            {
                PS3DS_TextureSwapper.TextureSwapSet set = sets[s];
                if (set == null || set.Entries == null)
                    continue;

                for (int e = 0; e < set.Entries.Length; e++)
                {
                    PS3DS_TextureSwapper.TextureSwapEntry entry = set.Entries[e];
                    if (entry == null || entry.Texture == null)
                        continue;

                    Texture variant = FindVariantTextureForTexture(entry.Texture, folder, hue);
                    if (variant != null)
                        entry.Texture = variant;
                }
            }

            setsField.SetValue(swapper, sets);
            EditorUtility.SetDirty(swapper);
        }

        private static void ReplaceAnimationMaterialCurvesWithVariant(
            GameObject root,
            string folder,
            string folderName,
            int hue,
            PS3DS_PokemonStadiumModelImporter.AnimationSystemMode animationSystemMode)
        {
            if (animationSystemMode == PS3DS_PokemonStadiumModelImporter.AnimationSystemMode.Legacy)
            {
                UnityEngine.Animation animation = root.GetComponent<UnityEngine.Animation>();
                if (animation == null)
                    return;

                List<AnimationClip> clips = new List<AnimationClip>();
                foreach (AnimationState state in animation)
                    if (state != null && state.clip != null && !clips.Contains(state.clip))
                        clips.Add(state.clip);

                for (int i = 0; i < clips.Count; i++)
                {
                    AnimationClip variantClip = CreateVariantAnimationClip(clips[i], folder, hue);
                    if (variantClip == null)
                        continue;
                    animation.RemoveClip(clips[i]);
                    animation.AddClip(variantClip, variantClip.name);
                    if (i == 0)
                        animation.clip = variantClip;
                }
            }
            else
            {
                Animator animator = root.GetComponent<Animator>();
                AnimatorController sourceController = animator != null ? animator.runtimeAnimatorController as AnimatorController : null;
                if (animator == null || sourceController == null)
                    return;

                string controllerPath = folder + "/Animations/" + folderName + "_Variant.controller";
                AnimatorController variantController = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                AnimatorStateMachine variantStateMachine = variantController.layers[0].stateMachine;
                AnimatorStateMachine sourceStateMachine = sourceController.layers[0].stateMachine;
                ChildAnimatorState[] states = sourceStateMachine.states;

                for (int i = 0; i < states.Length; i++)
                {
                    AnimatorState sourceState = states[i].state;
                    AnimationClip sourceClip = sourceState != null ? sourceState.motion as AnimationClip : null;
                    if (sourceClip == null)
                        continue;

                    AnimationClip variantClip = CreateVariantAnimationClip(sourceClip, folder, hue);
                    AnimatorState variantState = variantStateMachine.AddState(sourceState.name);
                    variantState.motion = variantClip != null ? variantClip : sourceClip;
                    if (sourceStateMachine.defaultState == sourceState)
                        variantStateMachine.defaultState = variantState;
                }

                animator.runtimeAnimatorController = variantController;
            }
        }

        private static void ApplyInitialVariantMaterialsFromAnimation(GameObject root)
        {
            if (root == null)
                return;

            AnimationClip clip = GetInitialAnimationClip(root);
            if (clip == null)
                return;

            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            for (int b = 0; b < bindings.Length; b++)
            {
                EditorCurveBinding binding = bindings[b];
                if (binding.propertyName == null ||
                    !binding.propertyName.StartsWith("m_Materials.Array.data[", StringComparison.Ordinal))
                    continue;

                int materialIndex = ParseMaterialArrayIndex(binding.propertyName);
                if (materialIndex < 0)
                    continue;

                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keys == null || keys.Length == 0)
                    continue;

                Material initialMaterial = keys[0].value as Material;
                if (initialMaterial == null)
                    continue;

                Transform target = string.IsNullOrEmpty(binding.path)
                    ? root.transform
                    : root.transform.Find(binding.path);
                if (target == null)
                    continue;

                Renderer renderer = target.GetComponent(binding.type) as Renderer;
                if (renderer == null)
                    renderer = target.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                if (materialIndex >= materials.Length)
                    continue;

                materials[materialIndex] = initialMaterial;
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static AnimationClip GetInitialAnimationClip(GameObject root)
        {
            Animator animator = root.GetComponent<Animator>();
            AnimatorController controller = animator != null
                ? animator.runtimeAnimatorController as AnimatorController
                : null;

            if (controller != null && controller.layers != null && controller.layers.Length > 0)
            {
                AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                AnimatorState defaultState = stateMachine != null ? stateMachine.defaultState : null;
                AnimationClip defaultClip = defaultState != null ? defaultState.motion as AnimationClip : null;
                if (defaultClip != null)
                    return defaultClip;

                ChildAnimatorState[] states = stateMachine != null ? stateMachine.states : null;
                if (states != null)
                {
                    for (int i = 0; i < states.Length; i++)
                    {
                        AnimationClip clip = states[i].state != null
                            ? states[i].state.motion as AnimationClip
                            : null;
                        if (clip != null)
                            return clip;
                    }
                }
            }

            UnityEngine.Animation legacy = root.GetComponent<UnityEngine.Animation>();
            if (legacy != null)
            {
                if (legacy.clip != null)
                    return legacy.clip;

                foreach (AnimationState state in legacy)
                    if (state != null && state.clip != null)
                        return state.clip;
            }

            return null;
        }

        private static int ParseMaterialArrayIndex(string propertyName)
        {
            const string prefix = "m_Materials.Array.data[";
            if (string.IsNullOrEmpty(propertyName) ||
                !propertyName.StartsWith(prefix, StringComparison.Ordinal))
                return -1;

            int end = propertyName.IndexOf(']', prefix.Length);
            if (end < 0)
                return -1;

            int index;
            if (!int.TryParse(propertyName.Substring(prefix.Length, end - prefix.Length), out index))
                return -1;
            return index;
        }

        private static AnimationClip CreateVariantAnimationClip(AnimationClip source, string folder, int hue)
        {
            if (source == null)
                return null;

            AnimationClip clip = UnityEngine.Object.Instantiate(source) as AnimationClip;
            if (clip == null)
                return null;

            clip.name = source.name + "_Variant";
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            for (int b = 0; b < bindings.Length; b++)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(source, bindings[b]);
                bool changed = false;
                for (int k = 0; k < keys.Length; k++)
                {
                    Material material = keys[k].value as Material;
                    if (material == null)
                        continue;

                    Material variant = FindVariantMaterialForMaterial(material, folder, hue);
                    if (variant == null)
                        continue;

                    keys[k].value = variant;
                    changed = true;
                }

                if (changed)
                    AnimationUtility.SetObjectReferenceCurve(clip, bindings[b], keys);
            }

            string path = folder + "/Animations/" + clip.name + ".anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void CreateSkeleton(FragmentModel model, Transform root, Transform[] pivots, Transform[] joints)
        {
            for (int i = 0; i < model.Bones.Count; i++)
            {
                BoneData bone = model.Bones[i];
                GameObject boneObject = new GameObject("bone" + bone.BoneId.ToString("00"));
                Transform parent = bone.Parent >= 0 ? joints[bone.Parent] : root;

                boneObject.transform.SetParent(parent, false);
                boneObject.transform.localPosition = bone.Translation * PositionScale;
                boneObject.transform.localRotation = RotationFromGameUnits(bone.RotationUnits);
                boneObject.transform.localScale = bone.Scale;

                pivots[i] = boneObject.transform;
                joints[i] = boneObject.transform;
            }
        }

        private static void DestroySkeletonRoots(FragmentModel model, Transform root, Transform[] pivots)
        {
            for (int i = 0; i < model.Bones.Count; i++)
            {
                if (model.Bones[i].Parent >= 0 || pivots[i] == null)
                    continue;

                if (pivots[i].parent == root)
                    UnityEngine.Object.DestroyImmediate(pivots[i].gameObject);
            }
        }

        private static void AssignMaterial(Renderer renderer, PrimitiveData primitive, Dictionary<string, Material> materials)
        {
            if (materials == null || materials.Count == 0)
                return;

            PrimitiveData keyPrimitive = new PrimitiveData();
            keyPrimitive.Texture = primitive.Texture;
            keyPrimitive.Tlut = primitive.Tlut;
            keyPrimitive.ClampS = true;
            keyPrimitive.ClampT = true;
            keyPrimitive.Palette = 0;
            keyPrimitive.MirrorS = primitive.MirrorS;
            keyPrimitive.MirrorT = primitive.MirrorT;

            Material material;
            if (!materials.TryGetValue(GetTextureVariantKey(keyPrimitive), out material))
            {
                keyPrimitive.MirrorS = false;
                keyPrimitive.MirrorT = false;
                if (!materials.TryGetValue(GetTextureVariantKey(keyPrimitive), out material))
                    materials.TryGetValue("fallback", out material);
            }

            renderer.sharedMaterial = material;
        }

        private static Dictionary<string, Material> CreateTextures(FragmentModel model, string folder, bool createMaterials, bool flipTexturesY, bool flipTexturesX, bool mirrorTextures, Shader materialShader, bool exportShiny, bool applyShinyMaterials, bool exportColorVariant, bool applyColorVariant, PS3DS_PokemonStadiumModelImporter.ColorVariantMode colorVariantMode, int variantTrainerId, string variantTrainerName, string variantNickname, int variantManualHue, int fileIndex)
        {
            Dictionary<string, Material> materials = createMaterials
                ? new Dictionary<string, Material>()
                : null;

            Shader shader = null;
            if (createMaterials)
            {
                shader = materialShader;
                if (shader == null)
                    shader = Shader.Find("N3DS/N64_StadiumLit");
#if UNITY_2017_1_OR_NEWER
                if (shader == null)
                    shader = Shader.Find("Standard");
#else
                if (shader == null)
                    shader = Shader.Find("Legacy Shaders/VertexLit");
#endif
                if (shader == null)
                    shader = Shader.Find("Unlit/Texture");
            }

            int variantHue = 0;
            bool hasColorVariant = false;
            if (exportColorVariant && model.Species >= 1 && model.Species <= 151)
            {
                if (colorVariantMode == PS3DS_PokemonStadiumModelImporter.ColorVariantMode.TrainerData)
                {
                    hasColorVariant = TryCalculateTrainerVariantHue(model.Species, variantTrainerId, variantTrainerName, variantNickname, out variantHue);
                }
                else if (colorVariantMode == PS3DS_PokemonStadiumModelImporter.ColorVariantMode.ManualHue)
                {
                    variantHue = Mathf.Clamp(variantManualHue, -180, 180);
                    hasColorVariant = true;
                }
                else
                {
                    hasColorVariant = TryCalculateRandomVariantHue(model.Species, fileIndex, out variantHue);
                }

                if (hasColorVariant)
                    Debug.Log("[Stadium2Unity Variant] " + model.Name + " hue shift: " + FormatSignedInt(variantHue) + " degrees (" + colorVariantMode + ").");
            }

            Dictionary<string, PrimitiveData> variants = new Dictionary<string, PrimitiveData>();

            for (int i = 0; i < model.Textures.Count; i++)
            {
                PrimitiveData baseVariant = new PrimitiveData();
                baseVariant.Texture = i;
                baseVariant.Tlut = FindTlutForTexture(model, i);
                baseVariant.Palette = 0;
                baseVariant.ClampS = true;
                baseVariant.ClampT = true;
                variants[GetTextureVariantKey(baseVariant)] = baseVariant;
            }

            for (int i = 0; i < model.Primitives.Count; i++)
            {
                PrimitiveData source = model.Primitives[i];
                if (source.Texture < 0 || source.Texture >= model.Textures.Count)
                    continue;

                AddTextureVariant(variants, source.Texture, source.Tlut, 0,
                    mirrorTextures && source.MirrorS,
                    mirrorTextures && source.MirrorT);
            }

            for (int primitiveIndex = 0; primitiveIndex < model.Primitives.Count; primitiveIndex++)
            {
                PrimitiveData source = model.Primitives[primitiveIndex];
                if (source.TextureAnimation < 0)
                    continue;

                for (int auxIndex = 0; auxIndex < model.AuxAnimations.Count; auxIndex++)
                {
                    AuxAnimationData auxiliary = model.AuxAnimations[auxIndex];
                    if (source.TextureAnimation >= auxiliary.Channels.Length)
                        continue;
                    int[] track = auxiliary.Channels[source.TextureAnimation];
                    if (track == null)
                        continue;
                    for (int frame = 0; frame < track.Length; frame++)
                    {
                        int textureIndex = track[frame];
                        if (textureIndex < 0 || textureIndex >= model.Textures.Count)
                            continue;
                        AddTextureVariant(variants, textureIndex, source.Tlut, 0,
                            mirrorTextures && source.MirrorS,
                            mirrorTextures && source.MirrorT);
                    }
                }
            }

            foreach (KeyValuePair<string, PrimitiveData> pair in variants)
            {
                PrimitiveData primitive = pair.Value;
                int textureIndex = primitive.Texture;
                DecodedTexture decoded = DecodeTexture(model, textureIndex, primitive.Tlut, 0);

                PrepareDecodedTexture(decoded, flipTexturesY, flipTexturesX);

#if !UNITY_2017_1_OR_NEWER
                if (mirrorTextures && (primitive.MirrorS || primitive.MirrorT))
                    decoded = BakeMirroredTexture(decoded, primitive.MirrorS, primitive.MirrorT);
#endif

                string variantSuffix = BuildTextureVariantSuffix(primitive);
                string normalTextureName = "Texture_" + textureIndex.ToString("00") + variantSuffix;
                string normalTexturePath = folder + "/Textures/" + normalTextureName + ".png";
                WriteTextureAsset(decoded, normalTextureName, normalTexturePath, mirrorTextures, primitive);

                Material normalMaterial = null;
                if (createMaterials)
                {
                    string normalMaterialName = "Material_" + textureIndex.ToString("00") + variantSuffix;
                    string materialPath = folder + "/Materials/" + normalMaterialName + ".mat";
                    normalMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (normalMaterial == null)
                    {
                        normalMaterial = new Material(shader);
                        normalMaterial.name = normalMaterialName;
                        normalMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexturePath);
                        AssetDatabase.CreateAsset(normalMaterial, materialPath);
                    }
                }

                Material selectedMaterial = normalMaterial;

                if (exportShiny && model.Species >= 1 && model.Species <= 151)
                {
                    DecodedTexture shinyDecoded = CloneDecodedTexture(decoded);
                    if (ApplyStadiumShinyRecolor(model.Species, shinyDecoded))
                    {
                        string shinyTextureName = "Texture_" + textureIndex.ToString("00") + variantSuffix + "_Shiny";
                        string shinyTexturePath = folder + "/Textures/Shiny/" + shinyTextureName + ".png";
                        WriteTextureAsset(shinyDecoded, shinyTextureName, shinyTexturePath, mirrorTextures, primitive);

                        if (createMaterials)
                        {
                            string shinyMaterialName = "Material_" + textureIndex.ToString("00") + variantSuffix + "_Shiny";
                            string shinyMaterialPath = folder + "/Materials/Shiny/" + shinyMaterialName + ".mat";
                            Material shinyMaterial = AssetDatabase.LoadAssetAtPath<Material>(shinyMaterialPath);
                            if (shinyMaterial == null)
                            {
                                shinyMaterial = new Material(shader);
                                shinyMaterial.name = shinyMaterialName;
                                shinyMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(shinyTexturePath);
                                AssetDatabase.CreateAsset(shinyMaterial, shinyMaterialPath);
                            }
                        }
                    }
                }

                if (exportColorVariant && hasColorVariant)
                {
                    DecodedTexture variantDecoded = CloneDecodedTexture(decoded);
                    ApplyStadiumHueVariant(variantDecoded, variantHue);
                    string hueSuffix = "_Hue" + FormatSignedInt(variantHue);
                    string variantTextureName = "Texture_" + textureIndex.ToString("00") + variantSuffix + hueSuffix;
                    string variantTexturePath = folder + "/Textures/Variants/" + variantTextureName + ".png";
                    WriteTextureAsset(variantDecoded, variantTextureName, variantTexturePath, mirrorTextures, primitive);

                    if (createMaterials)
                    {
                        string variantMaterialName = "Material_" + textureIndex.ToString("00") + variantSuffix + hueSuffix;
                        string variantMaterialPath = folder + "/Materials/Variants/" + variantMaterialName + ".mat";
                        Material variantMaterial = AssetDatabase.LoadAssetAtPath<Material>(variantMaterialPath);
                        if (variantMaterial == null)
                        {
                            variantMaterial = new Material(shader);
                            variantMaterial.name = variantMaterialName;
                            variantMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(variantTexturePath);
                            AssetDatabase.CreateAsset(variantMaterial, variantMaterialPath);
                        }
                    }
                }

                if (createMaterials)
                    materials[pair.Key] = selectedMaterial;
            }

            if (createMaterials)
            {
                Material fallback = new Material(shader);
                fallback.name = "Material_None";
                fallback.color = Color.white;
                AssetDatabase.CreateAsset(fallback, folder + "/Materials/Material_None.mat");
                materials["fallback"] = fallback;
            }

            return materials;
        }

        private static DecodedTexture CloneDecodedTexture(DecodedTexture source)
        {
            DecodedTexture clone = new DecodedTexture();
            clone.Width = source.Width;
            clone.Height = source.Height;
            clone.Pixels = new Color32[source.Pixels.Length];
            Array.Copy(source.Pixels, clone.Pixels, source.Pixels.Length);
            return clone;
        }

        private static bool TryCalculateRandomVariantHue(int species, int fileIndex, out int hue)
        {
            hue = 0;
            int minHue, maxHue;
            if (!PS3DS_PokemonStadiumShinyData.TryGetHueRange(species, out minHue, out maxHue))
                return false;

            int seed = unchecked(Environment.TickCount ^ (species * 397) ^ (fileIndex * 7919));
            System.Random random = new System.Random(seed);
            hue = random.Next(minHue, maxHue + 1);
            return true;
        }

        private static bool TryCalculateTrainerVariantHue(int species, int trainerId, string trainerName, string nickname, out int hue)
        {
            hue = 0;
            int minHue, maxHue;
            if (!PS3DS_PokemonStadiumShinyData.TryGetHueRange(species, out minHue, out maxHue))
                return false;

            if (string.IsNullOrEmpty(nickname))
                return false;

            int value = 0;
            trainerId = Mathf.Clamp(trainerId, 0, 65535);
            value += (trainerId >> 8) & 0xFF;
            value += trainerId & 0xFF;
            value += SumStadiumNameBytes(trainerName);
            value += SumStadiumNameBytes(nickname);
            value &= 0xFF;

            double mapped = minHue + (value * (maxHue - minHue) / 255.0);
            hue = (int)Math.Round(mapped, MidpointRounding.AwayFromZero);
            return true;
        }

        private static int SumStadiumNameBytes(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            int sum = 0;
            for (int i = 0; i < value.Length; i++)
                sum += GetStadiumCharacterByte(value[i]);
            return sum;
        }

        private static int GetStadiumCharacterByte(char c)
        {
            switch (c)
            {
                case '×': return 0xD7;
                case '♂': return 0xA9;
                case '♀': return 0xBE;
                case 'Ä': return 0xC4;
                case 'Ö': return 0xD6;
                case 'Ü': return 0xDC;
                case 'ä': return 0xE4;
                case 'ö': return 0xF6;
                case 'ü': return 0xFC;
            }

            if (c <= 0xFF)
                return (int)c;

            return (int)'?';
        }

        private static void ApplyStadiumHueVariant(DecodedTexture texture, int hueDegrees)
        {
            if (texture == null || texture.Pixels == null)
                return;

            Dictionary<int, Color32> memo = new Dictionary<int, Color32>();
            for (int i = 0; i < texture.Pixels.Length; i++)
            {
                Color32 source = texture.Pixels[i];
                int key = (source.r << 16) | (source.g << 8) | source.b;
                Color32 recolored;
                if (!memo.TryGetValue(key, out recolored))
                {
                    byte r, g, b;
                    ApplyHslSlide(source.r, source.g, source.b, hueDegrees, 0, 0, out r, out g, out b);
                    recolored = new Color32(r, g, b, 255);
                    memo.Add(key, recolored);
                }
                texture.Pixels[i] = new Color32(recolored.r, recolored.g, recolored.b, source.a);
            }
        }

        private static string FormatSignedInt(int value)
        {
            return value >= 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool ApplyStadiumShinyRecolor(int species, DecodedTexture texture)
        {
            if (texture == null || texture.Pixels == null || texture.Pixels.Length == 0)
                return false;

            Dictionary<int, int> lut = PS3DS_PokemonStadiumShinyData.GetLut(species);
            PS3DS_PokemonStadiumShinyData.Slide slide;
            bool hasSlide = PS3DS_PokemonStadiumShinyData.TryGetSlide(species, out slide);

            if (lut == null && !hasSlide)
                return false;

            Dictionary<int, Color32> memo = new Dictionary<int, Color32>();
            Color32[] pixels = texture.Pixels;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 source = pixels[i];
                int key = (source.r << 16) | (source.g << 8) | source.b;

                Color32 recolored;
                if (!memo.TryGetValue(key, out recolored))
                {
                    byte r = source.r;
                    byte g = source.g;
                    byte b = source.b;

                    if (lut != null)
                    {
                        int mapped;
                        if (lut.TryGetValue(key, out mapped))
                        {
                            r = (byte)((mapped >> 16) & 0xFF);
                            g = (byte)((mapped >> 8) & 0xFF);
                            b = (byte)(mapped & 0xFF);
                        }
                    }
                    else
                    {
                        ApplyHslSlide(r, g, b, slide.Hue, slide.Saturation, slide.Lightness, out r, out g, out b);
                    }

                    recolored = new Color32(r, g, b, 255);
                    memo.Add(key, recolored);
                }

                pixels[i] = new Color32(recolored.r, recolored.g, recolored.b, source.a);
            }

            return true;
        }

        private static void ApplyHslSlide(byte sourceR, byte sourceG, byte sourceB, int hueDegrees, int saturationSteps, int lightnessSteps, out byte resultR, out byte resultG, out byte resultB)
        {
            double r = sourceR / 255.0;
            double g = sourceG / 255.0;
            double b = sourceB / 255.0;
            double maxValue = Math.Max(r, Math.Max(g, b));
            double minValue = Math.Min(r, Math.Min(g, b));
            double lightness = (maxValue + minValue) * 0.5;
            double saturation = 0.0;
            double hue = 0.0;

            if (maxValue != minValue)
            {
                double delta = maxValue - minValue;
                saturation = lightness > 0.5 ? delta / (2.0 - maxValue - minValue) : delta / (maxValue + minValue);
                if (maxValue == r)
                    hue = (g - b) / delta + (g < b ? 6.0 : 0.0);
                else if (maxValue == g)
                    hue = (b - r) / delta + 2.0;
                else
                    hue = (r - g) / delta + 4.0;
                hue *= 60.0;
            }

            double saturationShift = saturationSteps * 0.125;
            double lightnessShift = lightnessSteps * 0.125;

            if (saturation <= 0.0)
            {
                if (lightnessShift == 0.0)
                {
                    resultR = sourceR;
                    resultG = sourceG;
                    resultB = sourceB;
                    return;
                }
                byte gray = ToByteRounded(ShiftLightness(lightness, lightnessShift) * 255.0);
                resultR = gray; resultG = gray; resultB = gray;
                return;
            }

            saturation = ShiftSaturation(saturation, saturationShift);
            lightness = ShiftLightness(lightness, lightnessShift);
            HslToRgb(hue + hueDegrees, saturation, lightness, out resultR, out resultG, out resultB);
        }

        private static double ShiftSaturation(double saturation, double shift)
        {
            if (shift == 0.0) return saturation;
            return Math.Max(0.0, Math.Min(1.0, saturation * (1.0 + shift)));
        }

        private static double ShiftLightness(double lightness, double shift)
        {
            if (shift == 0.0) return lightness;
            if (shift < 0.0) return Math.Max(0.0, lightness * (1.0 + shift));
            return Math.Min(1.0, lightness + shift * (1.0 - lightness));
        }

        private static void HslToRgb(double hueDegrees, double saturation, double lightness, out byte r, out byte g, out byte b)
        {
            if (saturation <= 0.0)
            {
                byte gray = ToByteRounded(lightness * 255.0);
                r = gray; g = gray; b = gray;
                return;
            }

            double hue = hueDegrees % 360.0;
            if (hue < 0.0) hue += 360.0;
            hue /= 360.0;
            double q = lightness < 0.5 ? lightness * (1.0 + saturation) : lightness + saturation - lightness * saturation;
            double p = 2.0 * lightness - q;
            r = ToByteRounded(HueToRgb(p, q, hue + 1.0 / 3.0) * 255.0);
            g = ToByteRounded(HueToRgb(p, q, hue) * 255.0);
            b = ToByteRounded(HueToRgb(p, q, hue - 1.0 / 3.0) * 255.0);
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0.0) t += 1.0;
            if (t > 1.0) t -= 1.0;
            if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
            return p;
        }

        private static byte ToByteRounded(double value)
        {
            return (byte)Math.Max(0, Math.Min(255, (int)Math.Floor(value + 0.5)));
        }

        private static void ExportAllDebugTextures(FragmentModel model, string folder, bool flipTexturesY, bool flipTexturesX)
        {
            string debugFolder = folder + "/DebugTextures";
            PS3DS_PokemonStadiumModelImporter.EnsureAssetFolder(debugFolder);

            for (int textureIndex = 0; textureIndex < model.Textures.Count; textureIndex++)
            {
                try
                {
                    int tlutIndex = FindTlutForTexture(model, textureIndex);
                    DecodedTexture decoded = DecodeTexture(model, textureIndex, tlutIndex, 0);
                    PrepareDecodedTexture(decoded, flipTexturesY, flipTexturesX);

                    Texture2D texture = new Texture2D(decoded.Width, decoded.Height, TextureFormat.RGBA32, false);
                    texture.name = "Texture_" + textureIndex.ToString("00");
                    texture.SetPixels32(decoded.Pixels);
                    texture.Apply(false, false);

                    string texturePath = debugFolder + "/" + texture.name + ".png";
                    File.WriteAllBytes(ToAbsolutePath(texturePath), texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);

                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                    TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Default;
                        importer.mipmapEnabled = false;
                        importer.filterMode = FilterMode.Point;
                        importer.alphaIsTransparency = true;
                        importer.wrapMode = TextureWrapMode.Clamp;
                        importer.SaveAndReimport();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Stadium2Unity DebugTextures] Texture " + textureIndex + " failed: " + ex.Message);
                }
            }

            Debug.Log("[Stadium2Unity DebugTextures] Exported " + model.Textures.Count + " raw decoded textures for " + model.Name + " to " + debugFolder + ".");
        }

        private static void PrepareDecodedTexture(DecodedTexture texture, bool flipTexturesY, bool flipTexturesX)
        {
            if (flipTexturesY)
                FlipTextureY(texture);
            if (flipTexturesX)
                FlipTextureX(texture);
        }

        private static bool IsPaletteTexture(FragmentModel model, int textureIndex)
        {
            return textureIndex >= 0 &&
                   textureIndex < model.Textures.Count &&
                   model.Textures[textureIndex].Format == 2 &&
                   model.Textures[textureIndex].Size == 0;
        }

        private static void LogShinyDiagnostics(FragmentModel model, string folder)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Pokemon Stadium deep material / texture diagnostics");
            report.AppendLine("Model: " + model.Species.ToString("000") + " " + model.Name);
            report.AppendLine("Textures: " + model.Textures.Count);
            report.AppendLine("TLUTs: " + model.Tluts.Count);
            report.AppendLine("Primitives: " + model.Primitives.Count);
            report.AppendLine();

            report.AppendLine("=== TEXTURE TABLE ===");
            int ci4Count = 0;
            for (int i = 0; i < model.Textures.Count; i++)
            {
                TextureRecord texture = model.Textures[i];
                int tlut = FindTlutForTexture(model, i);
                bool ci4 = texture.Format == 2 && texture.Size == 0;
                if (ci4)
                    ci4Count++;

                report.Append("Texture ");
                report.Append(i.ToString("00"));
                report.Append(": format=");
                report.Append(texture.Format);
                report.Append(" (");
                report.Append(GetTextureFormatName(texture.Format));
                report.Append("), size=");
                report.Append(texture.Size);
                report.Append(" (");
                report.Append(GetTextureSizeName(texture.Size));
                report.Append("), dimensions=");
                report.Append(texture.Width);
                report.Append('x');
                report.Append(texture.Height);
                report.Append(", data=0x");
                report.Append(texture.DataOffset.ToString("X8"));
                report.Append(", firstPrimitiveTLUT=");
                report.Append(tlut);
                report.Append(", CI4=");
                report.Append(ci4 ? "YES" : "NO");
                report.AppendLine();
            }

            report.AppendLine();
            report.AppendLine("=== TLUT TABLE ===");
            for (int i = 0; i < model.Tluts.Count; i++)
            {
                TlutRecord tlut = model.Tluts[i];
                report.Append("TLUT ");
                report.Append(i.ToString("00"));
                report.Append(": count=");
                report.Append(tlut.Count);
                report.Append(", data=0x");
                report.Append(tlut.DataOffset.ToString("X8"));
                report.Append(", displayList=0x");
                report.Append(tlut.DisplayList.ToString("X8"));
                report.AppendLine();
                DumpDisplayList(report, model.Reader, tlut.DisplayList, "  ", 0, true);
            }

            report.AppendLine();
            report.AppendLine("=== PRIMITIVES / MATERIALS ===");
            HashSet<int> dumpedMaterials = new HashSet<int>();
            for (int i = 0; i < model.Primitives.Count; i++)
            {
                PrimitiveData primitive = model.Primitives[i];
                report.Append("Primitive ");
                report.Append(i.ToString("00"));
                report.Append(": texture=");
                report.Append(primitive.Texture);
                report.Append(", tlut=");
                report.Append(primitive.Tlut);
                report.Append(", parsedPalette=");
                report.Append(primitive.Palette);
                report.Append(", texAnim=");
                report.Append(primitive.TextureAnimation);
                report.Append(", mirrorS=");
                report.Append(primitive.MirrorS);
                report.Append(", mirrorT=");
                report.Append(primitive.MirrorT);
                report.Append(", clampS=");
                report.Append(primitive.ClampS);
                report.Append(", clampT=");
                report.Append(primitive.ClampT);
                report.Append(", materialDL=0x");
                report.Append(primitive.MaterialDisplayList.ToString("X8"));
                report.AppendLine();

                if (primitive.MaterialDisplayList >= 0 && dumpedMaterials.Add(primitive.MaterialDisplayList))
                {
                    report.AppendLine("  Material display list:");
                    DumpDisplayList(report, model.Reader, primitive.MaterialDisplayList, "    ", 0, false);
                }
            }

            report.AppendLine();
            report.AppendLine("=== AUX ANIMATIONS / TEXTURE CHANNELS ===");
            if (model.AuxAnimations.Count == 0)
            {
                report.AppendLine("No auxiliary animations found.");
            }
            else
            {
                for (int auxIndex = 0; auxIndex < model.AuxAnimations.Count; auxIndex++)
                {
                    AuxAnimationData aux = model.AuxAnimations[auxIndex];
                    report.Append("AuxAnimation ");
                    report.Append(auxIndex.ToString("00"));
                    report.Append(": frames=");
                    report.Append(aux.FrameCount);
                    report.Append(", loopStart=");
                    report.Append(aux.LoopStart);
                    report.Append(", flags=0x");
                    report.Append(aux.Flags.ToString("X2"));
                    report.Append(", channels=");
                    report.Append(aux.Channels != null ? aux.Channels.Length : 0);
                    report.AppendLine();

                    if (aux.Channels == null)
                        continue;

                    for (int channelIndex = 0; channelIndex < aux.Channels.Length; channelIndex++)
                    {
                        int[] values = aux.Channels[channelIndex];
                        report.Append("  Channel ");
                        report.Append(channelIndex.ToString("00"));
                        report.Append(": ");
                        if (values == null || values.Length == 0)
                        {
                            report.AppendLine("<empty>");
                            continue;
                        }

                        report.Append("values=[");
                        for (int frame = 0; frame < values.Length; frame++)
                        {
                            if (frame > 0) report.Append(',');
                            report.Append(values[frame]);
                        }
                        report.AppendLine("]");

                        report.Append("    runs: ");
                        int runStart = 0;
                        int runValue = values[0];
                        bool firstRun = true;
                        for (int frame = 1; frame <= values.Length; frame++)
                        {
                            bool endsRun = frame == values.Length || values[frame] != runValue;
                            if (!endsRun)
                                continue;

                            if (!firstRun) report.Append(" | ");
                            report.Append(runStart);
                            report.Append('-');
                            report.Append(frame - 1);
                            report.Append(" => Texture ");
                            report.Append(runValue);
                            firstRun = false;

                            if (frame < values.Length)
                            {
                                runStart = frame;
                                runValue = values[frame];
                            }
                        }
                        report.AppendLine();
                    }
                }
            }

            report.AppendLine();
            report.AppendLine("=== PRIMITIVE -> AUX CHANNEL REFERENCES ===");
            for (int primitiveIndex = 0; primitiveIndex < model.Primitives.Count; primitiveIndex++)
            {
                PrimitiveData primitive = model.Primitives[primitiveIndex];
                if (primitive.TextureAnimation < 0)
                    continue;
                report.Append("Primitive ");
                report.Append(primitiveIndex.ToString("00"));
                report.Append(" uses texture animation channel ");
                report.Append(primitive.TextureAnimation);
                report.Append(" (base texture ");
                report.Append(primitive.Texture);
                report.AppendLine(")");
            }

            report.AppendLine();
            report.AppendLine("=== SUMMARY ===");
            report.AppendLine("CI4 texture count: " + ci4Count);
            if (ci4Count == 0)
            {
                report.AppendLine("No CI4 textures were found, so a CI4 TLUT-bank shiny exporter cannot recolor this model.");
                report.AppendLine("Inspect the material display lists above for G_SETPRIMCOLOR/G_SETENVCOLOR/G_SETBLENDCOLOR or alternate texture/TLUT state.");
            }

            string reportPath = folder + "/ShinyDiagnostics.txt";
            File.WriteAllText(ToAbsolutePath(reportPath), report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(reportPath, ImportAssetOptions.ForceSynchronousImport);

            Debug.Log("[Stadium2Unity Shiny] Deep diagnostics written for " +
                      model.Species.ToString("000") + " " + model.Name +
                      ": textures=" + model.Textures.Count +
                      ", tluts=" + model.Tluts.Count +
                      ", CI4=" + ci4Count +
                      ". Report: " + reportPath);
        }

        private static string GetTextureFormatName(int format)
        {
            if (format == 0) return "RGBA";
            if (format == 1) return "YUV";
            if (format == 2) return "CI";
            if (format == 3) return "IA";
            if (format == 4) return "I";
            return "Unknown";
        }

        private static string GetTextureSizeName(int size)
        {
            if (size == 0) return "4b";
            if (size == 1) return "8b";
            if (size == 2) return "16b";
            if (size == 3) return "32b";
            return "Unknown";
        }

        private static void DumpDisplayList(StringBuilder report, FragmentReader reader, int offset, string indent, int depth, bool tlutList)
        {
            if (reader == null || offset < 0 || offset + 8 > reader.Data.Length)
            {
                report.Append(indent);
                report.AppendLine("<invalid display list>");
                return;
            }

            if (depth > 6)
            {
                report.Append(indent);
                report.AppendLine("<maximum nested display-list depth reached>");
                return;
            }

            int commandOffset = offset;
            int guard = 0;
            while (commandOffset >= 0 && commandOffset + 8 <= reader.Data.Length && guard++ < 256)
            {
                uint w0 = reader.U32(commandOffset);
                uint w1 = reader.U32(commandOffset + 4);
                int op = (int)(w0 >> 24);

                report.Append(indent);
                report.Append("0x");
                report.Append(commandOffset.ToString("X8"));
                report.Append("  ");
                report.Append(GetDisplayListOpcodeName(op));
                report.Append("  w0=0x");
                report.Append(w0.ToString("X8"));
                report.Append(" w1=0x");
                report.Append(w1.ToString("X8"));
                AppendDecodedDisplayListCommand(report, reader, op, w0, w1, tlutList);
                report.AppendLine();

                commandOffset += 8;
                if (op == 0xDF)
                    break;

                if (op == 0xDE)
                {
                    int nested = unchecked((int)w1) - FragmentReader.BaseAddress;
                    report.Append(indent);
                    report.Append("  -> nested 0x");
                    report.Append(nested.ToString("X8"));
                    report.AppendLine();
                    DumpDisplayList(report, reader, nested, indent + "    ", depth + 1, tlutList);
                    if (((w0 >> 16) & 0xFF) != 0)
                        break;
                }
            }

            if (guard >= 256)
            {
                report.Append(indent);
                report.AppendLine("<display-list guard limit reached>");
            }
        }

        private static string GetDisplayListOpcodeName(int op)
        {
            switch (op)
            {
                case 0xD7: return "G_TEXTURE";
                case 0xD9: return "G_GEOMETRYMODE";
                case 0xDE: return "G_DL";
                case 0xDF: return "G_ENDDL";
                case 0xE2: return "G_SETOTHERMODE_L";
                case 0xE3: return "G_SETOTHERMODE_H";
                case 0xE6: return "G_RDPLOADSYNC";
                case 0xE7: return "G_RDPPIPESYNC";
                case 0xE8: return "G_RDPTILESYNC";
                case 0xF0: return "G_LOADTLUT";
                case 0xF2: return "G_SETTILESIZE";
                case 0xF3: return "G_LOADBLOCK";
                case 0xF4: return "G_LOADTILE";
                case 0xF5: return "G_SETTILE";
                case 0xF7: return "G_SETFILLCOLOR";
                case 0xF8: return "G_SETFOGCOLOR";
                case 0xF9: return "G_SETBLENDCOLOR";
                case 0xFA: return "G_SETPRIMCOLOR";
                case 0xFB: return "G_SETENVCOLOR";
                case 0xFC: return "G_SETCOMBINE";
                case 0xFD: return "G_SETTIMG";
                case 0xFE: return "G_SETZIMG";
                case 0xFF: return "G_SETCIMG";
                default: return "OP_" + op.ToString("X2");
            }
        }

        private static void AppendDecodedDisplayListCommand(StringBuilder report, FragmentReader reader, int op, uint w0, uint w1, bool tlutList)
        {
            if (op == 0xFD)
            {
                int format = (int)((w0 >> 21) & 0x07);
                int size = (int)((w0 >> 19) & 0x03);
                int width = (int)(w0 & 0x0FFF) + 1;
                int address = unchecked((int)w1) - FragmentReader.BaseAddress;
                report.Append("  fmt="); report.Append(format); report.Append("/"); report.Append(GetTextureFormatName(format));
                report.Append(" size="); report.Append(size); report.Append("/"); report.Append(GetTextureSizeName(size));
                report.Append(" width="); report.Append(width);
                report.Append(" address=0x"); report.Append(address.ToString("X8"));
            }
            else if (op == 0xF5)
            {
                int format = (int)((w0 >> 21) & 0x07);
                int size = (int)((w0 >> 19) & 0x03);
                int line = (int)((w0 >> 9) & 0x01FF);
                int tmem = (int)(w0 & 0x01FF);
                int tile = (int)((w1 >> 24) & 0x07);
                int palette = (int)((w1 >> 20) & 0x0F);
                int cmt = (int)((w1 >> 18) & 0x03);
                int maskT = (int)((w1 >> 14) & 0x0F);
                int shiftT = (int)((w1 >> 10) & 0x0F);
                int cms = (int)((w1 >> 8) & 0x03);
                int maskS = (int)((w1 >> 4) & 0x0F);
                int shiftS = (int)(w1 & 0x0F);
                report.Append("  tile="); report.Append(tile);
                report.Append(" palette="); report.Append(palette);
                report.Append(" fmt="); report.Append(format); report.Append("/"); report.Append(GetTextureFormatName(format));
                report.Append(" size="); report.Append(size); report.Append("/"); report.Append(GetTextureSizeName(size));
                report.Append(" line="); report.Append(line);
                report.Append(" tmem="); report.Append(tmem);
                report.Append(" cms="); report.Append(cms);
                report.Append(" cmt="); report.Append(cmt);
                report.Append(" maskS="); report.Append(maskS);
                report.Append(" maskT="); report.Append(maskT);
                report.Append(" shiftS="); report.Append(shiftS);
                report.Append(" shiftT="); report.Append(shiftT);
            }
            else if (op == 0xF0)
            {
                int tile = (int)((w1 >> 24) & 0x07);
                int count = (int)((w1 >> 14) & 0x03FF) + 1;
                report.Append("  tile="); report.Append(tile);
                report.Append(" count="); report.Append(count);
            }
            else if (op == 0xFA || op == 0xFB || op == 0xF9 || op == 0xF7 || op == 0xF8)
            {
                byte r = (byte)(w1 >> 24);
                byte g = (byte)(w1 >> 16);
                byte b = (byte)(w1 >> 8);
                byte a = (byte)w1;
                report.Append("  rgba=("); report.Append(r); report.Append(','); report.Append(g); report.Append(','); report.Append(b); report.Append(','); report.Append(a); report.Append(')');
            }
            else if (op == 0xFC)
            {
                report.Append("  combineMux=0x"); report.Append((w0 & 0x00FFFFFFu).ToString("X6")); report.Append(w1.ToString("X8"));
            }
            else if (op == 0xDE)
            {
                int nested = unchecked((int)w1) - FragmentReader.BaseAddress;
                report.Append("  target=0x"); report.Append(nested.ToString("X8"));
                report.Append(" push="); report.Append((((w0 >> 16) & 0xFF) == 0) ? "YES" : "NO");
            }
        }

        private static int GetAvailablePaletteCount(FragmentModel model, int tlutIndex)
        {
            if (model == null || model.Reader == null || tlutIndex < 0 || tlutIndex >= model.Tluts.Count)
                return 1;

            int baseOffset = model.Tluts[tlutIndex].DataOffset;
            if (baseOffset < 0 || baseOffset >= model.Reader.Data.Length)
                return 1;

            int availableBytes = model.Reader.Data.Length - baseOffset;
            int availableBanks = availableBytes / (16 * 2);
            return Mathf.Clamp(availableBanks, 1, 16);
        }

        private static void WriteTextureAsset(DecodedTexture decoded, string textureName, string texturePath, bool mirrorTextures, PrimitiveData primitive)
        {
            Texture2D texture = new Texture2D(decoded.Width, decoded.Height, TextureFormat.RGBA32, false);
            texture.name = textureName;
            texture.SetPixels32(decoded.Pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(ToAbsolutePath(texturePath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
#if UNITY_2017_1_OR_NEWER
            importer.wrapModeU = mirrorTextures && primitive.MirrorS ? TextureWrapMode.Mirror : TextureWrapMode.Clamp;
            importer.wrapModeV = mirrorTextures && primitive.MirrorT ? TextureWrapMode.Mirror : TextureWrapMode.Clamp;
#else
            importer.wrapMode = TextureWrapMode.Clamp;
#endif
            importer.SaveAndReimport();
        }

#if UNITY_2017_1_OR_NEWER
        private static TextureWrapMode GetNativeWrapMode(bool mirror, bool clamp)
        {
            if (clamp)
                return TextureWrapMode.Clamp;
            return mirror ? TextureWrapMode.Mirror : TextureWrapMode.Repeat;
        }
#endif

        private static DecodedTexture BakeMirroredTexture(DecodedTexture source, bool mirrorX, bool mirrorY)
        {
            int sourceWidth = source.Width;
            int sourceHeight = source.Height;
            int targetWidth = mirrorX ? sourceWidth * 2 : sourceWidth;
            int targetHeight = mirrorY ? sourceHeight * 2 : sourceHeight;
            Color32[] targetPixels = new Color32[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                int sourceY = y;
                if (mirrorY && y >= sourceHeight)
                    sourceY = sourceHeight - 1 - (y - sourceHeight);

                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = x;
                    if (mirrorX && x >= sourceWidth)
                        sourceX = sourceWidth - 1 - (x - sourceWidth);

                    targetPixels[y * targetWidth + x] = source.Pixels[sourceY * sourceWidth + sourceX];
                }
            }

            DecodedTexture result = new DecodedTexture();
            result.Width = targetWidth;
            result.Height = targetHeight;
            result.Pixels = targetPixels;
            return result;
        }

        private static void AddTextureVariant(
            Dictionary<string, PrimitiveData> variants,
            int textureIndex,
            int tlut,
            int palette,
            bool mirrorS,
            bool mirrorT)
        {
            PrimitiveData variant = new PrimitiveData();
            variant.Texture = textureIndex;
            variant.Tlut = tlut;
            variant.Palette = palette;
            variant.ClampS = true;
            variant.ClampT = true;
            variant.MirrorS = mirrorS;
            variant.MirrorT = mirrorT;

            variants[GetTextureVariantKey(variant)] = variant;
        }

        private static bool AreAnimationTextureFramesCompatible(TextureRecord a, TextureRecord b)
        {
            return a.Format == b.Format &&
                   a.Size == b.Size &&
                   a.Width == b.Width &&
                   a.Height == b.Height;
        }

        private static string GetTextureVariantKey(PrimitiveData primitive)
        {
            return primitive.Texture + ":" +
                   primitive.Tlut + ":" +
                   primitive.Palette + ":" +
                   primitive.MirrorS + ":" +
                   primitive.MirrorT + ":" +
                   primitive.ClampS + ":" +
                   primitive.ClampT;
        }

        private static string BuildTextureVariantSuffix(PrimitiveData primitive)
        {
            string suffix = string.Empty;

            if (primitive.MirrorS)
                suffix += "_MirrorX";
            if (primitive.MirrorT)
                suffix += "_MirrorY";
            return suffix;
        }


        private static void FlipTextureY(DecodedTexture texture)
        {
            int w = texture.Width;
            int h = texture.Height;
            Color32[] p = texture.Pixels;
            for (int y = 0; y < h / 2; y++)
            {
                int a = y * w;
                int b = (h - 1 - y) * w;
                for (int x = 0; x < w; x++)
                {
                    Color32 t = p[a + x];
                    p[a + x] = p[b + x];
                    p[b + x] = t;
                }
            }
        }

        private static void FlipTextureX(DecodedTexture texture)
        {
            int w = texture.Width;
            int h = texture.Height;
            Color32[] p = texture.Pixels;
            for (int y = 0; y < h; y++)
            {
                int a = y * w;
                int b = a + w - 1;
                for (int x = 0; x < w / 2; x++)
                {
                    Color32 t = p[a + x];
                    p[a + x] = p[b - x];
                    p[b - x] = t;
                }
            }
        }

        private static int FindTlutForTexture(FragmentModel model, int texture)
        {
            for (int i = 0; i < model.Primitives.Count; i++) if (model.Primitives[i].Texture == texture) return model.Primitives[i].Tlut;
            return -1;
        }

        private static Mesh CreateMesh(FragmentModel model, PrimitiveData primitive, Transform[] bones, Transform root, bool includeSkinning, bool mirrorTextures, bool vertexColorsAsGrayscale, bool importVertexColors, float vertexColorLuminance, float animatedVertexColorStrength)
        {
            Mesh mesh = new Mesh();
            int count = primitive.Vertices.Count;
            Vector3[] vertices = new Vector3[count];
            Vector3[] normals = new Vector3[count];
            Vector2[] uv = new Vector2[count];
            Color32[] colors = new Color32[count];
            BoneWeight[] weights = new BoneWeight[count];
            int tw = primitive.Texture >= 0 && primitive.Texture < model.Textures.Count ? Math.Max(1, model.Textures[primitive.Texture].Width) : 32;
            int th = primitive.Texture >= 0 && primitive.Texture < model.Textures.Count ? Math.Max(1, model.Textures[primitive.Texture].Height) : 32;
            for (int i = 0; i < count; i++)
            {
                VertexData vertex = primitive.Vertices[i];
                int boneIndex = bones != null && bones.Length > 0
                    ? Mathf.Clamp(vertex.Bone, 0, bones.Length - 1)
                    : -1;

                Vector3 sourcePosition = vertex.Position * PositionScale;
                Vector3 sourceNormal = vertex.Normal.normalized;

                if (boneIndex >= 0 && bones[boneIndex] != null)
                {
                    Matrix4x4 boneToRoot = root.worldToLocalMatrix * bones[boneIndex].localToWorldMatrix;
                    vertices[i] = boneToRoot.MultiplyPoint3x4(sourcePosition);
                    normals[i] = boneToRoot.MultiplyVector(sourceNormal).normalized;
                }
                else
                {
                    vertices[i] = sourcePosition;
                    normals[i] = sourceNormal;
                }

                float u = vertex.UVRaw.x / tw;
                float v = 1f - vertex.UVRaw.y / th;

#if !UNITY_2017_1_OR_NEWER
                if (mirrorTextures && primitive.MirrorS)
                    u *= 0.5f;
                if (mirrorTextures && primitive.MirrorT)
                    v *= 0.5f;
#endif

                uv[i] = new Vector2(u, v);
                colors[i] = vertex.Color;

                if (includeSkinning && boneIndex >= 0)
                {
                    weights[i].boneIndex0 = boneIndex;
                    weights[i].weight0 = 1f;
                }
            }
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            if (importVertexColors && colors.Length == mesh.vertexCount && HasMeaningfulVertexColors(colors))
            {
                mesh.colors32 = colors;
                FixMeshVertexColors(
                    mesh,
                    vertexColorsAsGrayscale,
                    primitive.TextureAnimation >= 0 ? animatedVertexColorStrength : vertexColorLuminance);
            }
            mesh.triangles = primitive.Indices.ToArray();
            if (includeSkinning && bones != null && bones.Length > 0)
            {
                mesh.boneWeights = weights;
                Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; i++) bindPoses[i] = bones[i].worldToLocalMatrix * root.localToWorldMatrix;
                mesh.bindposes = bindPoses;
            }

            mesh.RecalculateBounds();

            return mesh;
        }

        private static void FixMeshVertexColors(
            Mesh mesh,
            bool grayscale,
            float luminanceMultiplier)
        {
            if (mesh == null)
                return;

            luminanceMultiplier = Mathf.Clamp(luminanceMultiplier, 0.01f, 1.0f);

            int vertexCount = mesh.vertexCount;
            if (vertexCount <= 0)
                return;

            Color32[] source = mesh.colors32;
            if (source == null || source.Length != vertexCount || !HasMeaningfulVertexColors(source))
                return;

            Color32[] fixedColors = new Color32[vertexCount];

            if (source.Length == vertexCount)
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    Color32 color = source[i];

                    if (grayscale)
                    {
                        byte gray = (byte)Mathf.Clamp(
                            Mathf.RoundToInt(
                                color.r * 0.299f +
                                color.g * 0.587f +
                                color.b * 0.114f),
                            0,
                            255);

                        gray = (byte)Mathf.RoundToInt(
                            Mathf.Lerp(255.0f, gray, luminanceMultiplier));

                        fixedColors[i] = new Color32(gray, gray, gray, color.a);
                    }
                    else
                    {
                        fixedColors[i] = new Color32(color.r, color.g, color.b, color.a);
                    }
                }
            }
            else
            {
                return;
            }

            mesh.colors32 = fixedColors;
            EditorUtility.SetDirty(mesh);
        }

        private static bool HasMeaningfulVertexColors(Color32[] colors)
        {
            if (colors == null || colors.Length == 0)
                return false;

            for (int i = 0; i < colors.Length; i++)
            {
                Color32 color = colors[i];
                if (color.r != 0 || color.g != 0 || color.b != 0 || color.a != 0)
                    return true;
            }

            return false;
        }

        private static void CreateCombinedPart(FragmentModel model, string folder, Transform root, Transform[] bones, bool skinned, List<PartBuildData> parts, bool vertexColorsAsGrayscale, bool importVertexColors, float vertexColorLuminance)
        {
            List<PartBuildData> sources = new List<PartBuildData>();
            for (int i = 0; i < parts.Count; i++)
                if (!parts[i].IsMaterialAnimated && parts[i].Mesh != null && parts[i].Renderer != null)
                    sources.Add(parts[i]);

            if (sources.Count < 2)
                return;

            Mesh combined = CombineMeshes(sources, bones, root, skinned, vertexColorsAsGrayscale, importVertexColors, vertexColorLuminance);
            combined.name = "Mesh_Combined";
            AssetDatabase.CreateAsset(combined, folder + "/Meshes/Mesh_Combined.asset");

            GameObject combinedObject = new GameObject("Part_Combined");
            combinedObject.transform.SetParent(root, false);
            Material[] combinedMaterials = new Material[sources.Count];
            for (int i = 0; i < sources.Count; i++)
                combinedMaterials[i] = sources[i].Material;

            if (skinned)
            {
                SkinnedMeshRenderer renderer = combinedObject.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = combined;
                renderer.bones = bones;
                renderer.rootBone = root;
                renderer.sharedMaterials = combinedMaterials;
            }
            else
            {
                MeshFilter filter = combinedObject.AddComponent<MeshFilter>();
                filter.sharedMesh = combined;
                MeshRenderer renderer = combinedObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = combinedMaterials;
            }

            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].Object != null)
                    UnityEngine.Object.DestroyImmediate(sources[i].Object);
                parts.Remove(sources[i]);
            }
        }

        private static Mesh CombineMeshes(List<PartBuildData> sources, Transform[] bones, Transform root, bool skinned, bool vertexColorsAsGrayscale, bool importVertexColors, float vertexColorLuminance)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<Color32> colors = new List<Color32>();
            List<BoneWeight> weights = new List<BoneWeight>();
            List<int[]> submeshes = new List<int[]>();

            for (int i = 0; i < sources.Count; i++)
            {
                Mesh mesh = sources[i].Mesh;
                int baseVertex = vertices.Count;
                vertices.AddRange(mesh.vertices);
                normals.AddRange(mesh.normals);
                uv.AddRange(mesh.uv);
                if (importVertexColors)
                {
                    Color32[] meshColors = mesh.colors32;
                    if (meshColors != null && meshColors.Length == mesh.vertexCount && HasMeaningfulVertexColors(meshColors))
                        colors.AddRange(meshColors);
                    else
                        importVertexColors = false;
                }
                if (skinned)
                    weights.AddRange(mesh.boneWeights);

                int[] indices = mesh.triangles;
                for (int n = 0; n < indices.Length; n++)
                    indices[n] += baseVertex;
                submeshes.Add(indices);
            }

            Mesh combined = new Mesh();
            combined.vertices = vertices.ToArray();
            combined.normals = normals.ToArray();
            combined.uv = uv.ToArray();
            if (importVertexColors && colors.Count == combined.vertexCount && HasMeaningfulVertexColors(colors.ToArray()))
            {
                combined.colors32 = colors.ToArray();
                FixMeshVertexColors(
                    combined,
                    vertexColorsAsGrayscale,
                    vertexColorLuminance);
            }
            combined.subMeshCount = submeshes.Count;
            for (int i = 0; i < submeshes.Count; i++)
                combined.SetTriangles(submeshes[i], i);

            if (skinned)
            {
                combined.boneWeights = weights.ToArray();
                Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                    bindPoses[i] = bones[i].worldToLocalMatrix * root.localToWorldMatrix;
                combined.bindposes = bindPoses;
            }

            combined.RecalculateBounds();
            return combined;
        }

        private static void AddMaterialAnimation(
            FragmentModel model,
            AnimationData source,
            AnimationClip clip,
            Transform root,
            List<PartBuildData> parts,
            Dictionary<string, Material> materials,
            bool mirrorTextures,
            PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode mode,
            MaterialCallbackContext callbackContext)
        {
            if (source.AuxAnimation < 0 ||
                source.AuxAnimation >= model.AuxAnimations.Count ||
                materials == null)
                return;

            if (mode == PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode.ByCallback)
            {
                AddMaterialAnimationCallbacks(
                    model,
                    source,
                    clip,
                    parts,
                    materials,
                    mirrorTextures,
                    callbackContext);
            }
            else
            {
                AddMaterialAnimationCurves(
                    model,
                    source,
                    clip,
                    root,
                    parts,
                    materials,
                    mirrorTextures);
            }
        }

        private static void AddMaterialAnimationCurves(
            FragmentModel model,
            AnimationData source,
            AnimationClip clip,
            Transform root,
            List<PartBuildData> parts,
            Dictionary<string, Material> materials,
            bool mirrorTextures)
        {
            AuxAnimationData auxiliary = model.AuxAnimations[source.AuxAnimation];

            for (int i = 0; i < parts.Count; i++)
            {
                PartBuildData part = parts[i];
                int channel = part.Primitive.TextureAnimation;
                if (channel < 0 || channel >= auxiliary.Channels.Length || part.Renderer == null)
                    continue;

                int[] track = auxiliary.Channels[channel];
                if (track == null || track.Length == 0)
                    continue;

                List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();
                Material previous = null;

                for (int frame = 0; frame < track.Length; frame++)
                {
                    int textureIndex = track[frame];
                    Material material = FindAnimatedMaterial(materials, part.Primitive, textureIndex, mirrorTextures);
                    if (material == null || material == previous)
                        continue;

                    ObjectReferenceKeyframe key = new ObjectReferenceKeyframe();
                    key.time = frame / 30f;
                    key.value = material;
                    keys.Add(key);
                    previous = material;
                }

                if (keys.Count == 0)
                    continue;

                EditorCurveBinding binding = new EditorCurveBinding();
                binding.path = AnimationUtility.CalculateTransformPath(part.Renderer.transform, root);
                binding.type = part.Renderer.GetType();
                binding.propertyName = "m_Materials.Array.data[0]";
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
            }
        }

        private static void AddMaterialAnimationCallbacks(
            FragmentModel model,
            AnimationData source,
            AnimationClip clip,
            List<PartBuildData> parts,
            Dictionary<string, Material> materials,
            bool mirrorTextures,
            MaterialCallbackContext context)
        {
            if (context == null || context.Swapper == null)
                return;

            AuxAnimationData auxiliary = model.AuxAnimations[source.AuxAnimation];
            int frameCount = Math.Max(1, auxiliary.FrameCount);
            int previousSet = -1;
            List<AnimationEvent> events = new List<AnimationEvent>();

            for (int frame = 0; frame < frameCount; frame++)
            {
                List<MaterialStateEntry> state = new List<MaterialStateEntry>();

                for (int i = 0; i < parts.Count; i++)
                {
                    PartBuildData part = parts[i];
                    int channel = part.Primitive.TextureAnimation;
                    if (channel < 0 || channel >= auxiliary.Channels.Length || part.Renderer == null)
                        continue;

                    int[] track = auxiliary.Channels[channel];
                    if (track == null || track.Length == 0)
                        continue;

                    int textureIndex = track[Math.Min(frame, track.Length - 1)];
                    Material material = FindAnimatedMaterial(materials, part.Primitive, textureIndex, mirrorTextures);
                    if (material == null || material.mainTexture == null)
                        continue;

                    MaterialStateEntry entry = new MaterialStateEntry();
                    entry.Part = part;
                    entry.Texture = material.mainTexture;
                    state.Add(entry);
                }

                if (state.Count == 0)
                    continue;

                int setIndex = GetOrCreateTextureSwapSet(context, state);
                if (setIndex == previousSet)
                    continue;

                AnimationEvent animationEvent = new AnimationEvent();
                animationEvent.time = frame / 30f;
                animationEvent.functionName = "SwapSet";
                animationEvent.floatParameter = setIndex;
                events.Add(animationEvent);
                previousSet = setIndex;
            }

            if (events.Count > 0)
                AnimationUtility.SetAnimationEvents(clip, events.ToArray());
        }

        private static int GetOrCreateTextureSwapSet(
            MaterialCallbackContext context,
            List<MaterialStateEntry> state)
        {
            state.Sort(delegate(MaterialStateEntry a, MaterialStateEntry b)
            {
                return a.Part.PrimitiveIndex.CompareTo(b.Part.PrimitiveIndex);
            });

            StringBuilder keyBuilder = new StringBuilder();
            for (int i = 0; i < state.Count; i++)
            {
                keyBuilder.Append(state[i].Part.PrimitiveIndex);
                keyBuilder.Append(':');
                keyBuilder.Append(state[i].Texture != null ? state[i].Texture.GetInstanceID() : 0);
                keyBuilder.Append(';');
            }

            string key = keyBuilder.ToString();
            int existing;
            if (context.SetIndices.TryGetValue(key, out existing))
                return existing;

            PS3DS_TextureSwapper.TextureSwapSet set = new PS3DS_TextureSwapper.TextureSwapSet();
            set.Entries = new PS3DS_TextureSwapper.TextureSwapEntry[state.Count];

            for (int i = 0; i < state.Count; i++)
            {
                PS3DS_TextureSwapper.TextureSwapEntry entry =
                    new PS3DS_TextureSwapper.TextureSwapEntry();
                entry.Renderer = state[i].Part.Renderer;
                entry.ToAllMaterials = false;
                entry.MaterialIndices = new int[] { 0 };
                entry.Texture = state[i].Texture;
                entry.UseTexture = true;
                entry.UseUV = false;
                entry.Offset = Vector2.zero;
                entry.Scale = Vector2.one;
                set.Entries[i] = entry;
            }

            int index = context.Sets.Count;
            context.Sets.Add(set);
            context.SetIndices.Add(key, index);
            return index;
        }

        private static void AssignTextureSwapSets(MaterialCallbackContext context)
        {
            if (context == null || context.Swapper == null)
                return;

            FieldInfo setsField = typeof(PS3DS_TextureSwapper).GetField(
                "m_sets",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (setsField == null)
            {
                Debug.LogWarning("[Stadium2Unity] Could not assign PS3DS_TextureSwapper sets because m_sets was not found.");
                return;
            }

            setsField.SetValue(context.Swapper, context.Sets.ToArray());
            EditorUtility.SetDirty(context.Swapper);
        }

        private static Material FindAnimatedMaterial(Dictionary<string, Material> materials, PrimitiveData source,
            int textureIndex, bool mirrorTextures)
        {
            PrimitiveData key = new PrimitiveData();
            key.Texture = textureIndex;
            key.Tlut = source.Tlut;
            key.ClampS = true;
            key.ClampT = true;
            key.Palette = 0;
            key.MirrorS = mirrorTextures && source.MirrorS;
            key.MirrorT = mirrorTextures && source.MirrorT;

            Material material;
            if (materials.TryGetValue(GetTextureVariantKey(key), out material))
                return material;

            key.MirrorS = false;
            key.MirrorT = false;
            materials.TryGetValue(GetTextureVariantKey(key), out material);
            return material;
        }

        private static AnimationClip CreateClip(
            FragmentModel model,
            AnimationData source,
            Transform root,
            Transform[] pivots,
            Transform[] joints,
            bool legacy,
            List<PartBuildData> parts,
            Dictionary<string, Material> materials,
            bool mirrorTextures,
            PS3DS_PokemonStadiumModelImporter.MaterialAnimationMode materialAnimationMode,
            MaterialCallbackContext callbackContext)
        {
            AnimationClip clip = new AnimationClip();
            clip.frameRate = 30f;
            clip.legacy = legacy;
            for (int i = 0; i < model.Bones.Count; i++)
            {
                TrackData track = source.Tracks[i]; if (track == null) continue;
                string pivotPath = AnimationUtility.CalculateTransformPath(pivots[i], root);
                string jointPath = AnimationUtility.CalculateTransformPath(joints[i], root);
                AnimationCurve px = new AnimationCurve(), py = new AnimationCurve(), pz = new AnimationCurve();
                AnimationCurve qx = new AnimationCurve(), qy = new AnimationCurve(), qz = new AnimationCurve(), qw = new AnimationCurve();
                AnimationCurve sx = new AnimationCurve(), sy = new AnimationCurve(), sz = new AnimationCurve();
                Quaternion previous = Quaternion.identity;
                for (int frame = 0; frame < source.FrameCount; frame++)
                {
                    float time = frame / 30f;
                    Vector3 position = track.Translation[frame] * PositionScale;
                    Quaternion rotation = RotationFromGameUnits(track.RotationUnits[frame]);
                    if (frame > 0 && Quaternion.Dot(previous, rotation) < 0f) rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    previous = rotation;
                    Vector3 scale = track.Scale[frame];
                    px.AddKey(time, position.x); py.AddKey(time, position.y); pz.AddKey(time, position.z);
                    qx.AddKey(time, rotation.x); qy.AddKey(time, rotation.y); qz.AddKey(time, rotation.z); qw.AddKey(time, rotation.w);
                    sx.AddKey(time, scale.x); sy.AddKey(time, scale.y); sz.AddKey(time, scale.z);
                }
                clip.SetCurve(pivotPath, typeof(Transform), "localPosition.x", px); clip.SetCurve(pivotPath, typeof(Transform), "localPosition.y", py); clip.SetCurve(pivotPath, typeof(Transform), "localPosition.z", pz);
                clip.SetCurve(pivotPath, typeof(Transform), "localRotation.x", qx); clip.SetCurve(pivotPath, typeof(Transform), "localRotation.y", qy); clip.SetCurve(pivotPath, typeof(Transform), "localRotation.z", qz); clip.SetCurve(pivotPath, typeof(Transform), "localRotation.w", qw);
                clip.SetCurve(jointPath, typeof(Transform), "localScale.x", sx); clip.SetCurve(jointPath, typeof(Transform), "localScale.y", sy); clip.SetCurve(jointPath, typeof(Transform), "localScale.z", sz);
            }
            AddMaterialAnimation(
                model,
                source,
                clip,
                root,
                parts,
                materials,
                mirrorTextures,
                materialAnimationMode,
                callbackContext);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = source.LoopStart < source.FrameCount; AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity(); return clip;
        }

        private static Quaternion RotationFromGameUnits(Vector3 r)
        {
            double x = r.x / 32768.0 * Math.PI, y = r.y / 32768.0 * Math.PI, z = r.z / 32768.0 * Math.PI;
            double sx = Math.Sin(x), cx = Math.Cos(x), sy = Math.Sin(y), cy = Math.Cos(y), sz = Math.Sin(z), cz = Math.Cos(z);
            double m00 = cy * cz, m01 = sx * sy * cz - cx * sz, m02 = cx * sy * cz + sx * sz;
            double m10 = cy * sz, m11 = sx * sy * sz + cx * cz, m12 = cx * sy * sz - sx * cz;
            double m20 = -sy, m21 = sx * cy, m22 = cx * cy;
            double tr = m00 + m11 + m22; double qx, qy, qz, qw, s;
            if (tr > 0.0) { s = Math.Sqrt(tr + 1.0) * 2.0; qw = 0.25 * s; qx = (m21 - m12) / s; qy = (m02 - m20) / s; qz = (m10 - m01) / s; }
            else if (m00 > m11 && m00 > m22) { s = Math.Sqrt(1.0 + m00 - m11 - m22) * 2.0; qw = (m21 - m12) / s; qx = 0.25 * s; qy = (m01 + m10) / s; qz = (m02 + m20) / s; }
            else if (m11 > m22) { s = Math.Sqrt(1.0 + m11 - m00 - m22) * 2.0; qw = (m02 - m20) / s; qx = (m01 + m10) / s; qy = 0.25 * s; qz = (m12 + m21) / s; }
            else { s = Math.Sqrt(1.0 + m22 - m00 - m11) * 2.0; qw = (m10 - m01) / s; qx = (m02 + m20) / s; qy = (m12 + m21) / s; qz = 0.25 * s; }
            Quaternion q = new Quaternion((float)qx, (float)qy, (float)qz, (float)qw); float length = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w); if (length > 0f) { q.x /= length; q.y /= length; q.z /= length; q.w /= length; }
            return q;
        }

        internal static DecodedTexture DecodeTextureForJson(FragmentModel model, int index, int tlutIndex) { return DecodeTexture(model, index, tlutIndex, 0); }

        private static DecodedTexture DecodeTexture(FragmentModel model, int index, int tlutIndex, int palette)
        {
            TextureRecord texture = model.Textures[index]; FragmentReader f = model.Reader; int width = Math.Max(1, texture.Width), height = Math.Max(1, texture.Height), count = width * height;
            Color32[] pixels = new Color32[count]; Color32[] paletteColors = null;
            if (texture.Format == 2)
            {
                int entries = texture.Size == 0 ? 16 : 256; paletteColors = new Color32[entries];
                int baseOffset = tlutIndex >= 0 && tlutIndex < model.Tluts.Count ? model.Tluts[tlutIndex].DataOffset : -1;
                if (baseOffset >= 0)
                {
                    if (texture.Size == 0) baseOffset += palette * 16 * 2;
                    int requiredEnd = baseOffset + entries * 2;
                    if (baseOffset < 0 || requiredEnd > f.Data.Length)
                        throw new InvalidDataException("TLUT palette read is outside the fragment. Texture=" + index + ", TLUT=" + tlutIndex + ", palette=" + palette + ", base=0x" + baseOffset.ToString("X") + ", requiredEnd=0x" + requiredEnd.ToString("X") + ", fragmentSize=0x" + f.Data.Length.ToString("X") + ".");
                    for (int i = 0; i < entries; i++) paletteColors[i] = Rgba5551(f.U16(baseOffset + i * 2));
                }
                else for (int i = 0; i < entries; i++) paletteColors[i] = new Color32(255, 0, 255, 255);
            }
            for (int i = 0; i < count; i++)
            {
                Color32 color;
                if (texture.Format == 0 && texture.Size == 2) color = Rgba5551(f.U16(texture.DataOffset + i * 2));
                else if (texture.Format == 0 && texture.Size == 3) { int o = texture.DataOffset + i * 4; color = new Color32(f.U8(o), f.U8(o + 1), f.U8(o + 2), f.U8(o + 3)); }
                else if (texture.Format == 2) { int value = texture.Size == 0 ? ((f.U8(texture.DataOffset + i / 2) >> ((i & 1) != 0 ? 0 : 4)) & 15) : f.U8(texture.DataOffset + i); color = paletteColors[value % paletteColors.Length]; }
                else if (texture.Format == 3)
                {
                    int l, a;
                    if (texture.Size == 2) { ushort v = f.U16(texture.DataOffset + i * 2); l = v >> 8; a = v & 255; }
                    else if (texture.Size == 1) { int v = f.U8(texture.DataOffset + i); l = (v >> 4) * 17; a = (v & 15) * 17; }
                    else { int v = (f.U8(texture.DataOffset + i / 2) >> ((i & 1) != 0 ? 0 : 4)) & 15; l = ((v >> 1) * 255) / 7; a = (v & 1) != 0 ? 255 : 0; }
                    color = new Color32((byte)l, (byte)l, (byte)l, (byte)a);
                }
                else if (texture.Format == 4) { int l = texture.Size == 1 ? f.U8(texture.DataOffset + i) : (((f.U8(texture.DataOffset + i / 2) >> ((i & 1) != 0 ? 0 : 4)) & 15) * 17); color = new Color32((byte)l, (byte)l, (byte)l, 255); }
                else color = new Color32(255, 0, 255, 255);
                pixels[i] = color;
            }
            DecodedTexture result = new DecodedTexture(); result.Width = width; result.Height = height; result.Pixels = pixels; return result;
        }

        private static Color32 Rgba5551(ushort p)
        {
            return new Color32((byte)(((p >> 11) & 31) * 255 / 31), (byte)(((p >> 6) & 31) * 255 / 31), (byte)(((p >> 1) & 31) * 255 / 31), (byte)((p & 1) != 0 ? 255 : 0));
        }
        internal static string AssetPathToAbsolute(string assetPath) { return ToAbsolutePath(assetPath); }
        private static string ToAbsolutePath(string assetPath) { return Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + assetPath; }
        private static string Sanitize(string value) { foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_'); return value.Replace("'", string.Empty); }
    }


    internal static class PipelineJsonWriter
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static void Write(FragmentModel model, string assetPath, int fileIndex)
        {
            Dictionary<int, int> textureSlots = new Dictionary<int, int>();
            List<int> usedTextures = new List<int>();
            for (int i = 0; i < model.Primitives.Count; i++)
            {
                PrimitiveData primitive = model.Primitives[i];
                if (primitive.Indices.Count == 0 || primitive.Texture < 0 || primitive.Texture >= model.Textures.Count)
                    continue;
                if (!textureSlots.ContainsKey(primitive.Texture))
                {
                    textureSlots.Add(primitive.Texture, usedTextures.Count);
                    usedTextures.Add(primitive.Texture);
                }
            }

            StringBuilder json = new StringBuilder(1024 * 64);
            json.Append('{');
            Property(json, "species"); json.Append(model.Species); json.Append(',');
            Property(json, "name"); String(json, model.Name); json.Append(',');
            Property(json, "file"); String(json, fileIndex.ToString(Invariant) + ".bin"); json.Append(',');
            Property(json, "rootScale"); Vector(json, model.RootScale, 10); json.Append(',');

            Property(json, "bones"); json.Append('[');
            for (int i = 0; i < model.Bones.Count; i++)
            {
                if (i > 0) json.Append(',');
                BoneData bone = model.Bones[i];
                json.Append('{');
                Property(json, "parent"); json.Append(bone.Parent); json.Append(',');
                Property(json, "boneId"); json.Append(bone.BoneId); json.Append(',');
                Property(json, "chan"); json.Append(bone.Channel); json.Append(',');
                Property(json, "flags"); json.Append((int)bone.Flags); json.Append(',');
                Property(json, "t"); Vector(json, bone.Translation, 0); json.Append(',');
                Property(json, "r"); Vector(json, bone.RotationUnits, 0); json.Append(',');
                Property(json, "s"); Vector(json, bone.Scale, 10);
                json.Append('}');
            }
            json.Append(']'); json.Append(',');

            Property(json, "textures"); json.Append('[');
            for (int i = 0; i < usedTextures.Count; i++)
            {
                if (i > 0) json.Append(',');
                int textureIndex = usedTextures[i];
                DecodedTexture decoded = UnityModelWriter.DecodeTextureForJson(model, textureIndex, FindTlut(model, textureIndex));
                Texture2D texture = new Texture2D(decoded.Width, decoded.Height, TextureFormat.RGBA32, false);
                texture.SetPixels32(decoded.Pixels);
                texture.Apply(false, false);
                string base64 = Convert.ToBase64String(texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                json.Append('{');
                Property(json, "index"); json.Append(textureIndex); json.Append(',');
                Property(json, "w"); json.Append(decoded.Width); json.Append(',');
                Property(json, "h"); json.Append(decoded.Height); json.Append(',');
                Property(json, "png"); String(json, "data:image/png;base64," + base64);
                json.Append('}');
            }
            json.Append(']'); json.Append(',');

            Property(json, "prims"); json.Append('[');
            bool firstPrimitive = true;
            for (int i = 0; i < model.Primitives.Count; i++)
            {
                PrimitiveData primitive = model.Primitives[i];
                if (primitive.Indices.Count == 0) continue;
                if (!firstPrimitive) json.Append(',');
                firstPrimitive = false;
                int textureSlot = -1;
                if (primitive.Texture >= 0) textureSlots.TryGetValue(primitive.Texture, out textureSlot);
                json.Append('{');
                Property(json, "tex"); json.Append(textureSlot); json.Append(',');
                Property(json, "cull"); json.Append(primitive.Cull); json.Append(',');
                Property(json, "texAnim"); json.Append(-1); json.Append(',');
                Property(json, "texMap"); json.Append("{},");
                Property(json, "pos"); FloatVertexArray(json, primitive, 0); json.Append(',');
                Property(json, "uv"); UvArray(json, model, primitive); json.Append(',');
                Property(json, "nrm"); FloatVertexArray(json, primitive, 1); json.Append(',');
                Property(json, "col"); ColorArray(json, primitive); json.Append(',');
                Property(json, "skin"); SkinArray(json, primitive); json.Append(',');
                Property(json, "idx"); IntArray(json, primitive.Indices);
                json.Append('}');
            }
            json.Append(']'); json.Append(',');

            Property(json, "anims"); json.Append('[');
            for (int i = 0; i < model.Animations.Count; i++)
            {
                if (i > 0) json.Append(',');
                AnimationData animation = model.Animations[i];
                json.Append('{');
                Property(json, "index"); json.Append(animation.Index); json.Append(',');
                Property(json, "frames"); json.Append(animation.FrameCount); json.Append(',');
                Property(json, "flags"); json.Append(0); json.Append(',');
                Property(json, "channels"); json.Append(model.Bones.Count * 3); json.Append(',');
                Property(json, "loopStart"); json.Append(animation.LoopStart); json.Append(',');
                Property(json, "tracks"); json.Append('[');
                for (int b = 0; b < model.Bones.Count; b++)
                {
                    if (b > 0) json.Append(',');
                    TrackData track = animation.Tracks[b];
                    if (track == null)
                    {
                        json.Append("null");
                        continue;
                    }
                    json.Append('{');
                    Property(json, "t"); TrackVector(json, track.Translation, 3); json.Append(',');
                    Property(json, "r"); TrackVector(json, track.RotationUnits, 0); json.Append(',');
                    Property(json, "s"); TrackVector(json, track.Scale, 5);
                    json.Append('}');
                }
                json.Append(']');
                json.Append('}');
            }
            json.Append(']'); json.Append(',');
            Property(json, "auxAnims"); json.Append("[],");
            Property(json, "fx"); json.Append("[],");
            Property(json, "warnings"); json.Append("[]");
            json.Append('}');

            File.WriteAllText(UnityModelWriter.AssetPathToAbsolute(assetPath), json.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static int FindTlut(FragmentModel model, int texture)
        {
            for (int i = 0; i < model.Primitives.Count; i++)
                if (model.Primitives[i].Texture == texture) return model.Primitives[i].Tlut;
            return -1;
        }

        private static void FloatVertexArray(StringBuilder json, PrimitiveData primitive, int kind)
        {
            json.Append('[');
            for (int i = 0; i < primitive.Vertices.Count; i++)
            {
                if (i > 0) json.Append(',');
                Vector3 value = kind == 0 ? primitive.Vertices[i].Position : primitive.Vertices[i].Normal;
                Number(json, value.x, kind == 0 ? 0 : 10); json.Append(',');
                Number(json, value.y, kind == 0 ? 0 : 10); json.Append(',');
                Number(json, value.z, kind == 0 ? 0 : 10);
            }
            json.Append(']');
        }

        private static void UvArray(StringBuilder json, FragmentModel model, PrimitiveData primitive)
        {
            int width = primitive.Texture >= 0 && primitive.Texture < model.Textures.Count ? Math.Max(1, model.Textures[primitive.Texture].Width) : 32;
            int height = primitive.Texture >= 0 && primitive.Texture < model.Textures.Count ? Math.Max(1, model.Textures[primitive.Texture].Height) : 32;
            json.Append('[');
            for (int i = 0; i < primitive.Vertices.Count; i++)
            {
                if (i > 0) json.Append(',');
                Number(json, primitive.Vertices[i].UVRaw.x / width, 10); json.Append(',');
                Number(json, primitive.Vertices[i].UVRaw.y / height, 10);
            }
            json.Append(']');
        }

        private static void ColorArray(StringBuilder json, PrimitiveData primitive)
        {
            json.Append('[');
            for (int i = 0; i < primitive.Vertices.Count; i++)
            {
                if (i > 0)
                    json.Append(',');

                Color32 color = primitive.Vertices[i].Color;
                json.Append(color.r); json.Append(',');
                json.Append(color.g); json.Append(',');
                json.Append(color.b); json.Append(',');
                json.Append(color.a);
            }
            json.Append(']');
        }

        private static void SkinArray(StringBuilder json, PrimitiveData primitive)
        {
            json.Append('[');
            for (int i = 0; i < primitive.Vertices.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append(primitive.Vertices[i].Bone);
            }
            json.Append(']');
        }

        private static void IntArray(StringBuilder json, List<int> values)
        {
            json.Append('[');
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append(values[i]);
            }
            json.Append(']');
        }

        private static void TrackVector(StringBuilder json, Vector3[] values, int decimals)
        {
            json.Append('[');
            AxisTrack(json, values, 0, decimals); json.Append(',');
            AxisTrack(json, values, 1, decimals); json.Append(',');
            AxisTrack(json, values, 2, decimals);
            json.Append(']');
        }

        private static void AxisTrack(StringBuilder json, Vector3[] values, int axis, int decimals)
        {
            float first = Round(Get(values[0], axis), decimals);
            bool constant = true;
            for (int i = 1; i < values.Length; i++)
            {
                if (Round(Get(values[i], axis), decimals) != first) { constant = false; break; }
            }
            if (constant)
            {
                Number(json, first, decimals);
                return;
            }
            json.Append('[');
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) json.Append(',');
                Number(json, Get(values[i], axis), decimals);
            }
            json.Append(']');
        }

        private static float Get(Vector3 value, int axis) { return axis == 0 ? value.x : axis == 1 ? value.y : value.z; }
        private static float Round(float value, int decimals) { return (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero); }
        private static void Vector(StringBuilder json, Vector3 value, int decimals)
        {
            json.Append('['); Number(json, value.x, decimals); json.Append(','); Number(json, value.y, decimals); json.Append(','); Number(json, value.z, decimals); json.Append(']');
        }
        private static void Number(StringBuilder json, float value, int decimals)
        {
            double rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
            json.Append(rounded.ToString(decimals == 0 ? "0" : "0.################", Invariant));
        }
        private static void Property(StringBuilder json, string name) { String(json, name); json.Append(':'); }
        private static void String(StringBuilder json, string value)
        {
            json.Append('"');
            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    if (c == '"' || c == '\\') { json.Append('\\'); json.Append(c); }
                    else if (c == '\n') json.Append("\\n");
                    else if (c == '\r') json.Append("\\r");
                    else if (c == '\t') json.Append("\\t");
                    else json.Append(c);
                }
            }
            json.Append('"');
        }
    }

    internal static class SpeciesNames
    {
        private static readonly string[] Names = ("Unknown Bulbasaur Ivysaur Venusaur Charmander Charmeleon Charizard Squirtle Wartortle Blastoise Caterpie Metapod Butterfree Weedle Kakuna Beedrill Pidgey Pidgeotto Pidgeot Rattata Raticate Spearow Fearow Ekans Arbok Pikachu Raichu Sandshrew Sandslash NidoranF Nidorina Nidoqueen NidoranM Nidorino Nidoking Clefairy Clefable Vulpix Ninetales Jigglypuff Wigglytuff Zubat Golbat Oddish Gloom Vileplume Paras Parasect Venonat Venomoth Diglett Dugtrio Meowth Persian Psyduck Golduck Mankey Primeape Growlithe Arcanine Poliwag Poliwhirl Poliwrath Abra Kadabra Alakazam Machop Machoke Machamp Bellsprout Weepinbell Victreebel Tentacool Tentacruel Geodude Graveler Golem Ponyta Rapidash Slowpoke Slowbro Magnemite Magneton Farfetchd Doduo Dodrio Seel Dewgong Grimer Muk Shellder Cloyster Gastly Haunter Gengar Onix Drowzee Hypno Krabby Kingler Voltorb Electrode Exeggcute Exeggutor Cubone Marowak Hitmonlee Hitmonchan Lickitung Koffing Weezing Rhyhorn Rhydon Chansey Tangela Kangaskhan Horsea Seadra Goldeen Seaking Staryu Starmie MrMime Scyther Jynx Electabuzz Magmar Pinsir Tauros Magikarp Gyarados Lapras Ditto Eevee Vaporeon Jolteon Flareon Porygon Omanyte Omastar Kabuto Kabutops Aerodactyl Snorlax Articuno Zapdos Moltres Dratini Dragonair Dragonite Mewtwo Mew").Split(' ');
        public static string Get(int species) { return species >= 1 && species < Names.Length ? Names[species] : "Species" + species; }

        public static string[] GetMainPokemonDisplayNames()
        {
            string[] result = new string[151];
            for (int i = 1; i <= 151; i++)
                result[i - 1] = i.ToString("000") + " - " + Get(i);
            return result;
        }
    }
}
#endif
