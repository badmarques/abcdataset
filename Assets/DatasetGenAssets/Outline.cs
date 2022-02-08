using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Outline")]

public sealed class Outline : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Controls the scale of the outline.")]
    public IntParameter scale = new IntParameter(1);

    [Tooltip("Controls the color of the outline.")]
    public ColorParameter color = new ColorParameter(Color.white);

    [Tooltip("Controls the depth threshold.")]
    public FloatParameter depthThreshold = new FloatParameter(0.2f);

    [Tooltip("Controls the depth normal threshold.")]
    public FloatParameter depthNormalThreshold = new ClampedFloatParameter(0.5f, 0f, 1f);

    [Tooltip("Controls the scale of the depth normal threshold.")]
    public FloatParameter depthNormalThresholdScale = new FloatParameter(7f);

    [Tooltip("Controls the normal threshold.")]
    public ClampedFloatParameter normalThreshold = new ClampedFloatParameter(0.4f, 0f, 1f);

    Material m_Material;
    public bool IsActive() => m_Material != null && intensity.value > 0f && scale.value > 0;
    //public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforeTAA;
    public override void Setup()
    {
        if (Shader.Find("Hidden/Shader/Outline") != null)
            m_Material = new Material(Shader.Find("Hidden/Shader/Outline"));
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;
        m_Material.SetFloat("_Intensity", intensity.value);
        m_Material.SetFloat("_Scale", scale.value);
        m_Material.SetColor("_Color", color.value);
        m_Material.SetFloat("_DepthThreshold", depthThreshold.value);
        m_Material.SetFloat("_DepthNormalThreshold", depthNormalThreshold.value);
        m_Material.SetFloat("_DepthNormalThresholdScale", depthNormalThresholdScale.value);
        m_Material.SetFloat("_NormalThreshold", normalThreshold.value);
        m_Material.SetTexture("_InputTexture", source);
        HDUtils.DrawFullScreen(cmd, m_Material, destination);
    }

    public override void Cleanup() => CoreUtils.Destroy(m_Material);
}
