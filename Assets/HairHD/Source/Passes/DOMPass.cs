using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Avol
{
	/// <summary>
	/// TODO: setup shadow slices in separate place (in constructor) for both shadow and DOM.
	/// TODO: custom hair math class.
	/// </summary>
	public class DOMPass : ScriptableRenderPass
	{
		HairRenderer			_HairRenderer;
		ScriptableRenderer		_Renderer;

		public RenderTexture	DirectionalLightDepthMap;
		public RenderTexture	AdditionalLightsDepthMap;

		public RenderTexture	DirectionalLightDepthMapDownsampled;
		public RenderTexture	AdditionalLightsDepthMapDownsampled;

		public RenderTexture    DirectionalLightOpacityMap;
		public RenderTexture    AdditionalLightsOpacityMap;

		private Material		_OpacityMaterial;

		private ShadowSlices	_ShadowSlices	= new ShadowSlices(RenderPassEvent.AfterRendering);

		public DOMPass()
        {
			_OpacityMaterial = new Material(Resources.Load<Shader>("Avol/Hair/Shaders/Core/TransparentShadowOpacity"));
			renderPassEvent = RenderPassEvent.AfterRenderingShadows;
		}



		public void Setup(HairRenderer hairRenderer, ScriptableRenderer renderer, RenderingData renderingData)
		{
			_HairRenderer		= hairRenderer;
			_Renderer			= renderer;

			RebuildBuffers(renderingData);
		}

		public void Dispose()
        {
			if (DirectionalLightDepthMap != null)				DirectionalLightDepthMap.Release();
			if (AdditionalLightsDepthMap != null)				AdditionalLightsDepthMap.Release();
			if (DirectionalLightDepthMapDownsampled != null)	DirectionalLightDepthMapDownsampled.Release();

			if (AdditionalLightsDepthMapDownsampled != null)	AdditionalLightsDepthMapDownsampled.Release();
			if (DirectionalLightOpacityMap != null)				DirectionalLightOpacityMap.Release();
			if (AdditionalLightsOpacityMap != null)				AdditionalLightsOpacityMap.Release();
		}


		// NOTE: idk about performance of this vs temporary textures. should be equal though.
		private void RebuildBuffers(RenderingData renderingData)
        {
			// main light resolutions.
			Vector2 mainLightShadowSize = new Vector2(renderingData.shadowData.mainLightShadowmapWidth, renderingData.shadowData.mainLightShadowmapHeight);
			int downscale = Mathf.Max(1, (int)Mathf.Pow(2, (int)_HairRenderer.SelfShadowDirectionalResolution));
			Vector2 mainLightShadowSizeDownscaled = new Vector2(mainLightShadowSize.x, mainLightShadowSize.y) / downscale;

			// additional resolutions.
			Vector2 additionalLightShadowSize = new Vector2(512, 512);
			RenderTexture additionalShadow = (RenderTexture)Shader.GetGlobalTexture("_AdditionalLightsShadowmapTexture");
			if (additionalShadow != null)
				additionalLightShadowSize = new Vector2(additionalShadow.width, additionalShadow.height);

			int downscaleAdditional = Mathf.Max(1, (int)Mathf.Pow(2, (int)_HairRenderer.SelfShadowDirectionalResolution));
			Vector2 additionalLightShadowSizeDownscaled = additionalLightShadowSize / downscaleAdditional;

			// directional maps
			{
				if (DirectionalLightDepthMap == null || DirectionalLightDepthMap.width != mainLightShadowSize.x || DirectionalLightDepthMap.height != mainLightShadowSize.y)
				{ 
					if (DirectionalLightDepthMap != null)
						DirectionalLightDepthMap.Release();

					DirectionalLightDepthMap				= new RenderTexture((int)mainLightShadowSize.x, (int)mainLightShadowSize.y, 24, RenderTextureFormat.Shadowmap);
				}

				if (DirectionalLightDepthMapDownsampled == null || DirectionalLightDepthMapDownsampled.width != mainLightShadowSizeDownscaled.x || DirectionalLightDepthMapDownsampled.height != mainLightShadowSizeDownscaled.y)
				{
					if (DirectionalLightDepthMapDownsampled != null)
						DirectionalLightDepthMapDownsampled.Release();

					if (DirectionalLightOpacityMap != null)
						DirectionalLightOpacityMap.Release();

					DirectionalLightDepthMapDownsampled		= new RenderTexture((int)mainLightShadowSizeDownscaled.x, (int)mainLightShadowSizeDownscaled.y, 0, RenderTextureFormat.RFloat);
					DirectionalLightOpacityMap				= new RenderTexture((int)mainLightShadowSizeDownscaled.x, (int)mainLightShadowSizeDownscaled.y, 0, RenderTextureFormat.ARGB32);
				}
			}

			// additional maps
			{
				if (AdditionalLightsDepthMap == null || AdditionalLightsDepthMap.width != additionalLightShadowSize.x || AdditionalLightsDepthMap.height != additionalLightShadowSize.y)
				{
					if (AdditionalLightsDepthMap != null)
						AdditionalLightsDepthMap.Release();

					AdditionalLightsDepthMap				= new RenderTexture((int)additionalLightShadowSize.x, (int)additionalLightShadowSize.y, 24, RenderTextureFormat.Shadowmap);
				}

				if (AdditionalLightsDepthMapDownsampled == null || AdditionalLightsDepthMapDownsampled.width != additionalLightShadowSizeDownscaled.x || AdditionalLightsDepthMapDownsampled.height != additionalLightShadowSizeDownscaled.y)
				{
					if (AdditionalLightsDepthMapDownsampled != null)
						AdditionalLightsDepthMapDownsampled.Release();

					if (AdditionalLightsOpacityMap != null)
						AdditionalLightsOpacityMap.Release();

					AdditionalLightsDepthMapDownsampled		= new RenderTexture((int)additionalLightShadowSizeDownscaled.x, (int)additionalLightShadowSizeDownscaled.y, 0, RenderTextureFormat.RFloat);
					AdditionalLightsOpacityMap				= new RenderTexture((int)additionalLightShadowSizeDownscaled.x, (int)additionalLightShadowSizeDownscaled.y, 0, RenderTextureFormat.ARGB32);
				}
			}
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			// depth maps.
			{
				// clear 
				CommandBuffer cmd = CommandBufferPool.Get("Clear Temporary Shadow Maps");

				cmd.SetRenderTarget(DirectionalLightDepthMap);
				cmd.ClearRenderTarget(true, true, Color.clear);
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				cmd.SetRenderTarget(AdditionalLightsDepthMap);
				cmd.ClearRenderTarget(true, true, Color.clear);
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				CommandBufferPool.Release(cmd);

				// write
				_HairRenderer.ShadowPass.RenderAllShadows(context, ref renderingData, RenderPassEvent.BeforeRenderingShadows, DirectionalLightDepthMap, AdditionalLightsDepthMap);

				// downsample
				_DownsampleDepth(context, ref renderingData);
			}

			// opacity maps.
			{
				// clear 
				CommandBuffer cmd = CommandBufferPool.Get("Clear Temporary Opacity Maps");
				cmd.SetRenderTarget(DirectionalLightOpacityMap);
				cmd.ClearRenderTarget(true, true, Color.clear);
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				cmd.SetRenderTarget(AdditionalLightsOpacityMap);
				cmd.ClearRenderTarget(true, true, Color.clear);
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();

				CommandBufferPool.Release(cmd);



				// write directional
				{
					// get main light bounds
					Bounds mainLightBounds;
					renderingData.cullResults.GetShadowCasterBounds(renderingData.lightData.mainLightIndex, out mainLightBounds);

					// render each hair.
					for (int i = 0; i < HairRenderer.Hairs.Count; i++)
					{
						if (HairRenderer.Hairs[i].Initialized)
						{
							BoundingSphere boundingSphere = HairRenderer.Hairs[i].GetBoundingSphere();
							if (mainLightBounds.Intersects(new Bounds(boundingSphere.position, Vector3.one * boundingSphere.radius)))
							{
								HairSimulation hairSimulation = HairRenderer.Hairs[i];
								_RenderDirectionalOpacity(hairSimulation, context, ref renderingData);
							}
						}
					}
				}

				// calculate atlas slices.
				_ShadowSlices.Setup(ref renderingData);

				// render hair to all opacity maps.
				for (int i = 0; i < HairRenderer.Hairs.Count; i++)
				{
					if (HairRenderer.Hairs[i].Initialized)
					{
						BoundingSphere boundingSphere = HairRenderer.Hairs[i].GetBoundingSphere();

						// write additional
						for (int c = 0; c < renderingData.lightData.visibleLights.Length; c++)
						{
							float spotAngle = renderingData.lightData.visibleLights[c].spotAngle;
							LightType lightType = renderingData.lightData.visibleLights[c].lightType;

							if (lightType == LightType.Point || lightType == LightType.Spot)
							{
								int shadowLightIndex = _ShadowSlices.GetShadowLightIndexFromLightIndex(c);
								int slices = lightType == LightType.Point ? 6 : 1;

								Bounds additionalLightBounds;
								renderingData.cullResults.GetShadowCasterBounds(c, out additionalLightBounds);

								if (additionalLightBounds.Intersects(new Bounds(boundingSphere.position, Vector3.one * boundingSphere.radius)))
								{
									for (int k = 0; k < slices; k++)
									{
										ShadowSliceData slice = _ShadowSlices.m_AdditionalLightsShadowSlices[shadowLightIndex + k];

										float nearPlane = renderingData.lightData.visibleLights[c].light.shadowNearPlane;
										float farPlane = renderingData.lightData.visibleLights[c].range;

										_RenderAdditionalOpacity(HairRenderer.Hairs[i], context, renderingData, slice, shadowLightIndex + k, spotAngle, nearPlane, farPlane);
									}
								}
							}
						}
					}
				}
			}
		}

		private void _DownsampleDepth(ScriptableRenderContext context, ref RenderingData renderingData)
        {
			CommandBuffer cmd = CommandBufferPool.Get("Downsample depth");

			cmd.Blit(DirectionalLightDepthMap, DirectionalLightDepthMapDownsampled);
			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();

			cmd.Blit(AdditionalLightsDepthMap, AdditionalLightsDepthMapDownsampled);
			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();

			CommandBufferPool.Release(cmd);
		}

		private void _RenderDirectionalOpacity(HairSimulation hairSimulation, ScriptableRenderContext context, ref RenderingData renderingData)
		{
			Rect[] rects = GetDirectionalLightTextureBounds(ref renderingData);

			int cascadeCount = renderingData.shadowData.mainLightShadowCascadesCount;

			float[] cascadeScales = new float[4] {  renderingData.shadowData.mainLightShadowCascadesSplit.x,
													renderingData.shadowData.mainLightShadowCascadesSplit.y,
													renderingData.shadowData.mainLightShadowCascadesSplit.z,
													1 };

			for (int c = 0; c < cascadeCount; c++)
			{
				MaterialPropertyBlock properties = new MaterialPropertyBlock();

				properties.SetTexture("_DepthMap", DirectionalLightDepthMapDownsampled);
				properties.SetBuffer("_HairPointData", hairSimulation.HairGeometry.HairPointBuffer);
				properties.SetBuffer("_HairStrandData", hairSimulation.HairGeometry.HairStrandBuffer);
				properties.SetInt("_StrandSegments", hairSimulation.BasePoints);

				properties.SetFloat("_Opacity", hairSimulation.SelfShadowingOpacity);
				properties.SetVector("_LayerDistribution", _HairRenderer.DOMPass.GetOpacityLayerDistribution());
				properties.SetFloat("_ThicknessMultiplier", 1);

				properties.SetFloat("_TextureWidth", 1.0f / _HairRenderer.UniversalRenderPipelineAsset.shadowDistance / cascadeScales[c]);
				properties.SetFloat("_TextureHeight", 1.0f / _HairRenderer.UniversalRenderPipelineAsset.shadowDistance / cascadeScales[c]);

				properties.SetInt("_Cascade", c);
				properties.SetInt("_CascadeCount", cascadeCount);
				properties.SetVector("_Resolution", new Vector2(DirectionalLightOpacityMap.width, DirectionalLightOpacityMap.height));
				properties.SetFloat("_SelfShadowRange", _HairRenderer.SelfShadowRange);
				properties.SetInt("_Orthographic", 1);

				Matrix4x4 PV = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false) * renderingData.cameraData.camera.worldToCameraMatrix;
				properties.SetMatrix("_CameraPV", PV);


				CommandBuffer cmd = new CommandBuffer();
				cmd.name = "Directional opacity map.";
				
				cmd.SetRenderTarget(DirectionalLightOpacityMap);

				cmd.EnableScissorRect(new Rect(rects[c].x, rects[c].y, rects[c].width, rects[c].height));
				cmd.DrawProcedural(Matrix4x4.identity, _OpacityMaterial, 0, MeshTopology.LineStrip, hairSimulation.HairGeometry.HairPointBuffer.count, 1, properties);
				cmd.DisableScissorRect();

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
			}
		}

		private void _RenderAdditionalOpacity(HairSimulation hairSimulation, ScriptableRenderContext context, RenderingData renderingData, ShadowSliceData shadowSliceData, int lightIndex, float spotAngle, float near, float far)
        {
			int downscale = Mathf.Max(1, (int)Mathf.Pow(2, (int)_HairRenderer.SelfShadowDirectionalResolution));

			int shadowSliceResolution = shadowSliceData.resolution / downscale;

			float camScale = renderingData.cameraData.camera.pixelWidth / (float)shadowSliceData.resolution;
			float atlasScaleX = AdditionalLightsOpacityMap == null ? 1 : AdditionalLightsOpacityMap.width / shadowSliceResolution;
			float atlasScaleY = AdditionalLightsOpacityMap == null ? 1 : AdditionalLightsOpacityMap.height / shadowSliceResolution;
			float scaleX = (calcf(spotAngle, shadowSliceResolution) / 1000.0f) * camScale / atlasScaleX;
			float scaleY = (calcf(spotAngle, shadowSliceResolution) / 1000.0f) * camScale / atlasScaleY;

			MaterialPropertyBlock properties = new MaterialPropertyBlock();
			properties.SetFloat("_TextureWidth", scaleX);
			properties.SetFloat("_TextureHeight", scaleY);
			properties.SetInt("_LightIndex", lightIndex);
	

			properties.SetTexture("_DepthMap", AdditionalLightsDepthMap);
			properties.SetBuffer("_HairPointData", hairSimulation.HairGeometry.HairPointBuffer);
			properties.SetBuffer("_HairStrandData", hairSimulation.HairGeometry.HairStrandBuffer);
			properties.SetInt("_StrandSegments", hairSimulation.BasePoints);

			properties.SetFloat("_Opacity", hairSimulation.SelfShadowingOpacity);
			properties.SetVector("_LayerDistribution", _HairRenderer.DOMPass.GetOpacityLayerDistribution());
			properties.SetFloat("_ThicknessMultiplier", 1);


			properties.SetVector("_Resolution", new Vector2(AdditionalLightsOpacityMap.width, AdditionalLightsOpacityMap.height));
			properties.SetFloat("_SelfShadowRange", _HairRenderer.SelfShadowRange);
			properties.SetFloat("_NearClip", near);
			properties.SetFloat("_FarClip", far);


			Matrix4x4 PV = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false) * renderingData.cameraData.camera.worldToCameraMatrix;
			properties.SetMatrix("_CameraPV", PV);


			CommandBuffer cmd = new CommandBuffer();
			cmd.name = "Additional opacity map.";

			cmd.SetRenderTarget(AdditionalLightsOpacityMap);

			cmd.EnableScissorRect(new Rect(shadowSliceData.offsetX/ downscale, shadowSliceData.offsetY / downscale, shadowSliceResolution, shadowSliceResolution));
			cmd.DrawProcedural(Matrix4x4.identity, _OpacityMaterial, 1, MeshTopology.LineStrip, hairSimulation.HairGeometry.HairPointBuffer.count, 1, properties);
			cmd.DisableScissorRect();

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
        }


		public Vector4 GetOpacityLayerDistribution()
        {
			// exponential is enough.
			return new Vector4(0.13534f, 0.22313f, 0.36788f, 0.60653f);

			/*if (_HairRenderer == null)
				return Vector4.zero;

			Vector4 distribution = Vector4.zero;

			switch (_HairRenderer.SelfShadowOpacityLayerDistribution)
            {
				case OpacityLayerDistribution.Linear:
					distribution = new Vector4(0.15f, 0.3f, 0.45f, 0.6f);
					break;

				case OpacityLayerDistribution.Exponential:
					distribution = new Vector4(0.13534f, 0.22313f, 0.36788f, 0.60653f);
					break;

				case OpacityLayerDistribution.Fibonacci:
					distribution = new Vector4(0.1f, 0.2f, 0.3f, 0.5f);
					break;
			}

			return distribution;*/
		}

		public Rect[] GetDirectionalLightTextureBounds(ref RenderingData renderingData)
        {
			int cascadeCount = renderingData.shadowData.mainLightShadowCascadesCount;

			Vector2 shadowResoluton = new Vector2(DirectionalLightOpacityMap.width, DirectionalLightOpacityMap.height);

			Rect[] rects = new Rect[4];

			float X = shadowResoluton.x; float Y = shadowResoluton.y;
			float halfX = shadowResoluton.x * 0.5f; float halfY = shadowResoluton.y * 0.5f;

			switch (cascadeCount)
			{
				case 1:
					rects[0] = new Rect(0, 0, X, Y);
					break;
				case 2:
					rects[0] = new Rect(0, 0, halfX, Y);
					rects[1] = new Rect(halfX, 0, halfY, Y);
					break;
				case 4:
					rects[0] = new Rect(0, 0, halfX, halfY);
					rects[1] = new Rect(halfX, 0, halfX, halfY);
					rects[2] = new Rect(0, halfY, halfX, halfY);
					rects[3] = new Rect(halfX, halfY, halfX, halfY);
					break;
			}

			return rects;
		}

		float calcf(float fov, float screenWidth)
		{
			float f = (screenWidth / (2 * Mathf.Tan(Mathf.PI * fov / 360)));
			return f;
		}

	}
}