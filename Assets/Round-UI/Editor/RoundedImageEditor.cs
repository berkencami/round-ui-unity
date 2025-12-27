using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RoundUI.Editor
{
    /// <summary>
    /// Custom editor for RoundedImage component providing specialized inspector UI.
    /// </summary>
    [CustomEditor(typeof(RoundedImage))]
    public class RoundedImageEditor : ImageEditor
    {
        private SerializedProperty _roundingMode;
        private SerializedProperty _roundingAmount;
        private SerializedProperty _borderThickness;
        private SerializedProperty _distanceFalloff;
        private SerializedProperty _useHitBoxOutside;
        private SerializedProperty _useHitBoxInside;

        // Base Image properties
        private SerializedProperty _imageType;
        private SerializedProperty _fillMethod;
        private SerializedProperty _fillOrigin;
        private SerializedProperty _fillAmount;
        private SerializedProperty _fillClockwise;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Cache RoundedImage properties
            _roundingMode = serializedObject.FindProperty("_roundingMode");
            _roundingAmount = serializedObject.FindProperty("_roundingAmount");
            _borderThickness = serializedObject.FindProperty("_borderThickness");
            _distanceFalloff = serializedObject.FindProperty("_distanceFalloff");
            _useHitBoxOutside = serializedObject.FindProperty("_useHitBoxOutside");
            _useHitBoxInside = serializedObject.FindProperty("_useHitBoxInside");

            // Cache base Image properties for Fill
            _imageType = serializedObject.FindProperty("m_Type");
            _fillMethod = serializedObject.FindProperty("m_FillMethod");
            _fillOrigin = serializedObject.FindProperty("m_FillOrigin");
            _fillAmount = serializedObject.FindProperty("m_FillAmount");
            _fillClockwise = serializedObject.FindProperty("m_FillClockwise");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw base Image properties manually
            DrawImageSettings();

            EditorGUILayout.Space();

            // Rounding Settings Section
            EditorGUILayout.LabelField("Rounding Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_roundingMode, new GUIContent("Mode"));

            // Border thickness only shown in BORDER mode
            if (_roundingMode.enumValueIndex == (int)RoundingMode.BORDER)
            {
                EditorGUILayout.Slider(_borderThickness, 0f, 1f, new GUIContent("Border Thickness"));
            }

            EditorGUILayout.Slider(_distanceFalloff, 0f, 5f, new GUIContent("Distance Falloff", "Anti-aliasing amount for smooth edges"));

            // Corners Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Corners", EditorStyles.boldLabel);
            if (_roundingAmount.isArray && _roundingAmount.arraySize == 4)
            {
                var topLeft = _roundingAmount.GetArrayElementAtIndex(0);
                var topRight = _roundingAmount.GetArrayElementAtIndex(1);
                var bottomLeft = _roundingAmount.GetArrayElementAtIndex(2);
                var bottomRight = _roundingAmount.GetArrayElementAtIndex(3);

                EditorGUILayout.Slider(topLeft, 0f, 1f, new GUIContent("Top Left"));
                EditorGUILayout.Slider(topRight, 0f, 1f, new GUIContent("Top Right"));
                EditorGUILayout.Slider(bottomLeft, 0f, 1f, new GUIContent("Bottom Left"));
                EditorGUILayout.Slider(bottomRight, 0f, 1f, new GUIContent("Bottom Right"));
            }

            // HitBox Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("HitBox", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useHitBoxOutside, new GUIContent("Use HitBox Outside"));

            // Only show inside hitbox option when outside hitbox is enabled
            if (_useHitBoxOutside.boolValue)
            {
                EditorGUILayout.PropertyField(_useHitBoxInside, new GUIContent("Use HitBox Inside"));
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the Image settings including Fill support.
        /// </summary>
        private void DrawImageSettings()
        {
            EditorGUILayout.LabelField("Image Settings", EditorStyles.boldLabel);

            // Source Image
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Sprite"), new GUIContent("Source Image"));

            // Color
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"), new GUIContent("Color"));

            // Material - Hidden (uses default rounded corners shader automatically)
            // EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Material"), new GUIContent("Material"));

            // Image Type (Simple or Filled)
            EditorGUILayout.PropertyField(_imageType, new GUIContent("Image Type"));

            Image.Type imageType = (Image.Type)_imageType.enumValueIndex;

            // Show Fill options when type is Filled
            if (imageType == Image.Type.Filled)
            {
                EditorGUI.indentLevel++;

                // Fill Method
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_fillMethod, new GUIContent("Fill Method"));
                if (EditorGUI.EndChangeCheck())
                {
                    _fillOrigin.intValue = 0;
                }

                // Fill Origin (changes based on Fill Method)
                Image.FillMethod fillMethod = (Image.FillMethod)_fillMethod.enumValueIndex;
                DrawFillOrigin(fillMethod);

                // Fill Amount
                EditorGUILayout.Slider(_fillAmount, 0f, 1f, new GUIContent("Fill Amount"));

                // Clockwise (for radial fills)
                if (fillMethod > Image.FillMethod.Vertical)
                {
                    EditorGUILayout.PropertyField(_fillClockwise, new GUIContent("Clockwise"));
                }

                EditorGUI.indentLevel--;
            }

            // Preserve Aspect
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PreserveAspect"), new GUIContent("Preserve Aspect"));

            // Raycast Target
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"), new GUIContent("Raycast Target"));

            // Maskable
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Maskable"), new GUIContent("Maskable"));
        }

        /// <summary>
        /// Draws the Fill Origin field based on the Fill Method.
        /// </summary>
        private void DrawFillOrigin(Image.FillMethod fillMethod)
        {
            int originValue = _fillOrigin.intValue;

            switch (fillMethod)
            {
                case Image.FillMethod.Horizontal:
                    originValue = (int)(Image.OriginHorizontal)EditorGUILayout.EnumPopup(
                        "Fill Origin",
                        (Image.OriginHorizontal)originValue
                    );
                    break;

                case Image.FillMethod.Vertical:
                    originValue = (int)(Image.OriginVertical)EditorGUILayout.EnumPopup(
                        "Fill Origin",
                        (Image.OriginVertical)originValue
                    );
                    break;

                case Image.FillMethod.Radial90:
                    originValue = (int)(Image.Origin90)EditorGUILayout.EnumPopup(
                        "Fill Origin",
                        (Image.Origin90)originValue
                    );
                    break;

                case Image.FillMethod.Radial180:
                    originValue = (int)(Image.Origin180)EditorGUILayout.EnumPopup(
                        "Fill Origin",
                        (Image.Origin180)originValue
                    );
                    break;

                case Image.FillMethod.Radial360:
                    originValue = (int)(Image.Origin360)EditorGUILayout.EnumPopup(
                        "Fill Origin",
                        (Image.Origin360)originValue
                    );
                    break;
            }

            _fillOrigin.intValue = originValue;
        }

        /// <summary>
        /// Adds the RoundUI Image in the "Create GameObject" menu.
        /// </summary>
        [MenuItem("GameObject/UI/RoundUI Image")]
        public static void CreateRoundedImage()
        {
            var go = new GameObject("RoundUI Image")
            {
                layer = LayerMask.NameToLayer("UI")
            };
            
            // Try to parent to selected object or canvas
            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponentInParent<Canvas>() != null)
            {
                go.transform.SetParent(Selection.activeGameObject.transform, false);
            }
            else
            {
                // Find or create canvas
                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                    EditorApplication.ExecuteMenuItem("GameObject/UI/Canvas");

                canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                    go.transform.SetParent(canvas.transform, false);
            }

            Selection.activeGameObject = go;
        }
    }
}
