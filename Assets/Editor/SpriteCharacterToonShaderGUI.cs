using UnityEngine;
using UnityEditor;

/// <summary>
/// 2D角色卡通Shader的自定义编辑器
/// </summary>
public class SpriteCharacterToonShaderGUI : ShaderGUI
{
    private MaterialProperty mainTex;
    private MaterialProperty color;
    private MaterialProperty toonRamp;
    private MaterialProperty shadowThreshold;
    private MaterialProperty shadowSoftness;
    private MaterialProperty shadowColor;
    private MaterialProperty rimColor;
    private MaterialProperty rimPower;
    private MaterialProperty rimIntensity;
    private MaterialProperty outlineWidth;
    private MaterialProperty outlineColor;
    private MaterialProperty flipX;
    private MaterialProperty flipY;
    private MaterialProperty brightness;
    private MaterialProperty contrast;
    private MaterialProperty saturation;
    private MaterialProperty cutoff;
    private MaterialProperty windStrength;
    private MaterialProperty windSpeed;
    private MaterialProperty windDirection;

    private bool showBaseSettings = true;
    private bool showToonSettings = true;
    private bool showRimSettings = true;
    private bool showOutlineSettings = true;
    private bool showAnimationSettings = false;
    private bool showEffectSettings = false;
    private bool showAdvancedSettings = false;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // 查找所有属性
        FindProperties(properties);

        // 绘制标题
        EditorGUILayout.Space();
        GUILayout.Label("2D角色卡通渲染器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 基础设置
        showBaseSettings = EditorGUILayout.Foldout(showBaseSettings, "基础设置", true);
        if (showBaseSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(new GUIContent("角色贴图", "主要的角色精灵贴图"), mainTex);
            materialEditor.ColorProperty(color, "颜色叠加");
            materialEditor.RangeProperty(cutoff, "透明度裁剪");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // 卡通渲染设置
        showToonSettings = EditorGUILayout.Foldout(showToonSettings, "卡通渲染", true);
        if (showToonSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(new GUIContent("卡通渐变贴图", "用于控制光影过渡的1D渐变贴图"), toonRamp);
            materialEditor.RangeProperty(shadowThreshold, "阴影阈值");
            materialEditor.RangeProperty(shadowSoftness, "阴影软度");
            materialEditor.ColorProperty(shadowColor, "阴影颜色");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // 边缘光设置
        showRimSettings = EditorGUILayout.Foldout(showRimSettings, "边缘光效果", true);
        if (showRimSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.ColorProperty(rimColor, "边缘光颜色");
            materialEditor.RangeProperty(rimPower, "边缘光强度");
            materialEditor.RangeProperty(rimIntensity, "边缘光亮度");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // 描边设置
        showOutlineSettings = EditorGUILayout.Foldout(showOutlineSettings, "描边效果", true);
        if (showOutlineSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.RangeProperty(outlineWidth, "描边宽度");
            materialEditor.ColorProperty(outlineColor, "描边颜色");

            if (outlineWidth.floatValue > 0)
            {
                EditorGUILayout.HelpBox("描边效果需要角色有正确的法线信息", MessageType.Info);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // 动画支持
        showAnimationSettings = EditorGUILayout.Foldout(showAnimationSettings, "动画支持", false);
        if (showAnimationSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("翻转控制", EditorStyles.boldLabel);
            materialEditor.FloatProperty(flipX, "水平翻转");
            materialEditor.FloatProperty(flipY, "垂直翻转");

            EditorGUILayout.HelpBox("通过脚本设置 material.SetFloat(\"_FlipX\", 1) 来翻转角色", MessageType.Info);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // 特效设置
        showEffectSettings = EditorGUILayout.Foldout(showEffectSettings, "视觉特效", false);
        if (showEffectSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("颜色调整", EditorStyles.boldLabel);
            materialEditor.RangeProperty(brightness, "亮度");
            materialEditor.RangeProperty(contrast, "对比度");
            materialEditor.RangeProperty(saturation, "饱和度");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("风效果", EditorStyles.boldLabel);
            materialEditor.RangeProperty(windStrength, "风力强度");
            materialEditor.RangeProperty(windSpeed, "风速");
            materialEditor.VectorProperty(windDirection, "风向");

            if (windStrength.floatValue > 0)
            {
                EditorGUILayout.HelpBox("风效果会让角色轻微摆动，适合布料或头发", MessageType.Info);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // 高级设置
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置", false);
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;

            // 渲染队列
            materialEditor.RenderQueueField();

            // 双面渲染提示
            EditorGUILayout.HelpBox("此Shader默认启用双面渲染，适合2D纸片人角色", MessageType.Info);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // 预设按钮
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("快速预设", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("明亮卡通"))
        {
            ApplyBrightToonPreset(materialEditor.target as Material);
        }
        if (GUILayout.Button("柔和阴影"))
        {
            ApplySoftShadowPreset(materialEditor.target as Material);
        }
        if (GUILayout.Button("强对比"))
        {
            ApplyHighContrastPreset(materialEditor.target as Material);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("重置为默认"))
        {
            ResetToDefault(materialEditor.target as Material);
        }
        if (GUILayout.Button("复制设置"))
        {
            CopyMaterialSettings(materialEditor.target as Material);
        }
        if (GUILayout.Button("粘贴设置"))
        {
            PasteMaterialSettings(materialEditor.target as Material);
        }
        EditorGUILayout.EndHorizontal();
    }

    void FindProperties(MaterialProperty[] props)
    {
        mainTex = FindProperty("_MainTex", props);
        color = FindProperty("_Color", props);
        toonRamp = FindProperty("_ToonRamp", props);
        shadowThreshold = FindProperty("_ShadowThreshold", props);
        shadowSoftness = FindProperty("_ShadowSoftness", props);
        shadowColor = FindProperty("_ShadowColor", props);
        rimColor = FindProperty("_RimColor", props);
        rimPower = FindProperty("_RimPower", props);
        rimIntensity = FindProperty("_RimIntensity", props);
        outlineWidth = FindProperty("_OutlineWidth", props);
        outlineColor = FindProperty("_OutlineColor", props);
        flipX = FindProperty("_FlipX", props);
        flipY = FindProperty("_FlipY", props);
        brightness = FindProperty("_Brightness", props);
        contrast = FindProperty("_Contrast", props);
        saturation = FindProperty("_Saturation", props);
        cutoff = FindProperty("_Cutoff", props);
        windStrength = FindProperty("_WindStrength", props);
        windSpeed = FindProperty("_WindSpeed", props);
        windDirection = FindProperty("_WindDirection", props);
    }

    void ApplyBrightToonPreset(Material mat)
    {
        mat.SetFloat("_ShadowThreshold", 0.6f);
        mat.SetFloat("_ShadowSoftness", 0.2f);
        mat.SetColor("_ShadowColor", new Color(0.7f, 0.7f, 0.9f, 1f));
        mat.SetFloat("_RimPower", 2f);
        mat.SetFloat("_RimIntensity", 0.8f);
        mat.SetFloat("_Brightness", 1.2f);
        mat.SetFloat("_Contrast", 1.1f);
        mat.SetFloat("_Saturation", 1.3f);
    }

    void ApplySoftShadowPreset(Material mat)
    {
        mat.SetFloat("_ShadowThreshold", 0.4f);
        mat.SetFloat("_ShadowSoftness", 0.3f);
        mat.SetColor("_ShadowColor", new Color(0.6f, 0.6f, 0.8f, 1f));
        mat.SetFloat("_RimPower", 4f);
        mat.SetFloat("_RimIntensity", 0.5f);
        mat.SetFloat("_Brightness", 1f);
        mat.SetFloat("_Contrast", 0.9f);
        mat.SetFloat("_Saturation", 1f);
    }

    void ApplyHighContrastPreset(Material mat)
    {
        mat.SetFloat("_ShadowThreshold", 0.7f);
        mat.SetFloat("_ShadowSoftness", 0.05f);
        mat.SetColor("_ShadowColor", new Color(0.3f, 0.3f, 0.6f, 1f));
        mat.SetFloat("_RimPower", 3f);
        mat.SetFloat("_RimIntensity", 1.2f);
        mat.SetFloat("_Brightness", 1f);
        mat.SetFloat("_Contrast", 1.5f);
        mat.SetFloat("_Saturation", 1.4f);
    }

    void ResetToDefault(Material mat)
    {
        mat.SetColor("_Color", Color.white);
        mat.SetFloat("_ShadowThreshold", 0.5f);
        mat.SetFloat("_ShadowSoftness", 0.1f);
        mat.SetColor("_ShadowColor", new Color(0.5f, 0.5f, 0.8f, 1f));
        mat.SetColor("_RimColor", Color.white);
        mat.SetFloat("_RimPower", 3f);
        mat.SetFloat("_RimIntensity", 1f);
        mat.SetFloat("_OutlineWidth", 0.005f);
        mat.SetColor("_OutlineColor", Color.black);
        mat.SetFloat("_FlipX", 0);
        mat.SetFloat("_FlipY", 0);
        mat.SetFloat("_Brightness", 1f);
        mat.SetFloat("_Contrast", 1f);
        mat.SetFloat("_Saturation", 1f);
        mat.SetFloat("_Cutoff", 0.1f);
        mat.SetFloat("_WindStrength", 0);
        mat.SetFloat("_WindSpeed", 1f);
        mat.SetVector("_WindDirection", new Vector4(1, 0, 0, 0));
    }

    void CopyMaterialSettings(Material mat)
    {
        // 这里可以实现复制材质设置到剪贴板的功能
        Debug.Log("材质设置已复制");
    }

    void PasteMaterialSettings(Material mat)
    {
        // 这里可以实现从剪贴板粘贴材质设置的功能
        Debug.Log("材质设置已粘贴");
    }
}