using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

#if (UNITY_EDITOR)
using UnityEditor.AnimatedValues;
#endif

namespace Avol
{
	public enum BrushType
	{
		Move,
		Property
	}

	public enum MoveBrushType
	{
		Drag,
		Twist,
		PushPull,
		Curl
	}

	public enum PropertyBrushType
	{
		Color,
		Length,
		LengthVariation,
		Thickness,
		Stiffness,
		Retention
	}

	#if (UNITY_EDITOR)

		[CustomEditor(typeof(HairSimulation))]
		public class HairSimulationEditor : Editor
		{
			private GUIStyle _HeaderStyle;
			private GUIStyle _DrawButtonStyle;
			private GUIStyle _DrawButtonStyleToggled;

			private Texture2D _BrushTexture;
			private Texture2D _BrushPressedTexture;


			private Tool _LastTool;

			private bool _MouseButtonDown = false;

			private int			_Tab		= 0;

			private AnimBool	_GeometryBaseOutAnim			= new AnimBool(true);
			private AnimBool	_GeometryBrushOutAnim			= new AnimBool(true);

			private AnimBool	_ShadingFoldOutAnim				= new AnimBool(true);
			private AnimBool	_ShadowsFoldOutAnim				= new AnimBool(true);

			private AnimBool	_CoreSolverOutAnim				= new AnimBool(true);
			private AnimBool	_WindFoldOutAnim				= new AnimBool(true);
			private AnimBool	_SelfCollisionFoldOutAnim		= new AnimBool(true);

			private GUIStyle	_ContainerStyle					= null;
			private GUIStyle	_ContainerStyle2				= null;
			private GUIStyle	_ContainerStyle3				= null;

			private GUIStyle	_TabStyle						= null;
			private GUIStyle	_TabStyle2						= null;
			private GUIStyle	_TabStyle3						= null;
			private GUIStyle	_TabStyle4						= null;

			private GUIStyle	_LabelStyle						= null;
			private GUIStyle	_TogglePreviewStyle				= null;


			public void OnEnable()
			{
				_BrushTexture			= Resources.Load<Texture2D>("Avol/Hair/Images/Brush");
				_BrushPressedTexture	= Resources.Load<Texture2D>("Avol/Hair/Images/Brush_Pressed");

				//HairSimulation hairSimulation = (HairSimulation)target;
				//SetIcon(hairSimulation.gameObject, 0);
			}

			public override void OnInspectorGUI()
			{
				_ContainerStyle				= new GUIStyle("HelpBox");
				_ContainerStyle.padding		= new RectOffset(10, 10, 10, 10);
				_ContainerStyle.margin		= new RectOffset(0, 0, 0, 0);

				_ContainerStyle2			= new GUIStyle("HelpBox");
				_ContainerStyle2.padding	= new RectOffset(10, 10, 10, 10);
				_ContainerStyle2.margin		= new RectOffset(0, 0, 0, 0);

				_ContainerStyle3			= new GUIStyle("HelpBox");
				_ContainerStyle3.padding	= new RectOffset(10, 10, 0, 0);
				_ContainerStyle3.margin		= new RectOffset(0, 0, 10, 10);

				_TabStyle					= new GUIStyle(GUI.skin.button);
				_TabStyle.margin			= new RectOffset(0, 0, 7, 11);
				_TabStyle.fixedHeight		= 30;

				_TabStyle3					= new GUIStyle("button");
				_TabStyle3.margin			= new RectOffset(0, 0, 5, 5);
				_TabStyle3.fixedHeight		= 30;
				_TabStyle3.fontSize			= 10;

				_TabStyle3.onHover.textColor	= Color.white;
				_TabStyle3.onNormal.textColor	= Color.white;

				_TabStyle4						= new GUIStyle("button");
				_TabStyle4.margin				= new RectOffset(0, 0, 5, 5);
				_TabStyle4.fixedHeight			= 30;
				_TabStyle4.fontSize				= 10;

				_TabStyle2						= new GUIStyle(GUI.skin.button);
				_TabStyle2.margin				= new RectOffset(0, 0, 7, 0);
				_TabStyle2.fixedHeight			= 30;
				_TabStyle2.normal.textColor		= Color.black;
				_TabStyle2.onNormal.textColor	= Color.white;
				_TabStyle2.hover.textColor		= new Color(0.2f, 0.2f, 0.2f, 1.0f); 
				_TabStyle2.onHover.textColor	= new Color(0.8f, 0.8f, 0.8f, 1.0f);



				_LabelStyle					= new GUIStyle(EditorStyles.boldLabel);
				_LabelStyle.alignment		= TextAnchor.MiddleLeft;
				_LabelStyle.fixedHeight     = 40;
				_LabelStyle.stretchHeight   = true;

				_TogglePreviewStyle					= new GUIStyle(EditorStyles.toggle);
				_TogglePreviewStyle.stretchHeight	= true;
				_TogglePreviewStyle.fixedHeight		= 40;
				_TogglePreviewStyle.fontStyle		= FontStyle.Bold;


				HairSimulation hairSimulation = (HairSimulation)target;

			
				if (hairSimulation.HairBuilder == null)
				{
					GUILayout.BeginVertical(EditorStyles.helpBox);
						EditorGUILayout.LabelField("Either HairRenderer.cs is missing on your camera or this gameobject is disabled.");
					GUILayout.EndVertical();
					return;
				}


				_Tab = GUILayout.Toolbar(_Tab, new string[] { "▲ Editor", "✎ Graphics", "≈ Physics", "ⓘ Info" }, _TabStyle);


				// ------------------------------------------ GEOMETRY ----------------------------------------------- //
				if (_Tab == 0)
				{
 
					GUILayout.BeginVertical(_ContainerStyle);

					_GeometryBaseOutAnim.target = GUILayout.Toggle(_GeometryBaseOutAnim.target, "❖ Base Geometry", _TabStyle2);
					if (EditorGUILayout.BeginFadeGroup(_GeometryBaseOutAnim.faded))
					{
						GUILayout.BeginVertical(_ContainerStyle2);

						hairSimulation.BasePoints				= Mathf.Max(3, EditorGUILayout.IntField("Strand Points ", hairSimulation.BasePoints));
						hairSimulation.BaseDensity				= Mathf.Max(1, EditorGUILayout.IntField("Triangle Density", hairSimulation.BaseDensity));

						hairSimulation.BaseRootColor			= EditorGUILayout.ColorField("Strand Root Color", hairSimulation.BaseRootColor);
						hairSimulation.BaseTipColor				= EditorGUILayout.ColorField("Strand Tip Color", hairSimulation.BaseTipColor);

						hairSimulation.BaseLength				= Mathf.Max(0, EditorGUILayout.FloatField("Strand Length", hairSimulation.BaseLength));
						hairSimulation.BaseLengthVariation		= EditorGUILayout.Slider("Strand Length Variation", hairSimulation.BaseLengthVariation, 0.0f, 1.0f);
						hairSimulation.BaseRootThickness		= Mathf.Max(0, EditorGUILayout.FloatField("Strand Root Thickness", hairSimulation.BaseRootThickness));
						hairSimulation.BaseTipThickness			= Mathf.Max(0, EditorGUILayout.FloatField("Strand Tip Thickness", hairSimulation.BaseTipThickness));
					
						hairSimulation.BaseStiffness			= EditorGUILayout.FloatField(new GUIContent("Strand Stiffness", "Straightens hair."), hairSimulation.BaseStiffness);
						hairSimulation.BaseRetention			= EditorGUILayout.FloatField(new GUIContent("Strand Retention", "Preserves hair shape."), hairSimulation.BaseRetention);
						hairSimulation.BaseBlendAngle			= EditorGUILayout.IntSlider(new GUIContent("Blend Angle Limit", "Will only blend guide hair directions if the angle between them is less or equal to the specified angle."), hairSimulation.BaseBlendAngle, 1, 180);
						

						EditorGUILayout.BeginHorizontal();
							if (GUILayout.Button(new GUIContent("Reset", "Resets hair to specified base settings."), _TabStyle))
								hairSimulation.HairBuilder.Reset();
							GUILayout.FlexibleSpace();
						EditorGUILayout.EndHorizontal();

						GUILayout.EndVertical();
					}
					EditorGUILayout.EndFadeGroup();


					_GeometryBrushOutAnim.target = GUILayout.Toggle(_GeometryBrushOutAnim.target, "✂ Brush Tools", _TabStyle2);
					if (EditorGUILayout.BeginFadeGroup(_GeometryBrushOutAnim.faded))
					{
						GUILayout.BeginVertical(_ContainerStyle2);

							hairSimulation.BrushType = (BrushType)GUILayout.Toolbar((int)hairSimulation.BrushType, new string[] { "✢ Shape", "✎ Geometry" }, _TabStyle);

							// toolbars
							if (hairSimulation.BrushType == BrushType.Move)
							{
								GUILayout.BeginHorizontal("box");

									hairSimulation.MoveBrushType = GUILayout.Toggle(hairSimulation.MoveBrushType == MoveBrushType.Drag, "Drag", _TabStyle3) ? MoveBrushType.Drag : hairSimulation.MoveBrushType;

									EditorGUI.BeginDisabledGroup(true);
										hairSimulation.MoveBrushType = GUILayout.Toggle(hairSimulation.MoveBrushType == MoveBrushType.Twist, "Twist", _TabStyle3) ? MoveBrushType.Twist : hairSimulation.MoveBrushType;
										hairSimulation.MoveBrushType = GUILayout.Toggle(hairSimulation.MoveBrushType == MoveBrushType.PushPull, "Push/Pull", _TabStyle3) ? MoveBrushType.PushPull : hairSimulation.MoveBrushType;
										hairSimulation.MoveBrushType = GUILayout.Toggle(hairSimulation.MoveBrushType == MoveBrushType.Curl, "Curl", _TabStyle3) ? MoveBrushType.Curl : hairSimulation.MoveBrushType;
									EditorGUI.EndDisabledGroup();

								GUILayout.EndHorizontal();
							}
							else if (hairSimulation.BrushType == BrushType.Property)
							{
								GUILayout.BeginHorizontal("box");

									hairSimulation.PropertyBrushType = GUILayout.Toggle(hairSimulation.PropertyBrushType == PropertyBrushType.Color, "Color", _TabStyle3) ? PropertyBrushType.Color : hairSimulation.PropertyBrushType;
									hairSimulation.PropertyBrushType = GUILayout.Toggle(hairSimulation.PropertyBrushType == PropertyBrushType.Length, "Length", _TabStyle3) ? PropertyBrushType.Length : hairSimulation.PropertyBrushType;
									hairSimulation.PropertyBrushType = GUILayout.Toggle(hairSimulation.PropertyBrushType == PropertyBrushType.LengthVariation, "Length Variation", _TabStyle3) ? PropertyBrushType.LengthVariation : hairSimulation.PropertyBrushType;
									hairSimulation.PropertyBrushType = GUILayout.Toggle(hairSimulation.PropertyBrushType == PropertyBrushType.Thickness, "Thickness", _TabStyle3) ? PropertyBrushType.Thickness : hairSimulation.PropertyBrushType;
									hairSimulation.PropertyBrushType = GUILayout.Toggle(hairSimulation.PropertyBrushType == PropertyBrushType.Stiffness, "Stiffness", _TabStyle3) ? PropertyBrushType.Stiffness : hairSimulation.PropertyBrushType;
									hairSimulation.PropertyBrushType = GUILayout.Toggle(hairSimulation.PropertyBrushType == PropertyBrushType.Retention, "Retention", _TabStyle3) ? PropertyBrushType.Retention : hairSimulation.PropertyBrushType;

								GUILayout.EndHorizontal();
							}


							GUILayout.Space(10);

							GUILayout.BeginVertical(_ContainerStyle);

								hairSimulation.BrushFrontInfluence	= EditorGUILayout.Slider(new GUIContent("Brush Front Influence", "How much hair will be selected from front to middle of all hair within brush range."), hairSimulation.BrushFrontInfluence, 0.0f, 1.0f);
								hairSimulation.BrushBackInfluence	= EditorGUILayout.Slider(new GUIContent("Brush Back Influence", "How much hair will be selected from back to middle of all hair within brush range."), hairSimulation.BrushBackInfluence, 0.0f, 1.0f);
								hairSimulation.BrushOpacity			= EditorGUILayout.Slider(new GUIContent("Brush Opacity", "Controls the opacity of brush."), hairSimulation.BrushOpacity, 0.0f, 1.0f);
								hairSimulation.BrushSize			= EditorGUILayout.Slider(new GUIContent("Brush Size", "Controls the size of brush."), hairSimulation.BrushSize, 0.0f, 1.0f);
								EditorGUILayout.MinMaxSlider(new GUIContent("Point Range ("+ (int)hairSimulation.MinPoint + "-" + (int)hairSimulation.MaxPoint + ")", "Influences hair points only in this range."), ref hairSimulation.MinPoint, ref hairSimulation.MaxPoint, 0, hairSimulation.BasePoints);
								hairSimulation.MinPoint = Mathf.RoundToInt(hairSimulation.MinPoint);
								hairSimulation.MaxPoint = Mathf.RoundToInt(hairSimulation.MaxPoint);

							GUILayout.EndVertical();

							GUILayout.FlexibleSpace();

							GUILayout.BeginVertical(_ContainerStyle);

								if (hairSimulation.BrushType == BrushType.Move)
								{
									switch (hairSimulation.MoveBrushType)
									{
										case MoveBrushType.Drag:
											hairSimulation.BrushDragStiffness = EditorGUILayout.Slider(new GUIContent("Strand Stiffness", "Uses custom stiffness to model hair while dragging. This does not affects stiffness outside of this brushing tool."), hairSimulation.BrushDragStiffness, 0.0f, 1.0f);
											break;
									}
								}

								else if (hairSimulation.BrushType == BrushType.Property)
								{
									switch (hairSimulation.PropertyBrushType)
									{	
										case PropertyBrushType.Thickness:
										case PropertyBrushType.Stiffness:
										case PropertyBrushType.Retention:
											hairSimulation.BrushSelectionMode = (BrushSelectionMode)EditorGUILayout.EnumPopup("Brush Selection Mode", hairSimulation.BrushSelectionMode);
										break;
									}

									switch (hairSimulation.PropertyBrushType)
									{
										case PropertyBrushType.Color:
											if (hairSimulation.BrushSelectionMode == BrushSelectionMode.Point)
											{
												hairSimulation.BrushColor = EditorGUILayout.ColorField(new GUIContent("Brush Color", "Colors hair points"), hairSimulation.BrushColor);
											}
											else
											{
												hairSimulation.BrushRootColor = EditorGUILayout.ColorField(new GUIContent("Brush Root Color", "Colors hair strand root"), hairSimulation.BrushRootColor);
												hairSimulation.BrushTipColor = EditorGUILayout.ColorField(new GUIContent("Brush Tip Color", "Colors hair strand tip"), hairSimulation.BrushTipColor);
											}
											break;
										case PropertyBrushType.Length:
											hairSimulation.BrushLength				= Mathf.Max(0, EditorGUILayout.FloatField("Strand Length ", hairSimulation.BrushLength));
											break;
										case PropertyBrushType.LengthVariation:
											hairSimulation.BrushLengthVariation		= EditorGUILayout.Slider("Strand Length Variation ", hairSimulation.BrushLengthVariation, 0.0f, 1.0f);
											break;
										case PropertyBrushType.Thickness:
											hairSimulation.BrushThickness			= Mathf.Max(0, EditorGUILayout.FloatField("Thickness", hairSimulation.BrushThickness));
											break;
										case PropertyBrushType.Stiffness:
											hairSimulation.BrushStiffness			= EditorGUILayout.Slider("Stiffness", hairSimulation.BrushStiffness, 0.0f, 1.0f);
											break;
										case PropertyBrushType.Retention:
											hairSimulation.BrushRetention			= EditorGUILayout.Slider("Retention", hairSimulation.BrushRetention, 0.0f, 1.0f);
											break;
									}
								}

							GUILayout.EndVertical();
						GUILayout.EndVertical();
					}
					EditorGUILayout.EndFadeGroup();


					GUILayout.BeginVertical(_ContainerStyle3);

						EditorGUILayout.BeginHorizontal();

							hairSimulation.PreviewHairPositions = GUILayout.Toggle(hairSimulation.PreviewHairPositions, new GUIContent("Preview", "Preview generated hair strands. Helps determine the resulting blend of hair properties."), _TogglePreviewStyle);

							GUILayout.Label("Strands: " + hairSimulation.HairGeometry.HairStrands.Count, _LabelStyle);

							if (!hairSimulation.RequiresRebuild || hairSimulation.HairBuilder.BuildProgress != -1)
								EditorGUI.BeginDisabledGroup(true);

								if (!hairSimulation.RequiresRebuild && hairSimulation.HairBuilder.BuildProgress == -1)
									if (GUILayout.Button("Rebuild (Done)", _TabStyle))
										hairSimulation.HairBuilder.Rebuild();

								if (hairSimulation.RequiresRebuild && hairSimulation.HairBuilder.BuildProgress == -1)
									if (GUILayout.Button("Rebuild", _TabStyle))
										hairSimulation.HairBuilder.Rebuild();

								if (hairSimulation.HairBuilder.BuildProgress != -1)
									if (GUILayout.Button("Building: " + hairSimulation.HairBuilder.BuildProgress +"%", _TabStyle))
										hairSimulation.HairBuilder.Rebuild();


								/*if (GUILayout.Button("⚔ Rebuild " + (!hairSimulation.RequiresRebuild ? "(Done)" : "("+hairSimulation.HairBuilder.BuildProgress+"%)"), _TabStyle))
								{
									hairSimulation.HairBuilder.Rebuild();
								}*/

							if (!hairSimulation.RequiresRebuild || hairSimulation.HairBuilder.BuildProgress != -1)
								EditorGUI.EndDisabledGroup();

					EditorGUILayout.EndHorizontal();

					GUILayout.EndVertical();


					GUILayout.EndVertical();
				}

				// ------------------------------------------ GRAPHICS ----------------------------------------------- //
				else if (_Tab == 1)
				{
					GUILayout.BeginVertical(_ContainerStyle);


						_ShadingFoldOutAnim.target = GUILayout.Toggle(_ShadingFoldOutAnim.target, "Shading", _TabStyle2);
						if (EditorGUILayout.BeginFadeGroup(_ShadingFoldOutAnim.faded))
						{
							GUILayout.BeginVertical(_ContainerStyle2);

								hairSimulation.ScatterTint				= EditorGUILayout.ColorField("Scatter Tint ", hairSimulation.ScatterTint);
								hairSimulation.Roughness				= EditorGUILayout.Slider("Roughness", hairSimulation.Roughness, 0.0f, 1.0f);
								hairSimulation.Shift					= EditorGUILayout.Slider("Shift", hairSimulation.Shift, 0.0f, 1.0f);
								hairSimulation.SelfShadowingOpacity		= EditorGUILayout.Slider("Self Shadow Opacity", hairSimulation.SelfShadowingOpacity, 0.0f, 1.0f);

							GUILayout.EndVertical();
						}

						EditorGUILayout.EndFadeGroup();



					GUILayout.EndVertical();
				}

				// ------------------------------------------ PHYSICS ----------------------------------------------- //
				else if (_Tab == 2)
				{
					GUILayout.BeginVertical(_ContainerStyle);

						_CoreSolverOutAnim.target = GUILayout.Toggle(_CoreSolverOutAnim.target, "Solver", _TabStyle2);
						if (EditorGUILayout.BeginFadeGroup(_CoreSolverOutAnim.faded))
						{
							GUILayout.BeginVertical(_ContainerStyle2);

							hairSimulation.Gravity		= EditorGUILayout.Vector3Field("Gravity", hairSimulation.Gravity);
							hairSimulation.Damping		= EditorGUILayout.Slider("Damping", hairSimulation.Damping, 0.0f, 1.0f);

							hairSimulation.Stiffness	= EditorGUILayout.Slider(new GUIContent("Stiffness Multiplier", "Straightens hair. Multiplies hair geometry stiffness by this value."), hairSimulation.Stiffness, 0.0f, 1.0f);
							hairSimulation.Retention	= EditorGUILayout.Slider(new GUIContent("Retention Multiplier", "Preserves hair shape. Hair geometry retention value is multiplied by this coefficient."), hairSimulation.Retention, 0.0f, 1.0f);
						
							GUILayout.EndVertical();
						}

						EditorGUILayout.EndFadeGroup();



						_WindFoldOutAnim.target = GUILayout.Toggle(_WindFoldOutAnim.target, "Wind", _TabStyle2);
						if (EditorGUILayout.BeginFadeGroup(_WindFoldOutAnim.faded))
						{
							GUILayout.BeginVertical(_ContainerStyle2);

							hairSimulation.Wind			= EditorGUILayout.Toggle("Wind", hairSimulation.Wind);
							if (hairSimulation.Wind)
							{
								GUILayout.BeginVertical(_ContainerStyle2);
									hairSimulation.WindTurbulance		= EditorGUILayout.Slider("Wind Turbulance", hairSimulation.WindTurbulance, 0.0f, 1.0f);
									hairSimulation.WindDirection		= EditorGUILayout.Vector3Field("Wind Direction", hairSimulation.WindDirection);
									hairSimulation.WindFrequency			= EditorGUILayout.Vector3Field("Wind Frequency", hairSimulation.WindFrequency);
								GUILayout.EndVertical();

								GUILayout.BeginVertical(_ContainerStyle2);
									hairSimulation.WindTurbulance2		= EditorGUILayout.Slider("Wind Turbulance 2", hairSimulation.WindTurbulance2, 0.0f, 1.0f);
									hairSimulation.WindDirection2		= EditorGUILayout.Vector3Field("Wind Direction 2", hairSimulation.WindDirection2);
									hairSimulation.WindFrequency2		= EditorGUILayout.Vector3Field("Wind Frequency 2", hairSimulation.WindFrequency2);
								GUILayout.EndVertical();
							}

							GUILayout.EndVertical();
						}

						EditorGUILayout.EndFadeGroup();


			
						_SelfCollisionFoldOutAnim.target = GUILayout.Toggle(_SelfCollisionFoldOutAnim.target, "Collision", _TabStyle2);
						if (EditorGUILayout.BeginFadeGroup(_SelfCollisionFoldOutAnim.faded))
						{
							GUILayout.BeginVertical(_ContainerStyle2);

								serializedObject.Update();
								EditorGUILayout.PropertyField(serializedObject.FindProperty("SphereColliders"), true);
								serializedObject.ApplyModifiedProperties();

								hairSimulation.SelfCollision			= EditorGUILayout.Toggle("Self Collision", hairSimulation.SelfCollision);

							GUILayout.EndVertical();
						}

						EditorGUILayout.EndFadeGroup();

					GUILayout.EndVertical();
				}

				else if (_Tab == 3)
				{
					GUILayout.BeginVertical(_ContainerStyle2);

						EditorGUILayout.LabelField("Contact E-mail: avolaso@gmail.com");

					GUILayout.EndVertical();
				}

				Repaint();
			}

			private void OnSceneGUI()
			{
				HairSimulation hairSimulation = (HairSimulation)target;

				EditorUtility.SetDirty(hairSimulation);

				bool prevMouseButtonDown = _MouseButtonDown;

				if (Event.current.type == EventType.MouseDown)
					if (Event.current.button == 0)
						_MouseButtonDown = true;

				if (Event.current.type == EventType.MouseUp)
					if (Event.current.button == 0)
						_MouseButtonDown = false;

				if (Event.current.type == EventType.MouseLeaveWindow)
					_MouseButtonDown = false;

				Handles.BeginGUI();

				if (_GeometryBrushOutAnim.faded == 1 && _Tab == 0)
				{
					if (Tools.current != Tool.None)
						_LastTool = Tools.current;

					Tools.current = Tool.None;


					float dpiScale = 20 / (1920 / Screen.dpi);
					float size = hairSimulation.BrushSize * Camera.current.pixelRect.height * 0.5f;
					

					GUI.DrawTexture(new Rect(Event.current.mousePosition.x - size * 0.5f, Event.current.mousePosition.y - size * 0.5f, size, size), _MouseButtonDown ? _BrushPressedTexture : _BrushTexture);

					HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

					if (_MouseButtonDown)		hairSimulation.HairBuilder.Brush(Event.current.mousePosition, size * 0.5f * dpiScale, prevMouseButtonDown != _MouseButtonDown);
					else						hairSimulation.HairBuilder.ReleaseBrush();
				}
				else
				{
					if (Tools.current == Tool.None)
						Tools.current = _LastTool;
				}

				Handles.EndGUI();
			}

			/*private static void SetIcon(GameObject gObj, Texture2D texture)
			{
				var ty = typeof(EditorGUIUtility);
				var mi = ty.GetMethod("SetIconForObject", BindingFlags.NonPublic | BindingFlags.Static);
				mi.Invoke(null, new object[] { gObj, texture });
			}*/
		}

	#endif
}
