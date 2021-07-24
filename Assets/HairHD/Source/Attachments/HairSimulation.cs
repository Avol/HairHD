using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace Avol
{
	public enum BrushSelectionMode
	{
		Point,
		Strand
	}

	public enum AmbientMode
	{
		Color,
		Sky,
		ReflectionsAndSky
	}

	[ExecuteInEditMode]
	public class HairSimulation : MonoBehaviour
	{
		[HideInInspector]		public bool								Drawing = false;

		public SphereCollider[]					SphereColliders;

		[HideInInspector]
		public		bool					RequiresRebuild			= true;
		public		bool					IsBuilding				= false;


		// EDITOR
		[HideInInspector]		public int					BasePoints						= 10;
		[HideInInspector]		public int					BaseDensity						= 3;
		[HideInInspector]		public float				BaseRootThickness				= 0.002f;
		[HideInInspector]		public float				BaseTipThickness				= 0.002f;
		[HideInInspector]		public float				BaseStiffness					= 0.0f;
		[HideInInspector]		public float				BaseRetention					= 0.75f;
		[HideInInspector]		public float				BaseLength						= 0.1f;
		[HideInInspector]		public float				BaseLengthVariation				= 0.0f;
		[HideInInspector]		public int					BaseBlendAngle					= 45;
		[HideInInspector]		public Color				BaseRootColor					= Color.white;
		[HideInInspector]		public Color				BaseTipColor					= Color.white;


		[HideInInspector]		public BrushType			BrushType						= BrushType.Move;
		[HideInInspector]		public MoveBrushType		MoveBrushType							= MoveBrushType.Drag;
		[HideInInspector]		public PropertyBrushType	PropertyBrushType						= PropertyBrushType.Length;

		[HideInInspector]		public float				BrushSize						= 0.1f;
		[HideInInspector]		public float				BrushOpacity					= 1.0f;
		[HideInInspector]		public float				BrushFrontInfluence				= 1.0f;
		[HideInInspector]		public float				BrushBackInfluence				= 1.0f;
		[HideInInspector]		public float				MinPoint						= 0;
		[HideInInspector]		public float				MaxPoint						= 0;

		[HideInInspector]		public float				BrushDragStiffness				= 0.0f;

		[HideInInspector]		public Color				BrushColor						= Color.white;
		[HideInInspector]		public Color				BrushRootColor					= Color.white;
		[HideInInspector]		public Color				BrushTipColor					= Color.white;

		[HideInInspector]		public float				BrushThickness					= 0.002f;
		[HideInInspector]		public float				BrushStiffness					= 0.5f;
		[HideInInspector]		public float				BrushRetention					= 0.5f;
		
		[HideInInspector]		public float				BrushLength						= 0.1f;
		[HideInInspector]		public float				BrushLengthVariation			= 0.0f;

		[HideInInspector]		public BrushSelectionMode	BrushSelectionMode				= BrushSelectionMode.Point;

		[HideInInspector]		public bool					PreviewHairPositions			= false;



		// SHADING
		[HideInInspector]		public Color				ScatterTint						= new Color(1.0f, 1.0f, 1.0f, 0.1f);
		[HideInInspector]		public float				Roughness						= 0;
		[HideInInspector]		public float				Shift							= 0;
		[HideInInspector]		public float				SelfShadowingOpacity			= 0.05f;

		// PHYSICS
		[HideInInspector]		public Vector3				Gravity							= new Vector3(0, -0.01f, 0);
		[HideInInspector]		public float				Damping							= 0.8f;
		[HideInInspector]		public float				Stiffness						= 1.0f;
		[HideInInspector]		public float				Retention						= 1.0f;

		[HideInInspector]		public bool					Wind							= false;

		[HideInInspector]		public Vector3				WindDirection					= Vector3.zero;
		[HideInInspector]		public Vector3				WindDirection2					= Vector3.zero;

		[HideInInspector]		public float				WindTurbulance					= 0.5f;
		[HideInInspector]		public float				WindTurbulance2					= 0.5f;

		[HideInInspector]		public Vector3				WindFrequency						= Vector3.one;
		[HideInInspector]		public Vector3				WindFrequency2						= Vector3.one;


		[HideInInspector]		public bool					SelfCollision;


		public		CommandBuffer		ShadowCommandBuffer				{ get; private set; }
		private		List<Vector3>		_HairPoints						= new List<Vector3>();

		public		HairGeometry			HairGeometry				{ get; private set; }
		public		HairPhysics				HairPhysics					{ get; private set; }

		public		HairBuilder				HairBuilder					{ get; private set; }

		private		ComputeShader		_TransformCompute;

		private		int					_TransformLocalToWorldAllKernel;
		private		int					_TransformLocalToWorldKernel;
		private		int					_TransformWorldToLocalKernel;

		private		bool				_TransformAll							= true;

		private		BoundingSphere		_BoundingSphere;
		private		Transform			_BoundingSphereCenter;


		public		bool				Initialized							{ get; private set; }

		private		bool				_EnableNextFrame					= false;

		private		bool				_CurrentSpaceWorld					= false;



		public void Init()
		{
			// create hair builder
			if (HairBuilder == null)
				HairBuilder = new HairBuilder(this);

			// compute hair geometry.
			if (HairGeometry == null)
				HairGeometry = new HairGeometry(this);

			// compute hair physics.
			if (HairPhysics == null)
				HairPhysics = new HairPhysics(this);


			// hair transform compute & kernels
			_TransformCompute = Resources.Load<ComputeShader>("Avol/Hair/Shaders/Core/GeometryTransform");
			_TransformLocalToWorldAllKernel = _TransformCompute.FindKernel("HairLocalToWorldAll");
			_TransformLocalToWorldKernel = _TransformCompute.FindKernel("HairLocalToWorld");
			_TransformWorldToLocalKernel = _TransformCompute.FindKernel("HairWorldToLocal");

			// recalculate bounding sphere.
			GetBoundingSphere(true);
		}

		public void InitBuffers()
		{
			if (!Application.isPlaying)
				return;

			if (!Initialized)
			{
				_TransformAll = true;

				HairGeometry.ComputeGeometryBuffer();
				HairPhysics.GenerateBuffers();

				Initialized = true;
			}
		}



		public void ReleaseBuffers()
		{
			if (HairGeometry != null)
				HairGeometry.ReleaseBuffers();

			if (HairPhysics != null)
				HairPhysics.ReleaseBuffers();
		}

		public BoundingSphere GetBoundingSphere(bool recalculate = false)
		{
			// recalculate sphere size.
			if (recalculate)
			{
				// calculate camera bounds based on geometry.
				// get center position.
				Vector3 center = Vector3.zero;
				for (int i = 0; i < HairGeometry.HairStrands.Count; i++)
					center += HairGeometry.HairStrands[i].transform.position;

				if (HairGeometry.HairStrands.Count > 0)
					center /= HairGeometry.HairStrands.Count;
				else center = transform.position;

				// get bounding sphere.
				float longestDistance = 0;
				for (int i = 0; i < HairGeometry.HairStrands.Count; i++)
				{
					HairStrand strand = HairGeometry.HairStrands[i];
					float distance = (center - strand.transform.position).magnitude;
					distance += strand.HairSegmentLength * strand.Points;

					if (distance > longestDistance)
						longestDistance = distance;
				}

				_BoundingSphere.position = center;
				_BoundingSphere.radius = longestDistance;

				// create local center object for position update in further transform.
				_BoundingSphereCenter = transform.Find("Bounds Center");
				if (_BoundingSphereCenter == null)
				{
					_BoundingSphereCenter = new GameObject("Bounds Center").transform;
					_BoundingSphereCenter.SetParent(transform);
				}

				_BoundingSphereCenter.transform.localPosition = Vector3.zero;
				_BoundingSphereCenter.transform.localScale = Vector3.one;
				if (_BoundingSphereCenter == null)
				{
					GameObject boundingSphereCenter = new GameObject("Bounds Center");
					boundingSphereCenter.transform.SetParent(transform);
					boundingSphereCenter.transform.position = center;
					_BoundingSphereCenter = boundingSphereCenter.transform;
				}
			}

			// restore position always.
			_BoundingSphere.position = _BoundingSphereCenter.transform.position;

			return _BoundingSphere;
		}


		private void _TransformToLocal()
		{
			if (_CurrentSpaceWorld)
			{
				_TransformCompute.SetMatrix("_GlobalTransform", transform.localToWorldMatrix);
				_TransformCompute.SetMatrix("_GlobalTransformInverse", transform.worldToLocalMatrix);
				_TransformCompute.SetInt("_StrandPoints", HairGeometry.HairPointBuffer.count / HairGeometry.HairStrandBuffer.count);
				_TransformCompute.SetBuffer(_TransformWorldToLocalKernel, "_HairPointData", HairGeometry.HairPointBuffer);
				_TransformCompute.SetBuffer(_TransformWorldToLocalKernel, "_HairStrandData", HairGeometry.HairStrandBuffer);


				_TransformCompute.Dispatch(_TransformWorldToLocalKernel, HairGeometry.HairStrandBuffer.count / 1, 1, 1);
				_CurrentSpaceWorld = false;
			}
		}

		private void _TransformToWorld()
		{
			if (!_CurrentSpaceWorld)
			{
				_TransformCompute.SetMatrix("_GlobalTransform", transform.localToWorldMatrix);
				_TransformCompute.SetMatrix("_GlobalTransformInverse", transform.worldToLocalMatrix);
				_TransformCompute.SetInt("_StrandPoints", HairGeometry.HairPointBuffer.count / HairGeometry.HairStrandBuffer.count);


				_TransformCompute.SetBuffer(_TransformLocalToWorldAllKernel, "_HairPointData", HairGeometry.HairPointBuffer);
				_TransformCompute.SetBuffer(_TransformLocalToWorldAllKernel, "_HairStrandData", HairGeometry.HairStrandBuffer);

				_TransformCompute.SetBuffer(_TransformLocalToWorldKernel, "_HairPointData", HairGeometry.HairPointBuffer);
				_TransformCompute.SetBuffer(_TransformLocalToWorldKernel, "_HairStrandData", HairGeometry.HairStrandBuffer);

				if (_TransformAll)	_TransformCompute.Dispatch(_TransformLocalToWorldAllKernel, HairGeometry.HairPointBuffer.count / 1, 1, 1);
				else				_TransformCompute.Dispatch(_TransformLocalToWorldKernel, HairGeometry.HairStrandBuffer.count / 1, 1, 1);

				_CurrentSpaceWorld	= true;
				_TransformAll		= false;
			}
		}

		private IEnumerator _EndFrame()
		{
			yield return new WaitForEndOfFrame();

			_TransformToLocal();
			StartCoroutine(_EndFrame());
		}



		private void Start()
		{
			HairRenderer.AddHair(this);

			StartCoroutine(_EndFrame());
		}

		private void Update()
		{
			if (HairBuilder != null)
				HairBuilder.Update();

			if (_EnableNextFrame)
			{
				HairRenderer.AddHair(this);
				_EnableNextFrame = false;
			}


		}

        private void LateUpdate()
        {
			if (Initialized)
				_TransformToWorld();
		}


        private void OnDrawGizmosSelected()
		{
			if (HairBuilder != null)
				HairBuilder.DrawGizmos();
		}

		private void OnEnable()
		{
			Init();
			_EnableNextFrame = true;
		}
 
		private void OnDisable() 
		{
			HairRenderer.RemoveHair(this);
			_EnableNextFrame = false;
		}

		private void OnDestroy()
		{ 
			HairRenderer.RemoveHair(this);
			ReleaseBuffers(); 
		}

		private void OnApplicationQuit()
		{
			ReleaseBuffers();
		}
	}
}