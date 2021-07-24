using System.Collections.Generic;
using UnityEngine;

namespace Avol
{
	[ExecuteInEditMode]
	public class HairStrand : MonoBehaviour
	{
		public float[]	Thickness				= null;
		public float[]	Stiffness				= null;
		public float[]	Retention				= null;

		public float	HairSegmentLength		= 0.0f;
		public float	Strands					= 1;
		public int		Points					= 0;
		public List<Color>	HairPointColors;


		public HairSimulation HairSimulation;

		private List<Vector3>	_HairPointPositions;
		private List<Vector3>	_HairPointTangents;
		private List<Vector3>	_HairPointBitangents;
		private List<Vector3>	_HairPointAngles;

		private GameObject[]	_HairPointHandles;


		public void Init()
		{
			_HairPointPositions						= new List<Vector3>();

			// create local positions relative to scalp world transform.
			for (int i = 0; i < transform.childCount; i++)
			{
				Transform childTransform = transform.GetChild(i).transform;

				// can be precalculated.
				Matrix4x4	localMatrix			= Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
				Vector3		strandPosition		= localMatrix.MultiplyPoint3x4(childTransform.localPosition);

				Matrix4x4	localMatrix2		= Matrix4x4.TRS(transform.parent.localPosition, transform.parent.localRotation, transform.parent.localScale);
							strandPosition		= localMatrix2.MultiplyPoint3x4(strandPosition);

				_HairPointPositions.Add(strandPosition);
			}

			// calculate root tangent, bitangent.
			{
				if (_HairPointPositions.Count > 0)
				{
					_HairPointTangents		= new List<Vector3>();
					for (int i = 0; i < _HairPointPositions.Count; i++)
						_HairPointTangents.Add(Vector3.zero);

					_HairPointBitangents	= new List<Vector3>();
					for (int i = 0; i < _HairPointPositions.Count; i++)
						_HairPointBitangents.Add(Vector3.zero);


					Vector3 normal	= (_HairPointPositions[1] - _HairPointPositions[0]).normalized;

					Vector3 c1 = Vector3.Cross(normal, new Vector3(0.0f, 0.0f, 1.0f));
					Vector3 c2 = Vector3.Cross(normal, new Vector3(0.0f, 1.0f, 0.0f));

					if ((c1).magnitude > (c2).magnitude)		_HairPointTangents[1] = c1.normalized;
					else										_HairPointTangents[1] = c2.normalized;

					_HairPointBitangents[1]	= Vector3.Cross(_HairPointTangents[1], normal).normalized;
				}
			}

			// calculate angular coeffs.
			{
				if (_HairPointPositions.Count > 0)
				{
					_HairPointAngles		= new List<Vector3>();
					for (int i = 0; i < _HairPointPositions.Count; i++)
						_HairPointAngles.Add(Vector2.zero);


					for (int i = 2; i < _HairPointPositions.Count; i++)
					{
						Vector3 normal = (_HairPointPositions[i] - _HairPointPositions[i - 1]).normalized;

 
						Vector3 prevTangent		= _HairPointTangents[i - 1].normalized;
						Vector3 prevBitangent	= _HairPointBitangents[i - 1].normalized;
						Vector3 prevNormal		= (_HairPointPositions[i - 1] - _HairPointPositions[i - 2]).normalized;

						float d0 = Vector3.Dot(normal, prevTangent);
						float d1 = Vector3.Dot(normal, prevBitangent);
						float d2 = Vector3.Dot(normal, prevNormal);

						_HairPointAngles[i] = new Vector3(d0, d1, d2);

						// calculate TBN
						Vector3 currentNormal		= (prevNormal * d2 + prevTangent * d0 + prevBitangent * d1).normalized;

						// note: produces small error.
						Vector3 currentTangent		= (prevTangent - currentNormal * d0).normalized;
						Vector3 currentBitangent	= (prevBitangent - currentNormal * d1).normalized;


						_HairPointTangents[i]		= currentTangent;
						_HairPointBitangents[i]		= currentBitangent;
					}
				}
			}
		}

		public void Create(float length, Vector3[] points, HairSimulation hairSimulation, Color[] colors, float[] thickness, float[] stiffness, float[] retention)
		{
			for (int i = transform.childCount - 1; i >= 0; i--)
				DestroyImmediate(transform.GetChild(i).gameObject);

			Points				= points.Length;
			HairSegmentLength	= (length / (float)points.Length);

			for (int i = 0; i < Points; i++)
				_CreateHairPointHandle(points[i], i);

			HairPointColors = new List<Color>();
			HairPointColors.AddRange(colors);

			Thickness = thickness;
			Stiffness = stiffness;
			Retention = retention;

			//_RestretchHair();
			Init();

			HairSimulation = hairSimulation;
		}

		private void _CreateHairPointHandle(Vector3 position, int ID)
		{
			GameObject handle = new GameObject("Handle " + ID);
			handle.transform.position = position;
			handle.transform.localScale = Vector3.one;
			handle.transform.SetParent(transform);
		}

		private void _RestretchHair()
		{
			for (int c = 1; c < transform.childCount; c++)
			{
				Vector3 dir = (transform.GetChild(c - 1).transform.position - transform.GetChild(c).transform.position).normalized;
				transform.GetChild(c).transform.position = transform.GetChild(c - 1).transform.position - dir * HairSegmentLength;
			}
		}

		public List<Vector3> GetPoints()
		{
			return _HairPointPositions;
		}

		public GameObject[] GetHandles()
		{
			if (_HairPointHandles == null)
			{
				_HairPointHandles = new GameObject[Points];
				for (int i = 0; i < Points; i++)
					_HairPointHandles[i] = transform.GetChild(i).gameObject;
			}
				
			return _HairPointHandles;
		}

		public List<Vector3> GetTangents()
		{
			return _HairPointTangents;
		}

		public List<Vector3> GetBiTangents()
		{
			return _HairPointBitangents;
		}

		public List<Vector3> GetAngles()
		{
			return _HairPointAngles;
		}
	}
}