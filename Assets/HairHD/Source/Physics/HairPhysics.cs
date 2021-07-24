using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Avol
{
	public struct SphereColliderBufferData
	{
		public Vector3 Position;
		public float Radius;

		public SphereColliderBufferData(Vector3 position, float radius)
		{
			Position	= position;
			Radius		= radius;
		}
	}

	public class HairPhysics
	{
		private		HairSimulation						_HairSimulation							= null;


		private		ComputeShader						_PhysicsCompute							= null;
		private		int									_PhysicsComputeKernel					= -1;

		private		ComputeShader						_OccupancyCompute						= null;
		private		int									_OccupancyComputeKernel					= -1;


		private		List<SphereColliderBufferData>		_SphereColliderBuffer					= new List<SphereColliderBufferData>();
		private		ComputeBuffer						_SphereColliderComputeBuffer			= null;

		public		RenderTexture						OccupancyMap;

		public		Vector3								Center;
		public		Vector3								Bounds;


		public HairPhysics(HairSimulation hairSimulation)
		{
			_HairSimulation				= hairSimulation;
		
			// occupancy
			_OccupancyCompute					= Resources.Load<ComputeShader>("Avol/Hair/Shaders/Physics/HairOccupancy");
			_OccupancyComputeKernel				= _OccupancyCompute.FindKernel("HairOccupancy");

			OccupancyMap						= new RenderTexture(64, 64, 0, RenderTextureFormat.RInt);
			OccupancyMap.dimension				= UnityEngine.Rendering.TextureDimension.Tex3D;
			OccupancyMap.volumeDepth			= 64;
			OccupancyMap.filterMode				= FilterMode.Point;
			OccupancyMap.enableRandomWrite		= true;
			OccupancyMap.Create();
 
			// final physics
			_PhysicsCompute				= Resources.Load<ComputeShader>("Avol/Hair/Shaders/Physics/HairPhysics");
			_PhysicsComputeKernel		= _PhysicsCompute.FindKernel("HairPhysics");
		}


		private void _ComputeColliderBuffers(SphereCollider[] Colliders)
		{
			if (Colliders.Length > 0)
			{
				_SphereColliderBuffer.Clear();

				for (int i = 0; i < Colliders.Length; i++)
				{
					Vector3 scaleAllAxes = Colliders[i].transform.lossyScale;
					float maxScale = Mathf.Max(Mathf.Max(scaleAllAxes.x, scaleAllAxes.y), scaleAllAxes.z);
					_SphereColliderBuffer.Add(new SphereColliderBufferData(Colliders[i].transform.position, Colliders[i].radius * maxScale + 0.01f));
				}

				if (_SphereColliderComputeBuffer == null)
					_SphereColliderComputeBuffer = new ComputeBuffer(_SphereColliderBuffer.Count, sizeof(float) * 3 + sizeof(float), ComputeBufferType.Default);

				_SphereColliderComputeBuffer.SetData(_SphereColliderBuffer);
			}

			if (_SphereColliderComputeBuffer != null)
				_PhysicsCompute.SetBuffer(_PhysicsComputeKernel, "_SphereColliders", _SphereColliderComputeBuffer);

			_PhysicsCompute.SetInt("_SphereColliderCount", Colliders.Length);
		}

		private void _ComputeBoundingBox(List<HairStrand> hairStrands)
		{
			Center = Vector3.zero;

			for (int i = 0; i < hairStrands.Count; i++)
				Center += hairStrands[i].transform.position;

			Center /= hairStrands.Count;

			float maxLength = 0;
			for (int i = 0; i < hairStrands.Count; i++)
			{
				float length = hairStrands[i].HairSegmentLength * hairStrands[i].Points;

				if (length > maxLength)
					maxLength = length;
			}

			for (int i = 0; i < hairStrands.Count; i++)
			{
				float distX = Mathf.Abs(Center.x - hairStrands[i].transform.position.x) + maxLength;
				float distY = Mathf.Abs(Center.y - hairStrands[i].transform.position.y) + maxLength;
				float distZ = Mathf.Abs(Center.z - hairStrands[i].transform.position.z) + maxLength;

				if (distX > Bounds.x) Bounds.x = distX;
				if (distY > Bounds.y) Bounds.y = distY;
				if (distZ > Bounds.z) Bounds.z = distZ;
			}

			//Debug.Log(_Bounds.x);
		}


		public void GenerateBuffers()
		{
			_ComputeColliderBuffers(_HairSimulation.SphereColliders);

			_ComputeBoundingBox(_HairSimulation.HairGeometry.HairStrands);

			_OccupancyCompute.SetTexture(_OccupancyComputeKernel, "_OccupancyMap", OccupancyMap);
			_PhysicsCompute.SetInt("_StrandSegments", _HairSimulation.BasePoints);
			_PhysicsCompute.SetTexture(_PhysicsComputeKernel, "_OccupancyMap", OccupancyMap);

			_PhysicsCompute.SetBuffer(_PhysicsComputeKernel, "_HairPointData", _HairSimulation.HairGeometry.HairPointBuffer);
			_PhysicsCompute.SetBuffer(_PhysicsComputeKernel, "_HairStrandData", _HairSimulation.HairGeometry.HairStrandBuffer);
		}

		public void ReleaseBuffers()
		{
			if (_SphereColliderComputeBuffer != null)
				_SphereColliderComputeBuffer.Release();

			if (OccupancyMap != null)
				OccupancyMap.Release();
		}


		public void Compute()
		{
			#if (UNITY_EDITOR)
				if (!Application.isPlaying || EditorApplication.isPaused)
					return;
			#endif

			// compute occupancy map
			if (_HairSimulation.SelfCollision)
			{
				// fill occupancy
				// TODO: use clear shader instead not to mess with RT buffers.
				RenderTexture rt = UnityEngine.RenderTexture.active;
				RenderTexture.active = OccupancyMap;
				GL.Clear(true, true, Color.clear);
				RenderTexture.active = rt;

				_OccupancyCompute.SetVector("_Center", Center);
				_OccupancyCompute.SetVector("_Bounds", Bounds);
				_OccupancyCompute.Dispatch(_OccupancyComputeKernel, _HairSimulation.HairGeometry.HairPointBuffer.count / 1, 1, 1);
			}

			// compute final physics.
			{
				_ComputeColliderBuffers(_HairSimulation.SphereColliders);

				_OccupancyCompute.SetTexture(_OccupancyComputeKernel, "_OccupancyMap", OccupancyMap);
				_PhysicsCompute.SetInt("_StrandSegments", _HairSimulation.BasePoints);
				_PhysicsCompute.SetTexture(_PhysicsComputeKernel, "_OccupancyMap", OccupancyMap);

				_PhysicsCompute.SetBuffer(_PhysicsComputeKernel, "_HairPointData", _HairSimulation.HairGeometry.HairPointBuffer);
				_PhysicsCompute.SetBuffer(_PhysicsComputeKernel, "_HairStrandData", _HairSimulation.HairGeometry.HairStrandBuffer);

				_PhysicsCompute.SetBool("_SelfCollision", _HairSimulation.SelfCollision);
				_PhysicsCompute.SetVector("_Center", Center);
				_PhysicsCompute.SetVector("_Bounds", Bounds);

				_PhysicsCompute.SetFloat("_Time", Time.realtimeSinceStartup);
				_PhysicsCompute.SetFloat("_DeltaTime", Time.deltaTime);


				_PhysicsCompute.SetVector("_Gravity", _HairSimulation.Gravity);
				_PhysicsCompute.SetFloat("_Damping", _HairSimulation.Damping);
				_PhysicsCompute.SetFloat("_Stiffness", _HairSimulation.Stiffness);
				_PhysicsCompute.SetFloat("_Retention", _HairSimulation.Retention);

				_PhysicsCompute.SetBool("_Wind", _HairSimulation.Wind);
				_PhysicsCompute.SetVector("_WindDirection", _HairSimulation.WindDirection);
				_PhysicsCompute.SetVector("_WindDirection2", _HairSimulation.WindDirection2);

				_PhysicsCompute.SetFloat("_WindTurbulance", _HairSimulation.WindTurbulance);
				_PhysicsCompute.SetFloat("_WindTurbulance2", _HairSimulation.WindTurbulance2);

				_PhysicsCompute.SetVector("_WindFrequency", _HairSimulation.WindFrequency);
				_PhysicsCompute.SetVector("_WindFrequency2", _HairSimulation.WindFrequency2);

				_PhysicsCompute.Dispatch(_PhysicsComputeKernel, _HairSimulation.HairGeometry.HairPointBuffer.count / _HairSimulation.BasePoints, 1, 1);
			}
		}
	}
}
