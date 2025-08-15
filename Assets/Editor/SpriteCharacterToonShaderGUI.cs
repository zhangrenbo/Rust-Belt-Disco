using UnityEngine;
using UnityEditor;

/// <summary>
/// 2D��ɫ��ͨShader���Զ���༭��
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
        // ������������
        FindProperties(properties);

        // ���Ʊ���
        EditorGUILayout.Space();
        GUILayout.Label("2D��ɫ��ͨ��Ⱦ��", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ��������
        showBaseSettings = EditorGUILayout.Foldout(showBaseSettings, "��������", true);
        if (showBaseSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(new GUIContent("��ɫ��ͼ", "��Ҫ�Ľ�ɫ������ͼ"), mainTex);
            materialEditor.ColorProperty(color, "��ɫ����");
            materialEditor.RangeProperty(cutoff, "͸���Ȳü�");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // ��ͨ��Ⱦ����
        showToonSettings = EditorGUILayout.Foldout(showToonSettings, "��ͨ��Ⱦ", true);
        if (showToonSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(new GUIContent("��ͨ������ͼ", "���ڿ��ƹ�Ӱ���ɵ�1D������ͼ"), toonRamp);
            materialEditor.RangeProperty(shadowThreshold, "��Ӱ��ֵ");
            materialEditor.RangeProperty(shadowSoftness, "��Ӱ���");
            materialEditor.ColorProperty(shadowColor, "��Ӱ��ɫ");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // ��Ե������
        showRimSettings = EditorGUILayout.Foldout(showRimSettings, "��Ե��Ч��", true);
        if (showRimSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.ColorProperty(rimColor, "��Ե����ɫ");
            materialEditor.RangeProperty(rimPower, "��Ե��ǿ��");
            materialEditor.RangeProperty(rimIntensity, "��Ե������");
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // �������
        showOutlineSettings = EditorGUILayout.Foldout(showOutlineSettings, "���Ч��", true);
        if (showOutlineSettings)
        {
            EditorGUI.indentLevel++;
            materialEditor.RangeProperty(outlineWidth, "��߿��");
            materialEditor.ColorProperty(outlineColor, "�����ɫ");

            if (outlineWidth.floatValue > 0)
            {
                EditorGUILayout.HelpBox("���Ч����Ҫ��ɫ����ȷ�ķ�����Ϣ", MessageType.Info);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // ����֧��
        showAnimationSettings = EditorGUILayout.Foldout(showAnimationSettings, "����֧��", false);
        if (showAnimationSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("��ת����", EditorStyles.boldLabel);
            materialEditor.FloatProperty(flipX, "ˮƽ��ת");
            materialEditor.FloatProperty(flipY, "��ֱ��ת");

            EditorGUILayout.HelpBox("ͨ���ű����� material.SetFloat(\"_FlipX\", 1) ����ת��ɫ", MessageType.Info);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // ��Ч����
        showEffectSettings = EditorGUILayout.Foldout(showEffectSettings, "�Ӿ���Ч", false);
        if (showEffectSettings)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("��ɫ����", EditorStyles.boldLabel);
            materialEditor.RangeProperty(brightness, "����");
            materialEditor.RangeProperty(contrast, "�Աȶ�");
            materialEditor.RangeProperty(saturation, "���Ͷ�");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("��Ч��", EditorStyles.boldLabel);
            materialEditor.RangeProperty(windStrength, "����ǿ��");
            materialEditor.RangeProperty(windSpeed, "����");
            materialEditor.VectorProperty(windDirection, "����");

            if (windStrength.floatValue > 0)
            {
                EditorGUILayout.HelpBox("��Ч�����ý�ɫ��΢�ڶ����ʺϲ��ϻ�ͷ��", MessageType.Info);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // �߼�����
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "�߼�����", false);
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;

            // ��Ⱦ����
            materialEditor.RenderQueueField();

            // ˫����Ⱦ��ʾ
            EditorGUILayout.HelpBox("��ShaderĬ������˫����Ⱦ���ʺ�2DֽƬ�˽�ɫ", MessageType.Info);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // Ԥ�谴ť
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("����Ԥ��", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("������ͨ"))
        {
            ApplyBrightToonPreset(materialEditor.target as Material);
        }
        if (GUILayout.Button("�����Ӱ"))
        {
            ApplySoftShadowPreset(materialEditor.target as Material);
        }
        if (GUILayout.Button("ǿ�Ա�"))
        {
            ApplyHighContrastPreset(materialEditor.target as Material);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("����ΪĬ��"))
        {
            ResetToDefault(materialEditor.target as Material);
        }
        if (GUILayout.Button("��������"))
        {
            CopyMaterialSettings(materialEditor.target as Material);
        }
        if (GUILayout.Button("ճ������"))
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
        // �������ʵ�ָ��Ʋ������õ�������Ĺ���
        Debug.Log("���������Ѹ���");
    }

    void PasteMaterialSettings(Material mat)
    {
        // �������ʵ�ִӼ�����ճ���������õĹ���
        Debug.Log("����������ճ��");
    }
}