using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Avol
{
    class HairPass : ScriptableRenderPass
	{
		private		HairRenderer		_HairRenderer;
		private		ScriptableRenderer	_Renderer;

		private		Material			_GeometryGatherMaterial			= null;
		private		Material			_ShadingGatherMaterial			= null;

		public void Setup(HairRenderer hairRenderer, ScriptableRenderer renderer)
		{
			_HairRenderer		= hairRenderer;
			_Renderer			= renderer;

			_GeometryGatherMaterial		= new Material(Resources.Load<Shader>("Avol/Hair/Shaders/Core/HairGeometryGather"));
			_ShadingGatherMaterial		= new Material(Resources.Load<Shader>("Avol/Hair/Shaders/Core/HairShadingGather"));

			renderPassEvent		= RenderPassEvent.AfterRenderingShadows;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			// if screen size changed or buffers are not initialized
			if (_HairRenderer.HairScreenBuffers.FinalColorRT.width != renderingData.cameraData.camera.pixelWidth || _HairRenderer.HairScreenBuffers.FinalColorRT.height != renderingData.cameraData.camera.pixelHeight)
				_HairRenderer.RebuildBuffers(renderingData.cameraData.camera.pixelWidth, renderingData.cameraData.camera.pixelHeight);

			// clear final color buffer.
			// TODO: replace with command buffers.
			Graphics.SetRenderTarget(_HairRenderer.HairScreenBuffers.FinalColorRT);
			GL.Clear(false, true, Color.clear);

			Graphics.SetRenderTarget(_HairRenderer.HairScreenBuffers.FinalColorSwapRT);
			GL.Clear(false, true, Color.clear);

			Graphics.SetRenderTarget(_HairRenderer.HairScreenBuffers.FinalDepthRT);
			GL.Clear(true, true, Color.clear);

			// clear geometry buffers
			_ClearGeometry(context, _HairRenderer.HairScreenBuffers);

			// render geometry buffers
			for (int i = 0; i < HairRenderer.VisibleHairs.Count; i++)
				if (HairRenderer.VisibleHairs[i].Initialized)
                {
					HairSimulation hairSimulation = HairRenderer.VisibleHairs[i];

					if (hairSimulation.HairGeometry.HairPointBuffer != null)
					{
						hairSimulation.HairPhysics.Compute();

						_GatherGeometry(hairSimulation, context, renderingData, _HairRenderer.HairScreenBuffers);
					}
				}

			// do shading
			if (HairRenderer.VisibleHairs.Count > 0)
				_ComputeShading(HairRenderer.VisibleHairs[0], context, renderingData, _HairRenderer.HairScreenBuffers, true);
		}


		float calcf(float fov, float screenWidth)
		{
			float f = (screenWidth / (2 * Mathf.Tan(Mathf.PI * fov / 360)));
			return f;
		}

		private void _ClearGeometry(ScriptableRenderContext context, HairScreenBuffers hairScreenBuffers)
        {
			CommandBuffer cmd = CommandBufferPool.Get("Clear Hair Geometry Buffer");
			cmd.Clear();

			cmd.SetRenderTarget(hairScreenBuffers.TempTangentRT);
			cmd.ClearRenderTarget(false, true, Color.clear);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
		
		private void _GatherGeometry(HairSimulation hairSimulation, ScriptableRenderContext context, RenderingData renderingData, HairScreenBuffers hairScreenBuffers)
		{
			MaterialPropertyBlock materialProperties = new MaterialPropertyBlock();

			materialProperties.SetBuffer("_HairPointData", hairSimulation.HairGeometry.HairPointBuffer);
			materialProperties.SetBuffer("_HairStrandData", hairSimulation.HairGeometry.HairStrandBuffer);

			materialProperties.SetInt("_StrandSegments", hairSimulation.BasePoints);

			materialProperties.SetFloat("_CameraAspect", renderingData.cameraData.camera.aspect);
			materialProperties.SetFloat("_CameraFOV", Mathf.Tan(renderingData.cameraData.camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f);
			materialProperties.SetVector("_CameraRight", renderingData.cameraData.camera.transform.right);
			materialProperties.SetVector("_CameraUp", renderingData.cameraData.camera.transform.up);
			materialProperties.SetVector("_CameraForward", renderingData.cameraData.camera.transform.forward);
			materialProperties.SetVector("_CameraPosition", renderingData.cameraData.camera.transform.position);
			materialProperties.SetVector("_CameraPlanes", new Vector2(renderingData.cameraData.camera.nearClipPlane, renderingData.cameraData.camera.farClipPlane));
			materialProperties.SetMatrix("_WorldMatrix", (renderingData.cameraData.camera.projectionMatrix * renderingData.cameraData.camera.worldToCameraMatrix).inverse);
			materialProperties.SetFloat("_Transparency", _HairRenderer.Transparency);
			materialProperties.SetFloat("_ThicknessMultiplier", 1);

			materialProperties.SetFloat("_Roughness", hairSimulation.Roughness);
			materialProperties.SetFloat("_Shift", hairSimulation.Shift);
			materialProperties.SetVector("_ScatterTint", hairSimulation.ScatterTint);

			Matrix4x4 PV = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false) * renderingData.cameraData.camera.worldToCameraMatrix;
			materialProperties.SetMatrix("_CameraPV", PV);
			_ShadingGatherMaterial.SetMatrix("_CameraPV", PV);

			float scaleX = (calcf(renderingData.cameraData.camera.fieldOfView, renderingData.cameraData.camera.pixelWidth) / 1000.0f);
			float scaleY = (calcf(renderingData.cameraData.camera.fieldOfView, renderingData.cameraData.camera.pixelHeight) / 1000.0f);

			materialProperties.SetFloat("_TextureWidth", scaleX);
			materialProperties.SetFloat("_TextureHeight", scaleX);


			RenderTargetIdentifier[] colorTargets = new RenderTargetIdentifier[]
			{
				hairScreenBuffers.TempTangentRT.colorBuffer,
				hairScreenBuffers.TempColorRT.colorBuffer,
				hairScreenBuffers.TempScatterTintRT.colorBuffer,
				hairScreenBuffers.FinalDepthRT.colorBuffer,
			};


			CommandBuffer cmd = CommandBufferPool.Get("Hair Geometry Buffer");

			cmd.SetRenderTarget(colorTargets, hairScreenBuffers.FinalDepthRT.depthBuffer);
			cmd.DrawProcedural(Matrix4x4.identity, _GeometryGatherMaterial, 0, MeshTopology.LineStrip, hairSimulation.HairGeometry.HairPointBuffer.count, 1, materialProperties);


			context.ExecuteCommandBuffer(cmd);

			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}

		private void _ComputeShading(HairSimulation hairSimulation, ScriptableRenderContext context, RenderingData renderingData, HairScreenBuffers hairScreenBuffers, bool swap)
		{
			int visibleAdditionalLights = 0;
			bool mainLightExists = false;
			List<Vector4> lightData = new List<Vector4>();
			for (int i = 0; i < renderingData.lightData.visibleLights.Length; i++)
			{
				if (renderingData.lightData.visibleLights[i].lightType == LightType.Point ||
					renderingData.lightData.visibleLights[i].lightType == LightType.Spot)
					visibleAdditionalLights++;
				else if (renderingData.lightData.visibleLights[i].lightType == LightType.Directional)
					mainLightExists = true;

				if (renderingData.lightData.visibleLights[i].lightType == LightType.Point ||
					renderingData.lightData.visibleLights[i].lightType == LightType.Spot)
				{
					float near	= renderingData.lightData.visibleLights[i].light.shadowNearPlane;
					float far	= renderingData.lightData.visibleLights[i].light.range;
					lightData.Add(new Vector4(near, far, 0, 0));
				}
			}

			if (visibleAdditionalLights != 0)
				_ShadingGatherMaterial.SetVectorArray("_AdditionalLightHairData", lightData);

			_ShadingGatherMaterial.SetInt("_MainLightEnabled", mainLightExists ? 1 : 0);
			_ShadingGatherMaterial.SetInt("_VisibleAdditionalLights", visibleAdditionalLights);

			_ShadingGatherMaterial.SetInt("_AmbientMode", (int)RenderSettings.ambientMode);
			_ShadingGatherMaterial.SetFloat("_AmbientIntensity", RenderSettings.ambientIntensity);
			_ShadingGatherMaterial.SetColor("_AmbientColor", RenderSettings.ambientSkyColor);

			_ShadingGatherMaterial.SetFloat("_CameraAspect", renderingData.cameraData.camera.aspect);
			_ShadingGatherMaterial.SetFloat("_CameraFOV", Mathf.Tan(renderingData.cameraData.camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f);
			_ShadingGatherMaterial.SetVector("_CameraRight", renderingData.cameraData.camera.transform.right);
			_ShadingGatherMaterial.SetVector("_CameraUp", renderingData.cameraData.camera.transform.up);
			_ShadingGatherMaterial.SetVector("_CameraForward", renderingData.cameraData.camera.transform.forward);
			_ShadingGatherMaterial.SetVector("_CameraPosition", renderingData.cameraData.camera.transform.position);
			_ShadingGatherMaterial.SetVector("_CameraPlanes", new Vector2(renderingData.cameraData.camera.nearClipPlane, renderingData.cameraData.camera.farClipPlane));
			_ShadingGatherMaterial.SetMatrix("_WorldMatrix", (renderingData.cameraData.camera.projectionMatrix * renderingData.cameraData.camera.worldToCameraMatrix).inverse);
			_ShadingGatherMaterial.SetInt("_DeepOpacityShadow", _HairRenderer.SelfShadowing ? 1 : 0);

			if (_HairRenderer.SelfShadowing)
			{
				_ShadingGatherMaterial.SetTexture("_DeepDepthMap", _HairRenderer.DOMPass.DirectionalLightDepthMapDownsampled);
				_ShadingGatherMaterial.SetTexture("_DeepOpacityMap", _HairRenderer.DOMPass.DirectionalLightOpacityMap);

				_ShadingGatherMaterial.SetTexture("_DeepAdditionalDepthMap", _HairRenderer.DOMPass.AdditionalLightsDepthMapDownsampled);
				_ShadingGatherMaterial.SetTexture("_DeepAdditionalOpacityMap", _HairRenderer.DOMPass.AdditionalLightsOpacityMap);

				_ShadingGatherMaterial.SetVector("_SelfShadowCascades", _HairRenderer.DOMPass.GetOpacityLayerDistribution());
				_ShadingGatherMaterial.SetFloat("_SelfShadowingStrength", _HairRenderer.SelfShadowingStrength);

				_ShadingGatherMaterial.SetFloat("_SelfShadowingSamples", _HairRenderer.SelfShadowingSamples);
				_ShadingGatherMaterial.SetFloat("_SelfShadowJitter", _HairRenderer.SelfShadowingJitter);
				_ShadingGatherMaterial.SetFloat("_SelfShadowRange", _HairRenderer.SelfShadowRange);
			}


			_ShadingGatherMaterial.SetTexture("_Tangent", hairScreenBuffers.TempTangentRT);
			_ShadingGatherMaterial.SetTexture("_Color", hairScreenBuffers.TempColorRT);
			_ShadingGatherMaterial.SetTexture("_ScatterTint", hairScreenBuffers.TempScatterTintRT);

			_ShadingGatherMaterial.SetTexture("_FinalDepth", hairScreenBuffers.FinalDepthRT);

			_ShadingGatherMaterial.SetInt("_AASampleCount", Mathf.Max(1, (int)Mathf.Pow(2, (int)_HairRenderer.Antialiasing)));

			LightProbeUtility.SetSHCoefficients(hairSimulation.transform.position, _ShadingGatherMaterial);

			CommandBuffer cmd = CommandBufferPool.Get("Compute hair shading.");

			RenderTexture finalRead		= _HairRenderer.HairScreenBuffers.FinalColorRT;
			RenderTexture finalWrite	= _HairRenderer.HairScreenBuffers.FinalColorSwapRT;

			cmd.SetRenderTarget(finalWrite);
			cmd.Blit(finalRead, finalWrite, _ShadingGatherMaterial, (int)_HairRenderer.Antialiasing);
			context.ExecuteCommandBuffer(cmd);

			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}
	}
}