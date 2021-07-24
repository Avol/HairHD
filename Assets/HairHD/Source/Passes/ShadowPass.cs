using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Avol
{
	public class ShadowPass : ScriptableRenderPass
	{
		private		HairRenderer		_HairRenderer;
		private		ScriptableRenderer	_Renderer;
		private		ShadowSlices		_ShadowSlices		= new ShadowSlices(RenderPassEvent.AfterRendering);

		private		Material			_ShadowCasterMaterial			= null;
		private		Material			_CopyDepthMaterial				= null;

		public void Setup(HairRenderer hairRenderer, ScriptableRenderer renderer)
		{
			_HairRenderer	= hairRenderer;
			_Renderer		= renderer;

			if (_ShadowCasterMaterial == null)
			{
				_ShadowCasterMaterial		= new Material(Resources.Load<Shader>("Avol/Hair/Shaders/Core/HairShadowCaster"));
				_CopyDepthMaterial			= new Material(Resources.Load<Shader>("Avol/Hair/Shaders/Core/CopyShadowDepth"));
			}
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (!_HairRenderer.SelfShadowing)
			{
				RenderAllShadows(context, ref renderingData, RenderPassEvent.AfterRenderingShadows,
								(RenderTexture)Shader.GetGlobalTexture("_MainLightShadowmapTexture"), (RenderTexture)Shader.GetGlobalTexture("_AdditionalLightsShadowmapTexture"));
			}
			else
			{
				CopyAllShadowsFromDOM(context, ref renderingData);
			}
		}

		public void RenderAllShadows(ScriptableRenderContext context, ref RenderingData renderingData,
									 RenderPassEvent renderPassEvent, RenderTexture mainLightTarget, RenderTexture additionalLightTarget)
        {
			this.renderPassEvent = renderPassEvent;

			// calculate atlas slices.
			_ShadowSlices.Setup(ref renderingData);

			// get main light bounds
			Bounds mainLightBounds;
			renderingData.cullResults.GetShadowCasterBounds(renderingData.lightData.mainLightIndex, out mainLightBounds);

			// render hair to all shadow maps.
			for (int i = 0; i < HairRenderer.Hairs.Count; i++)
			{
				if (HairRenderer.Hairs[i].Initialized)
				{
					BoundingSphere hairBoundingSphere = HairRenderer.Hairs[i].GetBoundingSphere();

					// render hair to directional light shadow map
					if (mainLightBounds.Intersects(new Bounds(hairBoundingSphere.position, Vector3.one * hairBoundingSphere.radius)))
						_RenderMainLightShadow(mainLightTarget, HairRenderer.Hairs[i], context, renderingData, _HairRenderer.UniversalRenderPipelineAsset);

					// render hair to additional lights shadow map
					Vector4[] additionalLightShadowTextureBounds	= new Vector4[256];
					Vector4[] additionalLightShadowTexelSizes		= new Vector4[256];

					if (additionalLightTarget != null)
					{
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

								if (additionalLightBounds.Intersects(new Bounds(hairBoundingSphere.position, Vector3.one * hairBoundingSphere.radius)))
                                {
									for (int k = 0; k < slices; k++)
									{
										ShadowSliceData slice = _ShadowSlices.m_AdditionalLightsShadowSlices[shadowLightIndex + k];
										_RenderAdditionalLightShadow(additionalLightTarget, HairRenderer.Hairs[i], context, renderingData, slice, shadowLightIndex + k, spotAngle);

										additionalLightShadowTextureBounds[shadowLightIndex + k] = new Vector4((float)slice.offsetX / additionalLightTarget.width, (float)slice.offsetY / additionalLightTarget.height,
																												(float)slice.resolution / additionalLightTarget.width, (float)slice.resolution / additionalLightTarget.height);


										additionalLightShadowTexelSizes[shadowLightIndex + k] = new Vector2((float)slice.resolution / additionalLightTarget.width, (float)slice.resolution / additionalLightTarget.height);
									}
								}
							}
						}
					}

					// set texture sampling bounds for soft shadow.
					Shader.SetGlobalVectorArray("_AdditionalLightsTextureBounds", additionalLightShadowTextureBounds);
					//Shader.SetGlobalFloat("_AdditionalLightsTextureAspect", additionalLightTarget.height / additionalLightTarget.width);

					Shader.SetGlobalVectorArray("_AdditionalShadowTextureTexelSizes", additionalLightShadowTexelSizes);
				}
			}
		}

		public void CopyAllShadowsFromDOM(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			this.renderPassEvent = RenderPassEvent.AfterRenderingShadows;

			if (renderingData.shadowData.supportsMainLightShadows)
			{
				CommandBuffer cmd = CommandBufferPool.Get("Copy Directional Shadow Maps");
				cmd.Clear();
				cmd.Blit(_HairRenderer.DOMPass.DirectionalLightDepthMap, (RenderTexture)Shader.GetGlobalTexture("_MainLightShadowmapTexture"), _CopyDepthMaterial);
				context.ExecuteCommandBuffer(cmd);
				CommandBufferPool.Release(cmd);
			}

			if (renderingData.shadowData.supportsAdditionalLightShadows)
			{
				CommandBuffer cmd = CommandBufferPool.Get("Copy Additional Shadow Maps");
				cmd.Clear();
				cmd.Blit(_HairRenderer.DOMPass.AdditionalLightsDepthMap, (RenderTexture)Shader.GetGlobalTexture("_AdditionalLightsShadowmapTexture"), _CopyDepthMaterial);
				context.ExecuteCommandBuffer(cmd);
				CommandBufferPool.Release(cmd);
			}
		}

		private void _RenderMainLightShadow(RenderTexture target, HairSimulation hairSimulation, ScriptableRenderContext context, RenderingData renderingData, UniversalRenderPipelineAsset universalRenderPipelineAsset)
		{
			int cascadeCount = renderingData.shadowData.mainLightShadowCascadesCount;

			Matrix4x4 PV = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false) * renderingData.cameraData.camera.worldToCameraMatrix;
			_ShadowCasterMaterial.SetMatrix("_CameraPV", PV);

			Vector2 shadowResoluton = new Vector2(renderingData.shadowData.mainLightShadowmapWidth, renderingData.shadowData.mainLightShadowmapWidth);
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

			float[] cascadeScales = new float[4] {  renderingData.shadowData.mainLightShadowCascadesSplit.x,
													renderingData.shadowData.mainLightShadowCascadesSplit.y,
													renderingData.shadowData.mainLightShadowCascadesSplit.z,
													1 };

			for (int c = 0; c < cascadeCount; c++)
			{
				float scaleX = renderingData.cameraData.camera.pixelWidth / (float)shadowResoluton.x;
				float scaleY = renderingData.cameraData.camera.pixelWidth / (float)shadowResoluton.y;

				MaterialPropertyBlock block = new MaterialPropertyBlock();

				block.SetBuffer("_HairPointData", hairSimulation.HairGeometry.HairPointBuffer);
				block.SetBuffer("_HairStrandData", hairSimulation.HairGeometry.HairStrandBuffer);

				block.SetInt("_StrandSegments", hairSimulation.BasePoints);
				block.SetInt("_CascadeCount", cascadeCount);

				block.SetFloat("_TextureWidth", 1.0f / universalRenderPipelineAsset.shadowDistance / cascadeScales[c]);
				block.SetFloat("_TextureHeight", 1.0f / universalRenderPipelineAsset.shadowDistance / cascadeScales[c]);
				block.SetInt("_Cascade", c);
				block.SetInt("_Orthographic", 1);

				CommandBuffer cmd = CommandBufferPool.Get("Hair Shadow To Main Light.");
				cmd.Clear();

				cmd.SetRenderTarget(target);
				cmd.EnableScissorRect(new Rect(rects[c].x, rects[c].y, rects[c].width, rects[c].height));
				cmd.DrawProcedural(Matrix4x4.identity, _ShadowCasterMaterial, 0, MeshTopology.LineStrip, hairSimulation.HairGeometry.HairPointBuffer.count, 1, block);
				cmd.DisableScissorRect();
				context.ExecuteCommandBuffer(cmd);

				cmd.Clear();
				CommandBufferPool.Release(cmd);
			}

			// set texture sampling bounds for soft shadow.
			Vector4[] textureBounds = new Vector4[4];
			for (int i = 0; i < cascadeCount; i++)
				textureBounds[i] = new Vector4(rects[i].x / shadowResoluton.x, rects[i].y / shadowResoluton.y, rects[i].width / shadowResoluton.x, rects[i].height / shadowResoluton.x);


			Shader.SetGlobalVectorArray("_DirectionaLightTextureBounds", textureBounds);

			// set texture sampling bounds for soft shadow.
			Vector2 texelSize = new Vector2(rects[0].width / shadowResoluton.x, rects[0].height / shadowResoluton.x);
			Shader.SetGlobalVector("_DirectionalShadowTextureTexelSize", texelSize);

			//Shader.SetGlobalFloat("_DirectionaLightTextureAspect", shadowResoluton.y / shadowResoluton.x);
		}

		private void _RenderAdditionalLightShadow(RenderTexture target, HairSimulation hairSimulation, ScriptableRenderContext context, RenderingData renderingData, ShadowSliceData shadowSliceData, int lightIndex, float spotAngle)
		{
			RenderTexture shadowBuffer = target;

			float camScale = renderingData.cameraData.camera.pixelWidth / (float)shadowSliceData.resolution;
			float atlasScaleX = shadowBuffer == null ? 1 : shadowBuffer.width / (float)shadowSliceData.resolution;
			float atlasScaleY = shadowBuffer == null ? 1 : shadowBuffer.height / (float)shadowSliceData.resolution;
			float scaleX = (calcf(spotAngle, shadowSliceData.resolution) / 1000.0f) * camScale / atlasScaleX;
			float scaleY = (calcf(spotAngle, shadowSliceData.resolution) / 1000.0f) * camScale / atlasScaleY;

			MaterialPropertyBlock block = new MaterialPropertyBlock();

			block.SetBuffer("_HairPointData", hairSimulation.HairGeometry.HairPointBuffer);
			block.SetBuffer("_HairStrandData", hairSimulation.HairGeometry.HairStrandBuffer);
			block.SetInt("_StrandSegments", hairSimulation.BasePoints);

			block.SetFloat("_TextureWidth", scaleX);
			block.SetFloat("_TextureHeight", scaleY);
			block.SetInt("_LightIndex", lightIndex);
			//block.SetVector("_AdditionalLightViewport", new Vector4(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));

			CommandBuffer cmd = CommandBufferPool.Get("Hair Shadow To Additional Lights. " + lightIndex);
			cmd.Clear();

			cmd.SetRenderTarget(shadowBuffer, shadowBuffer);
			cmd.EnableScissorRect(new Rect(shadowSliceData.offsetX, shadowSliceData.offsetY, shadowSliceData.resolution, shadowSliceData.resolution));
			cmd.DrawProcedural(Matrix4x4.identity, _ShadowCasterMaterial, 1, MeshTopology.LineStrip, hairSimulation.HairGeometry.HairPointBuffer.count, 0, block);
			cmd.DisableScissorRect();

			context.ExecuteCommandBuffer(cmd);

			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}


		float calcf(float fov, float screenWidth)
		{
			float f = (screenWidth / (2 * Mathf.Tan(Mathf.PI * fov / 360)));
			return f;
		}
	}
}
