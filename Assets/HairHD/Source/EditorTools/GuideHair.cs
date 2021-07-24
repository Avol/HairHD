using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Avol
{
	public class GuideHair : MonoBehaviour
	{
		public float Length					{ get;  private set; }
		public float LengthVariation;

		public Vector3[]	ScreenPoints;
		public Color[]		Colors;

		public float[]		Thickness;
		public float[]		Stiffness;
		public float[]		Retention;


		public void Create(Vector3 rootPosition, Vector3 rootNormal, HairSimulation hairSimulation, Camera camera)
		{
			Length		= hairSimulation.BaseLength;

			Colors		= new Color[hairSimulation.BasePoints];

			Thickness	= new float[hairSimulation.BasePoints];
			Stiffness	= new float[hairSimulation.BasePoints];
			Retention	= new float[hairSimulation.BasePoints];


			for (int i = transform.childCount-1; i >= 0; i--)
				Destroy(transform.GetChild(i));

			for (int i = 0; i < hairSimulation.BasePoints; i++)
			{
				GameObject pointObject = new GameObject("GuideHairPoint " + i);
				pointObject.transform.SetParent(transform);
				pointObject.transform.position = rootPosition + rootNormal * ((float)i / (float)(hairSimulation.BasePoints - 1)) * Length;

				Colors[i]		= Color.Lerp(hairSimulation.BaseRootColor, hairSimulation.BaseTipColor, (float)i / (float)(hairSimulation.BasePoints - 1));
				Thickness[i]	= Mathf.Lerp(hairSimulation.BaseRootThickness, hairSimulation.BaseTipThickness, (float)i / (float)(hairSimulation.BasePoints - 1));
				Stiffness[i]	= hairSimulation.BaseStiffness;
				Retention[i]	= hairSimulation.BaseRetention;
			}

			CalculateScreenPoints(camera);
		}

		public void CalculateScreenPoints(Camera camera)
		{
			ScreenPoints = new Vector3[transform.childCount];
			for (int i = 0; i < transform.childCount; i++)
				ScreenPoints[i] = camera.WorldToScreenPoint(transform.GetChild(i).position);
		}

		public void CollideHair(SphereCollider[] sphereColliders)
		{
			for (int i = 0; i < sphereColliders.Length; i++)
			{
				Vector3 scaleAllAxes = sphereColliders[i].transform.lossyScale;
				float maxScale = Mathf.Max(Mathf.Max(scaleAllAxes.x, scaleAllAxes.y), scaleAllAxes.z);

				for (int c = 0; c < transform.childCount; c++)
				{
					Vector3 offset = Vector3.zero;

					float radius = sphereColliders[i].radius * maxScale + 0.01f;
					Vector3 dir = sphereColliders[i].transform.position - transform.GetChild(c).position;

					if (dir.magnitude < radius)
						offset = -dir.normalized * (radius - dir.magnitude);

					transform.GetChild(c).position += offset;
				}
			}
		}

		public void RestretchHair(int priorityPoint)
		{
			// stretch back
			for (int c = priorityPoint; c >= 2; c--)
			{
				Vector3 dir = (transform.GetChild(c-1).position - transform.GetChild(c).position).normalized;
				transform.GetChild(c - 1).position = transform.GetChild(c).position + dir * (Length / transform.childCount);
			}

			// restretch correctly
			for (int c = 1; c < transform.childCount; c++)
			{
				Vector3 dir = (transform.GetChild(c-1).position - transform.GetChild(c).position).normalized;
				transform.GetChild(c).position = transform.GetChild(c-1).position - dir * (Length / transform.childCount);
			}
		}

		public void StraightenHair(float stiffness)
		{
			for (int c = 2; c < transform.childCount; c++)
			{
				Vector3 prevPosition2	=  transform.GetChild(c-2).position;
				Vector3 prevPosition	=  transform.GetChild(c-1).position;

				Vector3 diff	= prevPosition2 - prevPosition;
				Vector3 diff2	= prevPosition - transform.GetChild(c).position;

				if (diff.magnitude != 0 && diff2.magnitude != 0)
				{
					Vector3 prevBend = prevPosition - diff.normalized * (Length / transform.childCount);
					float dotStiffness = 1.0f - Mathf.Max(0, Vector3.Dot(diff.normalized, diff2.normalized));
					transform.GetChild(c).position = Vector3.Lerp(transform.GetChild(c).position, prevBend, stiffness * Mathf.Lerp(dotStiffness, 1, stiffness));
				}
			}
		}

		public void Twist(Vector2 center, float radius, float distFromCenter)
		{
			int smallestAffectedPoint = transform.childCount;

			for (int i = 0; i < ScreenPoints.Length; i++)
			{
				if ((new Vector2(ScreenPoints[i].x, ScreenPoints[i].y) - center).magnitude < radius)
				{
					smallestAffectedPoint = i;
					break;
				}
			}

			Vector2 dir = new Vector2(ScreenPoints[smallestAffectedPoint].x, ScreenPoints[smallestAffectedPoint].y) - center;
			dir = dir.normalized;
		}

		public void ChangeLength(float length)
		{
			if (length < Length)
			{
				for (int i = transform.childCount - 1; i > 0; i--)
				{
					Vector3 segment = transform.GetChild(i).position - transform.GetChild(i-1).position;
					transform.GetChild(i).position = transform.GetChild(i-1).position + segment.normalized * length / transform.childCount;
				}
			}
			else
			{
				Vector3[] segmentOffsets = new Vector3[transform.childCount-1];
				for (int i = 1; i < transform.childCount; i++)
					segmentOffsets[i-1] = transform.GetChild(i).position - transform.GetChild(i-1).position;

				for (int i = 1; i < transform.childCount; i++)
					transform.GetChild(i).position = transform.GetChild(i-1).position + segmentOffsets[i-1].normalized * length / transform.childCount;
			}

			Length = length;
		}
	}
}
