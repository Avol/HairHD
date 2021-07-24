

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Avol
{
	[ExecuteInEditMode]
	public class HairBuilder
	{
		private		HairSimulation			_HairSimulation;

		private		Mesh					_ScalpMesh;
		private		float					_MaxExtents;
		private		Vector3[]				_RootPositions;
		private		Vector3[]				_RootNormals;

		private		Transform				_GuideHairContainer;
		private		GuideHair[]				_GuideHairs;
		private		GuideHair[]				_GuideHairDetails;


		private		bool					_CameraUpdated;
		private		bool					_BrushReleased			= true;
		private		Vector2					_PrevMousePosition;

		private		float					_BrushFront;
		private		float					_BrushBack;
		private		float					_BrushCenter;

		public		int						BuildProgress			= -1;
		public		int						LastBuiltTriangle		= -1;


		private		int						_LastBrushedFrame		= 30;


		public HairBuilder(HairSimulation hairSimulation)
		{
#if (UNITY_EDITOR)

			_HairSimulation = hairSimulation;

			if (!Application.isPlaying)
			{
				_ScalpMesh = _HairSimulation.GetComponent<MeshFilter>().sharedMesh;

				// calculate hair max extents
				float scaleX = _ScalpMesh.bounds.extents.x * _HairSimulation.transform.lossyScale.x;
				float scaleY = _ScalpMesh.bounds.extents.y * _HairSimulation.transform.lossyScale.y;
				float scaleZ = _ScalpMesh.bounds.extents.z * _HairSimulation.transform.lossyScale.z;
				_MaxExtents = Mathf.Max(scaleZ, Mathf.Max(scaleX, scaleY));

				// calculate guide hair world position based on scalp triangles.
				_CalcRootPositionsNormals();

				// init guide hair contaner.
				_GuideHairContainer = _HairSimulation.transform.Find("Guide Hairs");
				if (_GuideHairContainer == null)
				{
					GameObject guideHairs = new GameObject("Guide Hairs");
					guideHairs.transform.SetParent(hairSimulation.transform);
					guideHairs.transform.localPosition = Vector3.zero;
					guideHairs.transform.localRotation = Quaternion.identity;
					_GuideHairContainer = guideHairs.transform;
				}

				// get stored guide hairs
				_GetGuideHairs();

				#if (UNITY_EDITOR)
					EditorApplication.update -= _EditorUpdate;
					EditorApplication.update += _EditorUpdate;
				#endif
			}
#endif
		}



		private void _GetGuideHairs()
		{
#if (UNITY_EDITOR)
			_GuideHairs = new GuideHair[_GuideHairContainer.childCount];
			for (int i = 0; i < _GuideHairContainer.childCount; i++)
				_GuideHairs[i] = _GuideHairContainer.GetChild(i).GetComponent<GuideHair>();

			if (_GuideHairs.Length == 0)
				Reset();
#endif
		}

		private void _CreateBaseGuideHairs()
		{
#if (UNITY_EDITOR)
			// clear guide hair.
			for (int i = _GuideHairContainer.childCount-1; i >= 0; i--)
				GameObject.DestroyImmediate(_GuideHairContainer.GetChild(i).gameObject);

			// recalc root positions and normals.
			_CalcRootPositionsNormals();

			// create base guide hairs
			_GuideHairs = new GuideHair[_RootPositions.Length];
			for (int i = 0; i < _GuideHairs.Length; i++)
			{
				GameObject guideHair = new GameObject("Guide Hair " + i);
				guideHair.transform.SetParent(_GuideHairContainer);
				guideHair.transform.localRotation = Quaternion.identity;
				guideHair.transform.localPosition = Vector3.zero;

				_GuideHairs[i] = guideHair.AddComponent<GuideHair>();
				_GuideHairs[i].Create(_RootPositions[i], _RootNormals[i], _HairSimulation, SceneView.lastActiveSceneView.camera);
			}
#endif
		}

		private void _CalcRootPositionsNormals()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)
			{
				_RootPositions	= new Vector3[_ScalpMesh.vertices.Length];
				_RootNormals	= new Vector3[_ScalpMesh.normals.Length];

				for (int i = 0; i < _ScalpMesh.vertices.Length; i++)
				{
					_RootPositions[i]	= _HairSimulation.transform.localToWorldMatrix.MultiplyPoint3x4(_ScalpMesh.vertices[i]);
					_RootNormals[i]		= _HairSimulation.transform.localToWorldMatrix.MultiplyVector(_ScalpMesh.normals[i]).normalized;
				}
			}
#endif
		}


		public void DrawGizmos()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)
			{
				if (_GuideHairs != null)
				{
					for (int i = 0; i < _GuideHairs.Length; i++)
					{
						for (int c = 0; c < _GuideHairs[i].transform.childCount - 1; c++)
						{
							if (_HairSimulation.BrushType == BrushType.Move || _HairSimulation.PropertyBrushType == PropertyBrushType.Color ||
								_HairSimulation.PropertyBrushType == PropertyBrushType.Length || _HairSimulation.PropertyBrushType == PropertyBrushType.Thickness)
								Gizmos.color = _GuideHairs[i].Colors[c+1];

							else if (_HairSimulation.PropertyBrushType == PropertyBrushType.LengthVariation)
								Gizmos.color = Color.Lerp(Color.black, Color.white, _GuideHairs[i].LengthVariation);

							else if (_HairSimulation.PropertyBrushType == PropertyBrushType.Stiffness)
								Gizmos.color = Color.Lerp(Color.black, Color.white, _GuideHairs[i].Stiffness[c]);

							else if (_HairSimulation.PropertyBrushType == PropertyBrushType.Retention)
								Gizmos.color = Color.Lerp(Color.black, Color.white, _GuideHairs[i].Retention[c]);

							Gizmos.DrawLine(_GuideHairs[i].transform.GetChild(c).position, _GuideHairs[i].transform.GetChild(c+1).position);
						}
					}
				}


				if (_HairSimulation.PreviewHairPositions)
				{
					if (_HairSimulation.HairGeometry.HairStrands != null)
					{
						Gizmos.color = new Color(1, 1, 1, 0.2f);

						List<HairStrand> strands = _HairSimulation.HairGeometry.HairStrands;

						for (int i = 0; i < strands.Count; i++)
						{
							HairStrand strand = strands[i];
							if (strand.GetHandles() != null)
							{
								for (int c = 0; c < strand.GetHandles().Length - 1; c++)
								{
									Gizmos.color = strand.HairPointColors[c];
									Gizmos.DrawLine(strand.GetHandles()[c].transform.position, strand.GetHandles()[c + 1].transform.position);

									// draw tangent
									//Gizmos.color = Color.red;

									/*if (c > 0)
									{
										Vector3 normal		= (strand.GetHandles()[c].transform.position - strand.GetHandles()[c - 1].transform.position).normalized;
										Vector3 tangent		= (_HairSimulation.transform.localToWorldMatrix.MultiplyVector(strand.GetTangents()[c])).normalized;
										Vector3 bitangent	= (_HairSimulation.transform.localToWorldMatrix.MultiplyVector(strand.GetBiTangents()[c])).normalized;


										// draw tangent
										Gizmos.DrawLine(strand.GetHandles()[c].transform.position, strand.GetHandles()[c].transform.position + tangent * 0.1f);

										// draw bitangent
										Gizmos.color = Color.green;
										Gizmos.DrawLine(strand.GetHandles()[c].transform.position, strand.GetHandles()[c].transform.position + bitangent * 0.1f);

										// draw assumel normal
										Gizmos.color = Color.blue;
										Gizmos.DrawLine(strand.GetHandles()[c].transform.position, strand.GetHandles()[c].transform.position + normal * 0.05f);
									}*/
								}
							}
						}
					}
				}
			}
#endif
		}


		public void Update()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)
			{
				if (!Application.isPlaying)
				{
					if (Camera.current != null)
					{
						_CameraUpdated = false;
					}
					else
					{
						if (!_CameraUpdated)
						{
							for (int i = 0; i < _GuideHairs.Length; i++)
								_GuideHairs[i].CalculateScreenPoints(SceneView.lastActiveSceneView.camera);

							_CameraUpdated = true;
						}
					}

					if (_LastBrushedFrame < 60)
                    {
						_LastBrushedFrame++;

						for (int i = 0; i < _GuideHairs.Length; i++)
						{
							_GuideHairs[i].CollideHair(_HairSimulation.SphereColliders);

							if (_HairSimulation.MoveBrushType == MoveBrushType.Drag)
								_GuideHairs[i].StraightenHair(_HairSimulation.BrushDragStiffness * _HairSimulation.BrushOpacity);

							_GuideHairs[i].RestretchHair(0);

							_GuideHairs[i].CalculateScreenPoints(SceneView.lastActiveSceneView.camera);
						}
					}
				}
			}
#endif
		}

		private void _EditorUpdate()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)
			{
				if (BuildProgress != -1)
				{
					RebuildPart();

					if (BuildProgress == 100)
						BuildProgress = -1;
				}
			}
#endif
		}


		public void Reset()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)
			{
				_HairSimulation.BrushLength					= _HairSimulation.BaseLength;
				_HairSimulation.BrushLengthVariation		= _HairSimulation.BaseLengthVariation;
				_HairSimulation.BrushThickness				= _HairSimulation.BaseRootThickness;
				_HairSimulation.MinPoint					= 0;
				_HairSimulation.MaxPoint					= _HairSimulation.BasePoints;

				_CreateBaseGuideHairs();

				_HairSimulation.RequiresRebuild		= true;
			}
#endif
		}

		public void Rebuild()
		{
#if (UNITY_EDITOR)
			if (_HairSimulation.HairGeometry != null)
				_HairSimulation.HairGeometry.ClearHairStrands();


				LastBuiltTriangle	= -1;
				BuildProgress		= 0;
				RebuildPart();


			/*if (!Application.isPlaying)
			{
				if (_HairSimulation.HairGeometry != null)
				{
					float parts = (float)_ScalpMesh.triangles.Length / 100.0f;

					for (int i = 0; i < _ScalpMesh.triangles.Length; i += 3)
					{
						
							int i0 = _ScalpMesh.triangles[i];
							int i1 = _ScalpMesh.triangles[i + 1];
							int i2 = _ScalpMesh.triangles[i + 2];

							GuideHair guideHair0 = _GuideHairs[i0];
							GuideHair guideHair1 = _GuideHairs[i1];
							GuideHair guideHair2 = _GuideHairs[i2];

							// calculate bend angle limit normal coeffs
							Vector3 a0 = (guideHair0.Points[1] - guideHair0.Points[0]).normalized;
							Vector3 a1 = (guideHair1.Points[1] - guideHair1.Points[0]).normalized;
							Vector3 a2 = (guideHair2.Points[1] - guideHair2.Points[0]).normalized;

							float da0 = (Vector3.Dot(a0, a1) + 1.0f) * 0.5f;
							float da1 = (Vector3.Dot(a1, a2) + 1.0f) * 0.5f;
							float da2 = (Vector3.Dot(a2, a0) + 1.0f) * 0.5f;

							float angleNorm = _HairSimulation.BaseBlendAngle / 180.0f;

							da0 = Mathf.Min(1.0f, da0 + angleNorm);
							if (da0 < 1.0f)
								da0 = 0;

							da1 = Mathf.Min(1.0f, da1 + angleNorm);
							if (da1 < 1.0f)
								da1 = 0;

							da2 = Mathf.Min(1.0f, da2 + angleNorm);
							if (da2 < 1.0f)
								da2 = 0;

							for (int k = 0; k < _HairSimulation.BaseDensity; k++)
							{
								Vector3[] points = new Vector3[_HairSimulation.BasePoints];
								Color[] colors = new Color[_HairSimulation.BasePoints];

								float[] thickness = new float[_HairSimulation.BasePoints];
								float[] stiffness = new float[_HairSimulation.BasePoints];
								float[] retention = new float[_HairSimulation.BasePoints];

								float c0 = Random.Range(0.00001f, 1.0f);
								float c1 = Random.Range(0.00001f, 1.0f);
								float c2 = Random.Range(0.00001f, 1.0f);

								float normalizeFactor = Mathf.Max(float.MinValue, (c0 + c1 + c2));

								c0 /= normalizeFactor;
								c1 /= normalizeFactor;
								c2 /= normalizeFactor;

								float tc0 = c0;
								float tc1 = c1;
								float tc2 = c2;

								// get dominating direction
								if (c0 >= c1 && c0 >= c2)
								{
									tc1 *= Mathf.Min(1.0f, da0);
									tc2 *= Mathf.Min(1.0f, da2);
								}
								if (c1 >= c0 && c1 >= c2)
								{
									tc0 *= Mathf.Min(1.0f, da0);
									tc2 *= Mathf.Min(1.0f, da1);
								}
								else if (c2 >= c0 && c2 >= c1)
								{
									tc0 *= Mathf.Min(1.0f, da2);
									tc1 *= Mathf.Min(1.0f, da1);
								}

								float norm2 = Mathf.Max(0.0001f, (tc0 + tc1 + tc2));
								tc0 /= norm2;
								tc1 /= norm2;
								tc2 /= norm2;


								float randomLen0 = guideHair0.LengthVariation;
								float randomLen1 = guideHair1.LengthVariation;
								float randomLen2 = _GuideHairs[i2].LengthVariation;

								for (int c = 0; c < _GuideHairs[0].Points.Length; c++)
								{
									Vector3 v0 = guideHair0.Points[c];
									Vector3 v1 = guideHair1.Points[c];
									Vector3 v2 = guideHair2.Points[c];

									Color col0 = guideHair0.Colors[c];
									Color col1 = guideHair1.Colors[c];
									Color col2 = guideHair2.Colors[c];

									float thic0 = guideHair0.Thickness[c];
									float thic1 = guideHair1.Thickness[c];
									float thic2 = guideHair2.Thickness[c];

									float stiff0 = guideHair0.Stiffness[c];
									float stiff1 = guideHair1.Stiffness[c];
									float stiff2 = guideHair2.Stiffness[c];

									float ret0 = guideHair0.Retention[c];
									float ret1 = guideHair1.Retention[c];
									float ret2 = guideHair2.Retention[c];



									Vector3 pos = v0 * tc0 + v1 * tc1 + v2 * tc2;
									Color color = col0 * tc0 + col1 * tc1 + col2 * tc2;

									float thic = thic0 * tc0 + thic1 * tc1 + thic2 * tc2;
									float stiff = stiff0 * tc0 + stiff1 * tc1 + stiff2 * tc2;
									float ret = ret0 * tc0 + ret1 * tc1 + ret2 * tc2;

									if (c == 0)
									{
										pos = v0 * c0 + v1 * c1 + v2 * c2;
										color = col0 * c0 + col1 * c1 + col2 * c2;

										thic = thic0 * c0 + thic1 * c1 + thic2 * c2;
										stiff = stiff0 * c0 + stiff1 * c1 + stiff2 * c2;
										ret = ret0 * c0 + ret1 * c1 + ret2 * c2;
									}

									points[c] = pos;
									colors[c] = color;

									thickness[c] = thic;
									stiffness[c] = stiff;
									retention[c] = ret;
								}


								float normalLen = _RootNormals[0].magnitude;
								float maxLen = normalLen * _HairSimulation.BaseLength;
								float minLen = Mathf.Lerp(maxLen, 0, randomLen0 * c0 + randomLen1 * c1 + randomLen2 * c2);
								float randomLen = Random.Range(minLen, maxLen);

								_HairSimulation.HairGeometry.CreateStrand(points, thickness, stiffness, retention, randomLen, colors);
							}
						
					}

					_HairSimulation.RequiresRebuild = false;
				}
			}*/
#endif
		}

		public void RebuildPart()
		{
#if (UNITY_EDITOR)
			if (!Application.isPlaying)
			{
				if (_HairSimulation.HairGeometry != null)
				{
					float parts = (float)_ScalpMesh.triangles.Length / 100.0f;

					for (int i = 0; i < _ScalpMesh.triangles.Length; i += 3)
					{
						if (i >= BuildProgress * parts && i < (BuildProgress + 1) * parts && LastBuiltTriangle != i)
						{
							LastBuiltTriangle = i;

							int i0 = _ScalpMesh.triangles[i];
							int i1 = _ScalpMesh.triangles[i + 1];
							int i2 = _ScalpMesh.triangles[i + 2];

							GuideHair guideHair0 = _GuideHairs[i0];
							GuideHair guideHair1 = _GuideHairs[i1];
							GuideHair guideHair2 = _GuideHairs[i2];

							// calculate bend angle limit normal coeffs
							Vector3 a0 = (guideHair0.transform.GetChild(1).position - guideHair0.transform.GetChild(0).position).normalized;
							Vector3 a1 = (guideHair1.transform.GetChild(1).position - guideHair1.transform.GetChild(0).position).normalized;
							Vector3 a2 = (guideHair2.transform.GetChild(1).position - guideHair2.transform.GetChild(0).position).normalized;

							float da0 = (Vector3.Dot(a0, a1) + 1.0f) * 0.5f;
							float da1 = (Vector3.Dot(a1, a2) + 1.0f) * 0.5f;
							float da2 = (Vector3.Dot(a2, a0) + 1.0f) * 0.5f;

							float angleNorm = _HairSimulation.BaseBlendAngle / 180.0f;

							da0 = Mathf.Min(1.0f, da0 + angleNorm);
							if (da0 < 1.0f)
								da0 = 0;

							da1 = Mathf.Min(1.0f, da1 + angleNorm);
							if (da1 < 1.0f)
								da1 = 0;

							da2 = Mathf.Min(1.0f, da2 + angleNorm);
							if (da2 < 1.0f)
								da2 = 0;

							for (int k = 0; k < _HairSimulation.BaseDensity; k++)
							{
								Vector3[]	points	= new Vector3[_HairSimulation.BasePoints];
								Color[]		colors	= new Color[_HairSimulation.BasePoints];

								float[]		thickness	= new float[_HairSimulation.BasePoints];
								float[]		stiffness	= new float[_HairSimulation.BasePoints];
								float[]		retention	= new float[_HairSimulation.BasePoints];

								float c0 = Random.Range(0.00001f, 1.0f);
								float c1 = Random.Range(0.00001f, 1.0f);
								float c2 = Random.Range(0.00001f, 1.0f);

								float normalizeFactor = Mathf.Max(float.MinValue, (c0 + c1 + c2));

								c0 /= normalizeFactor;
								c1 /= normalizeFactor;
								c2 /= normalizeFactor;

								float tc0 = c0;
								float tc1 = c1;
								float tc2 = c2;

								// get dominating direction
								if (c0 >= c1 && c0 >= c2)
								{
									tc1 *= Mathf.Min(1.0f, da0);
									tc2 *= Mathf.Min(1.0f, da2);
								}
								if (c1 >= c0 && c1 >= c2)
								{
									tc0 *= Mathf.Min(1.0f, da0);
									tc2 *= Mathf.Min(1.0f, da1);
								}
								else if (c2 >= c0 && c2 >= c1)
								{
									tc0 *= Mathf.Min(1.0f, da2);
									tc1 *= Mathf.Min(1.0f, da1);
								}

								float norm2 = Mathf.Max(0.0001f, (tc0 + tc1 + tc2));
								tc0 /= norm2;
								tc1 /= norm2;
								tc2 /= norm2;


								float randomLen0 = guideHair0.LengthVariation;
								float randomLen1 = guideHair1.LengthVariation;
								float randomLen2 = _GuideHairs[i2].LengthVariation;

								for (int c = 0; c < _HairSimulation.BasePoints; c++)
								{
									Vector3 v0 = guideHair0.transform.GetChild(c).position;
									Vector3 v1 = guideHair1.transform.GetChild(c).position;
									Vector3 v2 = guideHair2.transform.GetChild(c).position;

									Color col0 = guideHair0.Colors[c];
									Color col1 = guideHair1.Colors[c];
									Color col2 = guideHair2.Colors[c];

									float thic0 = guideHair0.Thickness[c];
									float thic1 = guideHair1.Thickness[c];
									float thic2 = guideHair2.Thickness[c];

									float stiff0 = guideHair0.Stiffness[c];
									float stiff1 = guideHair1.Stiffness[c];
									float stiff2 = guideHair2.Stiffness[c];

									float ret0 = guideHair0.Retention[c];
									float ret1 = guideHair1.Retention[c];
									float ret2 = guideHair2.Retention[c];



									Vector3 pos = v0 * tc0 + v1 * tc1 + v2 * tc2;
									Color color = col0 * tc0 + col1 * tc1 + col2 * tc2;

									float thic = thic0 * tc0 + thic1 * tc1 + thic2 * tc2;
									float stiff = stiff0 * tc0 + stiff1 * tc1 + stiff2 * tc2;
									float ret = ret0 * tc0 + ret1 * tc1 + ret2 * tc2;

									if (c == 0)
									{
										pos = v0 * c0 + v1 * c1 + v2 * c2;
										color = col0 * c0 + col1 * c1 + col2 * c2;

										thic = thic0 * c0 + thic1 * c1 + thic2 * c2;
										stiff = stiff0 * c0 + stiff1 * c1 + stiff2 * c2;
										ret = ret0 * c0 + ret1 * c1 + ret2 * c2;
									}

									points[c] = pos;
									colors[c] = color;

									thickness[c] = thic;
									stiffness[c] = stiff;
									retention[c] = ret;
								}


								float normalLen = _RootNormals[0].magnitude;
								float maxLen = normalLen * _HairSimulation.BaseLength;
								float minLen = Mathf.Lerp(maxLen, 0, randomLen0 * c0 + randomLen1 * c1 + randomLen2 * c2);
								float randomLen = Random.Range(minLen, maxLen);

								_HairSimulation.HairGeometry.CreateStrand(points, thickness, stiffness, retention, randomLen, colors);
							}
						}
					}

					_HairSimulation.RequiresRebuild = false;
				}

				BuildProgress += 1;
			}
#endif
		}


		public void Brush(Vector2 mousePosition, float size, bool click)
		{
#if (UNITY_EDITOR)
			if (_HairSimulation.BrushType != BrushType.Move)
				_BrushReleased = false;

			float dpiScale = 20 / (1920 / Screen.dpi);
			Vector2 mousePositionFixed = mousePosition * dpiScale;
			mousePositionFixed.y = Camera.current.pixelHeight - mousePositionFixed.y;

	
			// calculate front and back influence.
			if (_BrushReleased)
			{
				int pointsFound = 0;
				for (int i = 0; i < _GuideHairs.Length; i++)
					for (int c = 1; c < _GuideHairs[i].ScreenPoints.Length; c++)
						if ((new Vector2(_GuideHairs[i].ScreenPoints[c].x, _GuideHairs[i].ScreenPoints[c].y) - mousePositionFixed).magnitude < size)
							pointsFound++;

				if (pointsFound > 1)
				{
					_BrushFront = SceneView.lastActiveSceneView.camera.farClipPlane;
					_BrushBack	= SceneView.lastActiveSceneView.camera.nearClipPlane;

					for (int i = 0; i < _GuideHairs.Length; i++)
					{
						for (int c = 1; c < _GuideHairs[i].ScreenPoints.Length; c++)
						{
							if ((new Vector2(_GuideHairs[i].ScreenPoints[c].x, _GuideHairs[i].ScreenPoints[c].y) - mousePositionFixed).magnitude < size)
							{
								if (_GuideHairs[i].ScreenPoints[c].z < _BrushFront)
									_BrushFront = _GuideHairs[i].ScreenPoints[c].z;

								if (_GuideHairs[i].ScreenPoints[c].z > _BrushBack)
									_BrushBack = _GuideHairs[i].ScreenPoints[c].z;
							}
						}
					}
				}
				else
				{
					_BrushFront		= SceneView.lastActiveSceneView.camera.nearClipPlane;
					_BrushBack		= SceneView.lastActiveSceneView.camera.farClipPlane;
				}

				_BrushCenter = (_BrushFront + _BrushBack) * 0.5f;
			}

			if (!_BrushReleased)
			{
				if (_HairSimulation.BrushType == BrushType.Move)
				{
					
					if ((_PrevMousePosition - mousePositionFixed).magnitude > 0)
						_MoveBrush(mousePositionFixed, size);
				}
				else if (_HairSimulation.BrushType == BrushType.Property)
				{
					if ((_PrevMousePosition - mousePositionFixed).magnitude > 0 || click)
						_PropertyBrush(mousePositionFixed, size);
				}
			}

			if (_HairSimulation.BrushType == BrushType.Move)
				_BrushReleased		= false;

			_PrevMousePosition	= mousePositionFixed;
#endif
		}

		public void ReleaseBrush()
		{
#if (UNITY_EDITOR)
			_BrushReleased = true;
#endif
		}


		private void _MoveBrush(Vector2 mousePositionFixed, float size)
		{
#if (UNITY_EDITOR)
			Vector2 move = mousePositionFixed - _PrevMousePosition;
			move *= _HairSimulation.BrushOpacity;

			for (int i = 0; i < _GuideHairs.Length; i++)
			{
				List<int> affectedPoints = new List<int>();

				// move hair points with the brush
				for (int c = 1; c < _GuideHairs[i].ScreenPoints.Length; c++)
				{
					if (c >= _HairSimulation.MinPoint && c <= _HairSimulation.MaxPoint)
					{
						if ((new Vector2(_GuideHairs[i].ScreenPoints[c].x, _GuideHairs[i].ScreenPoints[c].y) - mousePositionFixed).magnitude < size)
						{
							bool frontAllow = true;
							bool backAllow = true;

							if (_GuideHairs[i].ScreenPoints[c].z >= Mathf.Lerp(_BrushFront, _BrushCenter, _HairSimulation.BrushFrontInfluence))
								frontAllow = false;

							if (_GuideHairs[i].ScreenPoints[c].z <= Mathf.Lerp(_BrushBack, _BrushCenter, _HairSimulation.BrushBackInfluence))
								backAllow = false;

							if (frontAllow || backAllow)
							{
								if (_HairSimulation.MoveBrushType == MoveBrushType.Drag)
								{
									_GuideHairs[i].ScreenPoints[c] += new Vector3(move.x, move.y, 0);
									_GuideHairs[i].transform.GetChild(c).position = SceneView.lastActiveSceneView.camera.ScreenToWorldPoint(_GuideHairs[i].ScreenPoints[c]);
								}
								else if (_HairSimulation.MoveBrushType == MoveBrushType.Twist)
								{
									_GuideHairs[i].Twist(mousePositionFixed, size, 0.5f);
								}

								affectedPoints.Add(c);
							}
						}
					}
				}

				// reposition hair to meet spring constraints.
				if (affectedPoints.Count > 0)
				{
					_GuideHairs[i].CollideHair(_HairSimulation.SphereColliders);

					if (_HairSimulation.MoveBrushType == MoveBrushType.Drag)
						_GuideHairs[i].StraightenHair(_HairSimulation.BrushDragStiffness * _HairSimulation.BrushOpacity);

					_GuideHairs[i].RestretchHair(affectedPoints[Mathf.FloorToInt(affectedPoints.Count / 2)]);

					_GuideHairs[i].CalculateScreenPoints(SceneView.lastActiveSceneView.camera);
					_HairSimulation.RequiresRebuild = true;
					_LastBrushedFrame = 0;
				}
			}
#endif
		}

		private void _PropertyBrush(Vector2 mousePositionFixed, float size)
		{
#if (UNITY_EDITOR)
			for (int i = 0; i < _GuideHairs.Length; i++)
			{
				GuideHair guideHair = _GuideHairs[i];

				bool rebuild = false;

				for (int c = 0; c < guideHair.ScreenPoints.Length; c++)
				{
					if (c >= _HairSimulation.MinPoint && c <= _HairSimulation.MaxPoint)
					{
						if ((new Vector2(guideHair.ScreenPoints[c].x, guideHair.ScreenPoints[c].y) - mousePositionFixed).magnitude < size)
						{
							bool frontAllow = true;
							bool backAllow = true;

							if (_GuideHairs[i].ScreenPoints[c].z >= Mathf.Lerp(_BrushFront, _BrushCenter, _HairSimulation.BrushFrontInfluence))
								frontAllow = false;

							if (_GuideHairs[i].ScreenPoints[c].z <= Mathf.Lerp(_BrushBack, _BrushCenter, _HairSimulation.BrushBackInfluence))
								backAllow = false;

							if (frontAllow || backAllow)
							{
								// COLOR
								if (_HairSimulation.PropertyBrushType == PropertyBrushType.Color)
								{
									if (_HairSimulation.BrushSelectionMode == BrushSelectionMode.Strand)
									{
										for (int k = 0; k < guideHair.ScreenPoints.Length; k++)
											guideHair.Colors[k] = Color.Lerp(guideHair.Colors[k], Color.Lerp(_HairSimulation.BrushRootColor, _HairSimulation.BrushTipColor, (float)k / (float)(guideHair.ScreenPoints.Length - 1)), _HairSimulation.BrushOpacity);
									}
									else
									{
										guideHair.Colors[c] = Color.Lerp(guideHair.Colors[c], _HairSimulation.BrushColor, _HairSimulation.BrushOpacity);
									}

									rebuild = true;

									if (_HairSimulation.BrushSelectionMode == BrushSelectionMode.Strand)
										break;
								}


								// LENGTH
								if (_HairSimulation.PropertyBrushType == PropertyBrushType.Length)
									guideHair.ChangeLength(Mathf.Lerp(guideHair.Length, _HairSimulation.BrushLength, _HairSimulation.BrushOpacity));

								// LENGTH VARIATION
								else if (_HairSimulation.PropertyBrushType == PropertyBrushType.LengthVariation)
									guideHair.LengthVariation = Mathf.Lerp(guideHair.LengthVariation, _HairSimulation.BrushLengthVariation, _HairSimulation.BrushOpacity);


								// THICKNESS
								else if (_HairSimulation.PropertyBrushType == PropertyBrushType.Thickness)
								{
									if (_HairSimulation.BrushSelectionMode == BrushSelectionMode.Point)
										guideHair.Thickness[c] = Mathf.Lerp(guideHair.Thickness[c], _HairSimulation.BrushThickness, _HairSimulation.BrushOpacity);

									else if (_HairSimulation.BrushSelectionMode == BrushSelectionMode.Strand)
									{
										for (int k = 0; k < guideHair.transform.childCount; k++)
											guideHair.Thickness[k] = Mathf.Lerp(guideHair.Thickness[k], _HairSimulation.BrushThickness, _HairSimulation.BrushOpacity);

										break;
									}
								}

								// STIFFNESS
								else if (_HairSimulation.PropertyBrushType == PropertyBrushType.Stiffness)
								{
									if (_HairSimulation.BrushSelectionMode == BrushSelectionMode.Point)
										guideHair.Stiffness[c] = Mathf.Lerp(guideHair.Stiffness[c], _HairSimulation.BrushStiffness, _HairSimulation.BrushOpacity);

									else if (_HairSimulation.BrushSelectionMode == BrushSelectionMode.Strand)
									{
										for (int k = 0; k < guideHair.transform.childCount; k++)
											guideHair.Stiffness[k] = Mathf.Lerp(guideHair.Stiffness[k], _HairSimulation.BrushStiffness, _HairSimulation.BrushOpacity);

										break;
									}
								}

								// RETENTION
								else if (_HairSimulation.PropertyBrushType == PropertyBrushType.Retention)
								{
									if (_HairSimulation.BrushSelectionMode == BrushSelectionMode.Point)
										guideHair.Retention[c] = Mathf.Lerp(guideHair.Retention[c], _HairSimulation.BrushRetention, _HairSimulation.BrushOpacity);

									else if (_HairSimulation.BrushSelectionMode == BrushSelectionMode.Strand)
									{
										for (int k = 0; k < guideHair.transform.childCount; k++)
											guideHair.Retention[k] = Mathf.Lerp(guideHair.Retention[k], _HairSimulation.BrushRetention, _HairSimulation.BrushOpacity);

										break;
									}
								}

								rebuild = true;
							}
						}
					}
				}

				if (rebuild)
					_HairSimulation.RequiresRebuild = true;
			}
#endif
		}
	}
}