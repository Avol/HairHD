using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;


namespace Avol
{
	public enum HAIR_MSAA
	{
		Off,
		_2X,
		_4X,
		_8X
	}

	public enum SelfShadowResolution
	{
		High,
		Medium,
		Low,
	}

	public enum SoftShadowJitterQuality
	{
		High,
		Medium,
		Low,
	}

	public enum OpacityLayerDistribution
    {
		Linear,
		Exponential,
		Fibonacci
    }

	[System.Serializable]
	public struct HairScreenBuffers
    {
		public		RenderTexture			FinalColorRT;
		public		RenderTexture			FinalColorSwapRT;
		public		RenderTexture			FinalColorDeferredRT;
		public		RenderTexture			FinalDepthRT;

		public		RenderTexture			TempTangentRT;
		public		RenderTexture			TempColorRT;
		public		RenderTexture			TempScatterTintRT;
    }

	public class HairRenderer : ScriptableRendererFeature
	{
		[Header("References")]
		public		UniversalRendererData			UniversalRendererData				= null;
		public		UniversalRenderPipelineAsset	UniversalRenderPipelineAsset		= null;



		[Header("Antialiasing")]

		[Tooltip("Multi sampled antialiasing.")]
		public		HAIR_MSAA					Antialiasing					= HAIR_MSAA.Off;

		[Tooltip("Stoschastic hair transparency. More antialiasing allows for more transparency. If antialiasing is disabled, transparency won't work.")]
		[Range(0.0f, 1.0f)]
		public		float						Transparency					= 1;



		[Header("Shadows")]

		[Tooltip("Self shadowing is resource intensive, but provides a realistic self shadowing effect.")]
		public		bool						SelfShadowing					= true;

		[Tooltip("Lower resolution yields better performance and in combination with soft shadow softer results.")]
		public		SelfShadowResolution		SelfShadowDirectionalResolution	= SelfShadowResolution.Low;

		[Tooltip("Lower resolution yields better performance and in combination with soft shadow softer results.")]
		public		SelfShadowResolution		SelfShadowAdditionalResolution	= SelfShadowResolution.Low;

		[Tooltip("Transparent shadow strength")]
		[Range(0.0f, 1.0f)]
		public		float						SelfShadowingStrength			= 1.0f;

		//public		SoftShadowJitterQuality		SelfShadowJitterQuality			= SoftShadowJitterQuality.High;

		[Tooltip("How much sampling points to be used for soft shadow.")]
		[Range(1, 16)]
		public		int							SelfShadowingSamples			= 4;

		[Tooltip("How much sampling points should be jittered. Higher value softer results but more noise.")]
		[Range(0.0f, 1.0f)]
		public		float						SelfShadowingJitter				= 0.1f;

		[Tooltip("How far self shadowing interpolates into full black.")]
		[Min(0.0001f)]
		public		float						SelfShadowRange					= 2;




		[HideInInspector]
		public		HairScreenBuffers		HairScreenBuffers;


		private		static		BoundingSphere[]		_HairBounds				= new BoundingSphere[1000];
		private		static		CullingGroup			_HairCullingGroup;

		public		static		List<HairSimulation>	Hairs					= new List<HairSimulation>();
		public		static		List<HairSimulation>	VisibleHairs			= new List<HairSimulation>();


		public		DOMPass			DOMPass;
		public		ShadowPass		ShadowPass;

		private		HairPass		_HairPass;
		private		ComposePass		_ComposePass;



		public override void Create()
		{
			if (Application.isPlaying)
			{
				Dispose(true);

				_CreateCullingGroup();

				DOMPass				= new DOMPass();
				ShadowPass			= new ShadowPass();
				_HairPass			= new HairPass();
				_ComposePass		= new ComposePass();

				RebuildBuffers(100, 100);
			}
		}

        protected override void Dispose(bool disposing)
        {
			if (DOMPass != null) 
				DOMPass.Dispose();

			if (HairScreenBuffers.FinalColorDeferredRT != null)			HairScreenBuffers.FinalColorDeferredRT.Release();
			if (HairScreenBuffers.FinalColorRT != null)					HairScreenBuffers.FinalColorRT.Release();
			if (HairScreenBuffers.FinalColorSwapRT != null)				HairScreenBuffers.FinalColorSwapRT.Release();
			if (HairScreenBuffers.FinalDepthRT != null)					HairScreenBuffers.FinalDepthRT.Release();
			if (HairScreenBuffers.TempColorRT != null)					HairScreenBuffers.TempColorRT.Release();
			if (HairScreenBuffers.TempScatterTintRT != null)			HairScreenBuffers.TempScatterTintRT.Release();
			if (HairScreenBuffers.TempTangentRT != null)				HairScreenBuffers.TempTangentRT.Release();

			if (_HairCullingGroup != null)
			{
				_HairCullingGroup.Dispose();
				_HairCullingGroup = null;
			}

			base.Dispose(disposing);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (Application.isPlaying)
			{
				_HairCullingGroup.targetCamera = renderingData.cameraData.camera;

				if (renderingData.cameraData.camera.cameraType == CameraType.Game)
				{
					if (Hairs.Count > 0)
					{
						if (SelfShadowing)
						{
							DOMPass.Setup(this, renderer, renderingData);
							renderer.EnqueuePass(DOMPass);
						}

						_HairPass.Setup(this, renderer);
						renderer.EnqueuePass(_HairPass);

						ShadowPass.Setup(this, renderer);
						renderer.EnqueuePass(ShadowPass);

						_ComposePass.Setup(this, renderer);
						renderer.EnqueuePass(_ComposePass);
					}
				}
			}
		}


		public void RebuildBuffers(int width, int heigth)
		{
			if (Application.isPlaying)
			{
				if (HairScreenBuffers.FinalColorRT == null)
				{
					HairScreenBuffers = new HairScreenBuffers();
				}
				else
				{
					HairScreenBuffers.FinalColorRT.Release();
					HairScreenBuffers.FinalColorSwapRT.Release();
					HairScreenBuffers.FinalColorDeferredRT.Release();

					HairScreenBuffers.FinalDepthRT.Release();
					HairScreenBuffers.FinalColorRT.Release();

					HairScreenBuffers.TempTangentRT.Release();
					HairScreenBuffers.TempColorRT.Release();
					HairScreenBuffers.TempScatterTintRT.Release();
				}


				HairScreenBuffers.FinalColorRT = new RenderTexture(width, heigth, 0, RenderTextureFormat.ARGB32);
				HairScreenBuffers.FinalColorSwapRT = new RenderTexture(width, heigth, 0, RenderTextureFormat.ARGB32);
				HairScreenBuffers.FinalColorDeferredRT = new RenderTexture(width, heigth, 24, RenderTextureFormat.Default);

				HairScreenBuffers.FinalDepthRT = new RenderTexture(width, heigth, 24, RenderTextureFormat.RHalf);

				HairScreenBuffers.TempTangentRT = new RenderTexture(width, heigth, 0, RenderTextureFormat.ARGB32);
				HairScreenBuffers.TempColorRT = new RenderTexture(width, heigth, 0, RenderTextureFormat.ARGB32);
				HairScreenBuffers.TempScatterTintRT = new RenderTexture(width, heigth, 0, RenderTextureFormat.ARGB32);


				if (Antialiasing != HAIR_MSAA.Off)
				{
					int antialiasing = Mathf.Max(1, (int)Mathf.Pow(2, (int)Antialiasing));

					HairScreenBuffers.TempTangentRT.antiAliasing = antialiasing;
					HairScreenBuffers.TempTangentRT.bindTextureMS = true;

					HairScreenBuffers.TempColorRT.antiAliasing = antialiasing;
					HairScreenBuffers.TempColorRT.bindTextureMS = true;

					HairScreenBuffers.TempScatterTintRT.antiAliasing = antialiasing;
					HairScreenBuffers.TempScatterTintRT.bindTextureMS = true;

					HairScreenBuffers.FinalDepthRT.antiAliasing = antialiasing;
					HairScreenBuffers.FinalDepthRT.bindTextureMS = true;
				}

				for (int i = 0; i < Hairs.Count; i++)
					if (Hairs[i].Initialized)
						Hairs[i].InitBuffers();
			}
		}


		public static void AddHair(HairSimulation hair)
		{
			if (Application.isPlaying)
			{
				if (Hairs.Contains(hair))
				{
					hair.InitBuffers();
				}
				else
				{
					Hairs.Add(hair);
					hair.InitBuffers();

					_CreateCullingGroup();
					_HairBounds[Hairs.Count - 1] = hair.GetBoundingSphere();
					_HairCullingGroup.SetBoundingSphereCount(Hairs.Count);
				}
			}
		}

		public static void RemoveHair(HairSimulation hair)
		{
			if (Application.isPlaying)
			{
				bool removed = Hairs.Remove(hair);

				if (removed)
				{
					_CreateCullingGroup();

					for (int i = 0; i < Hairs.Count; i++)
						_HairBounds[i] = Hairs[i].GetBoundingSphere();

					_HairCullingGroup.SetBoundingSphereCount(Hairs.Count);
				}
			}
		}


		private static void _CreateCullingGroup()
        {
			if (Application.isPlaying)
			{
				if (_HairCullingGroup == null)
				{
					_HairCullingGroup = new CullingGroup();
					_HairCullingGroup.SetBoundingSpheres(_HairBounds);
					_HairCullingGroup.SetBoundingSphereCount(Hairs.Count);
					_HairCullingGroup.onStateChanged = _HairCullingStateChange;
				} 
			}
		}

		private static void _HairCullingStateChange(CullingGroupEvent evt)
		{
			if (Hairs.Count > 0)
			{
				if			(evt.hasBecomeVisible)		VisibleHairs.Add(Hairs[evt.index]);
				else if		(evt.hasBecomeInvisible)	VisibleHairs.Remove(Hairs[evt.index]);
			}
		}
	}
}