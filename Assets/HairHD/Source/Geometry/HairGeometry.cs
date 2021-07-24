using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Avol
{
	public struct HairPointData
	{
		public Vector3 Position;
		public Vector3 PrevPosition;
		public Vector3 RotationAngles;
		public Vector3 Color;

		public int StrandID;
		public int StrandPoint;
		public float StrandThickness;
		public float StrandStiffness;
		public float Retention;

		public HairPointData(Vector3 position, Vector3 rotationAngles, Color color, int strandID, int strandPoint, float strandThickness, float strandStiffness, float retention)
		{
			StrandID				= strandID;

			Position				= position;
			PrevPosition			= position;
			RotationAngles			= rotationAngles;

			StrandPoint				= strandPoint;
			StrandThickness			= strandThickness;
			StrandStiffness			= strandStiffness;
			Retention				= retention;

			Color					= new Vector3(color.r, color.g, color.b);
		}
	}

	public struct HairStrandData
	{
		public Vector3	Tangent;
		public Vector3	Bitangent;
		public float	SegmentLength;

		public HairStrandData(Vector3 tangent, Vector3 bitangent, float segmentLength)
		{
			Tangent			= tangent;
			Bitangent		= bitangent;
			SegmentLength	= segmentLength;
		}
	}

	public class HairGeometry
	{
		public		bool					Drawing						= false;
		public		List<HairStrand>		HairStrands					{ get; private set; }
		public		ComputeBuffer			HairPointBuffer				{ get; private set; }
		public		ComputeBuffer			HairStrandBuffer			{ get; private set; }

		private		HairSimulation			_HairSimulation				= null;


		public HairGeometry(HairSimulation hairSimulation)
		{
			_HairSimulation = hairSimulation;

			HairStrands = new List<HairStrand>();

			_GetStrands();
		}


		private void _GetStrands()
		{
			HairStrands.Clear();

			Transform hairStrandsContainer = _HairSimulation.transform.Find("Hair Strands");
			if (hairStrandsContainer == null)
			{
				GameObject strandsContainer = new GameObject("Hair Strands");
				strandsContainer.transform.SetParent(_HairSimulation.transform);
				hairStrandsContainer = strandsContainer.transform;
			}

			for (int i = 0; i < hairStrandsContainer.transform.childCount; i++)
			{
				HairStrand hairStrand = hairStrandsContainer.transform.GetChild(i).GetComponent<HairStrand>();
				if (hairStrand != null)
				{
					hairStrand.Init();
					HairStrands.Add(hairStrand);
				}
			}
		}

		public void ComputeGeometryBuffer()
		{
			// create buffer data
			List<HairPointData>				hairPointData				= new List<HairPointData>();
			List<HairStrandData>			hairStrandData				= new List<HairStrandData>();

			for (int i = 0; i < HairStrands.Count; i++)
			{
				List<Vector3>		localPosition				= HairStrands[i].GetPoints();

				int index = 0;
				for (int c = 0; c < localPosition.Count; c++)
				{

					hairPointData.Add(new HairPointData(localPosition[c], HairStrands[i].GetAngles()[c], HairStrands[i].HairPointColors[c], index, c, HairStrands[i].Thickness[c], HairStrands[i].Stiffness[c], HairStrands[i].Retention[c]));
					index++;
				}

				hairStrandData.Add(new HairStrandData(HairStrands[i].GetTangents()[1], HairStrands[i].GetBiTangents()[1], HairStrands[i].HairSegmentLength));
			}

			// create compute buffers
			if (hairPointData.Count != 0)
			{
				ReleaseBuffers();

				HairPointBuffer = new ComputeBuffer(hairPointData.Count, Marshal.SizeOf(typeof(HairPointData)), ComputeBufferType.Default);
				HairPointBuffer.SetData(hairPointData);

				HairStrandBuffer = new ComputeBuffer(hairStrandData.Count, Marshal.SizeOf(typeof(HairStrandData)), ComputeBufferType.Default);
				HairStrandBuffer.SetData(hairStrandData);
			}
			else
			{
				Debug.LogWarning("Hair simulation (" + _HairSimulation.name + ") has no geometry. Considering removing the simulation or adding geometry to it.", _HairSimulation.gameObject);
			}
		}

		public void ClearHairStrands()
		{
			if (HairStrands == null)
				return;

			for (int i = 0; i < HairStrands.Count; i++)
				GameObject.DestroyImmediate(HairStrands[i].gameObject);

			HairStrands.Clear();
		}

		public void DrawHairStrand()
		{
			Drawing = !Drawing;
		}

		public void CreateStrand(Vector3[] points, float[] thickness, float[] stiffness, float[] retention, float length, Color[] colors)
		{
			GameObject hairStrand = new GameObject("Hair Strand " + HairStrands.Count);

			Transform hairStrandsContainer = _HairSimulation.transform.Find("Hair Strands");
			if (hairStrandsContainer == null)
			{
				GameObject strandsContainer = new GameObject("Hair Strands");
				strandsContainer.transform.SetParent(_HairSimulation.transform);
				hairStrandsContainer = strandsContainer.transform;
			}

			hairStrand.transform.transform.position = points[0];
			hairStrand.transform.SetParent(hairStrandsContainer);

			HairStrand strandComponent = hairStrand.AddComponent<HairStrand>();
			strandComponent.Thickness = thickness;
			strandComponent.Create(length, points, _HairSimulation, colors, thickness, stiffness, retention);
			HairStrands.Add(strandComponent);

			ComputeGeometryBuffer();
		}

		public void ReleaseBuffers()
		{
			if (HairPointBuffer != null)
			{
				HairPointBuffer.Release();
				HairPointBuffer = null;
			}

			if (HairStrandBuffer != null)
			{
				HairStrandBuffer.Release();
				HairStrandBuffer = null;
			}
		}
	}
}
