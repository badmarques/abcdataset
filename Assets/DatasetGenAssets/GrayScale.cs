using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/GrayScale")]

public sealed class GrayScale : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Controls the scale of the outline.")]
    public IntParameter scale = new IntParameter(1);

    [Tooltip("Controls the color of the outline.")]
    public ColorParameter color = new ColorParameter(Color.white);

    [Tooltip("Controls the depth threshold.")]
    public FloatParameter depthThreshold = new FloatParameter(0.2f);

    Material m_Material;
    public bool IsActive() => m_Material != null && intensity.value > 0f && scale.value > 0;
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;
    public override void Setup()
    {
        if (Shader.Find("Hidden/Shader/GrayScale") != null)
            m_Material = new Material(Shader.Find("Hidden/Shader/GrayScale"));
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;
        m_Material.SetFloat("_Intensity", intensity.value);
        m_Material.SetFloat("_Scale", scale.value);
        m_Material.SetColor("_Color", color.value);
        m_Material.SetFloat("_DepthThreshold", depthThreshold.value);
        m_Material.SetTexture("_InputTexture", source);
        HDUtils.DrawFullScreen(cmd, m_Material, destination);
    }

    public override void Cleanup() => CoreUtils.Destroy(m_Material);
}
