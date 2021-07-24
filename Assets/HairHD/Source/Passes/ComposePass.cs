using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Avol
{
	public class ComposePass : ScriptableRenderPass
	{
		private Material			_ComposeMaterial;
		private HairRenderer		_HairRenderer;
		private ScriptableRenderer	_ScriptableRenderer;


		public void Setup(HairRenderer hairRenderer, ScriptableRenderer renderer)
		{
			_HairRenderer			= hairRenderer;
			_ScriptableRenderer		= renderer;
			_ComposeMaterial		= new Material(Resources.Load<Shader>("Avol/Hair/Shaders/Core/HairCompose"));
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
			_ComposeMaterial.DisableKeyword("MSAA_0");
			_ComposeMaterial.DisableKeyword("MSAA_1");
			_ComposeMaterial.DisableKeyword("MSAA_2");
			_ComposeMaterial.DisableKeyword("MSAA_3");
			_ComposeMaterial.EnableKeyword("MSAA_" + (int)_HairRenderer.Antialiasing);

			_ComposeMaterial.DisableKeyword("COMPARE_DEPTH");
			if (_HairRenderer.UniversalRendererData.renderingMode != RenderingMode.Forward ||
				_HairRenderer.UniversalRenderPipelineAsset.supportsCameraDepthTexture)
				_ComposeMaterial.EnableKeyword("COMPARE_DEPTH");

			_ComposeMaterial.SetTexture("_HairDepth", _HairRenderer.HairScreenBuffers.FinalDepthRT);
			_ComposeMaterial.SetTexture("_HairColorRT", _HairRenderer.HairScreenBuffers.FinalColorSwapRT);

			// force enable camera depth texture.
			_HairRenderer.UniversalRenderPipelineAsset.supportsCameraDepthTexture = renderingData.cameraData.requiresDepthTexture;

			// compose forward or with depth texture.
			if (_HairRenderer.UniversalRendererData.renderingMode == RenderingMode.Forward ||
				renderingData.cameraData.requiresDepthTexture)
			{
				renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

				CommandBuffer cmd = CommandBufferPool.Get("Compose hair to screen");

				if (QualitySettings.antiAliasing == 0 || _HairRenderer.UniversalRendererData.renderingMode == RenderingMode.Deferred)
				{
					// copy to temporary
					{
						cmd.Blit(_ScriptableRenderer.cameraColorTarget, _HairRenderer.HairScreenBuffers.FinalColorDeferredRT);
						context.ExecuteCommandBuffer(cmd);
						cmd.Clear();
					}

					// write to color & depth.
					{
						_ComposeMaterial.SetTexture("_CameraDepthAttachment", (RenderTexture)Shader.GetGlobalTexture("_CameraDepthAttachment"));
						cmd.Blit(_HairRenderer.HairScreenBuffers.FinalColorDeferredRT, _ScriptableRenderer.cameraColorTarget, _ComposeMaterial, 0);
						context.ExecuteCommandBuffer(cmd);
						cmd.Clear();
					}

					// write separetly to depth texture if used by pipeline.
					if (renderingData.cameraData.requiresDepthTexture)
					{
						cmd.SetRenderTarget((RenderTexture)Shader.GetGlobalTexture("_CameraDepthAttachment"), (RenderTexture)Shader.GetGlobalTexture("_CameraDepthAttachment"));
						cmd.Blit(_HairRenderer.HairScreenBuffers.FinalColorDeferredRT, (RenderTexture)Shader.GetGlobalTexture("_CameraDepthAttachment"), _ComposeMaterial, 1);
						context.ExecuteCommandBuffer(cmd);
						cmd.Clear();
					}
				}
				else
                {
					Debug.LogError("MSAA not supported. Please use post process antialiasing.");
				}

				CommandBufferPool.Release(cmd);
			}

			// compose deferred without depth texture.
			else
			{
				renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

				CommandBuffer cmd = CommandBufferPool.Get("Compose hair to screen");

				_ComposeMaterial.SetTexture("_CameraDepthAttachment", (RenderTexture)Shader.GetGlobalTexture("_CameraDepthAttachment"));

				// clear temporary rt.
				{
					cmd.SetRenderTarget(_HairRenderer.HairScreenBuffers.FinalColorDeferredRT);
					cmd.ClearRenderTarget(true, true, Color.clear);
					context.ExecuteCommandBuffer(cmd);
					cmd.Clear();
				}

				// write to temporary rt.
				{
					cmd.Blit(_ScriptableRenderer.cameraColorTarget, _HairRenderer.HairScreenBuffers.FinalColorDeferredRT, _ComposeMaterial, 0);
					context.ExecuteCommandBuffer(cmd);
					cmd.Clear();
				}

				// write to camera
				{
					cmd.Blit(_HairRenderer.HairScreenBuffers.FinalColorDeferredRT, _ScriptableRenderer.cameraColorTarget, _ComposeMaterial, 0);
					context.ExecuteCommandBuffer(cmd);
					cmd.Clear();
				}

				// write separetly to depth texture.
				{
					cmd.SetRenderTarget((RenderTexture)Shader.GetGlobalTexture("_CameraDepthAttachment"), (RenderTexture)Shader.GetGlobalTexture("_CameraDepthAttachment"));
					cmd.Blit(_HairRenderer.HairScreenBuffers.FinalColorDeferredRT, (RenderTexture)Shader.GetGlobalTexture("_CameraDepthAttachment"), _ComposeMaterial, 1);
					context.ExecuteCommandBuffer(cmd);
					cmd.Clear();
				}

				CommandBufferPool.Release(cmd);
			}
		}
	}
}