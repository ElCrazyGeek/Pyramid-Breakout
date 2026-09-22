using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(Torreta))]
public class TorretaEditor : Editor
{
    SerializedProperty tipoProp;
    SerializedProperty objetivoProp;
    SerializedProperty bocaProp;

    SerializedProperty velocidadRotacionProp;
    SerializedProperty rotarSoloYProp;

    SerializedProperty cadenciaProp;
    SerializedProperty alcanceProp;
    SerializedProperty mascaraProp;
    SerializedProperty prefabProp;

    SerializedProperty followDistanceProp;
    SerializedProperty followResponsivenessProp;
    SerializedProperty rotacionOffsetProp;
    SerializedProperty invertirDireccionProp;

    SerializedProperty tiempoVidaProp;
    SerializedProperty velocidadRetiradaProp;
    SerializedProperty tiempoRetiradaProp;

    SerializedProperty detectionRadiusProp;
    SerializedProperty perderAggroTiempoProp;

    void OnEnable()
    {
        tipoProp = serializedObject.FindProperty("tipo");
        objetivoProp = serializedObject.FindProperty("objetivo");
        bocaProp = serializedObject.FindProperty("boca");

        velocidadRotacionProp = serializedObject.FindProperty("velocidadRotacion");
        rotarSoloYProp = serializedObject.FindProperty("rotarSoloY");

        cadenciaProp = serializedObject.FindProperty("cadenciaDisparo");
        alcanceProp = serializedObject.FindProperty("alcance");
        mascaraProp = serializedObject.FindProperty("mascaraObjetivo");
        prefabProp = serializedObject.FindProperty("prefabProyectil");

        followDistanceProp = serializedObject.FindProperty("followDistance");
        followResponsivenessProp = serializedObject.FindProperty("followResponsiveness");
        rotacionOffsetProp = serializedObject.FindProperty("rotacionOffsetEuler");
        invertirDireccionProp = serializedObject.FindProperty("invertirDireccion");

        tiempoVidaProp = serializedObject.FindProperty("tiempoVida");
        velocidadRetiradaProp = serializedObject.FindProperty("velocidadRetirada");
        tiempoRetiradaProp = serializedObject.FindProperty("tiempoRetirada");

        detectionRadiusProp = serializedObject.FindProperty("detectionRadius");
        perderAggroTiempoProp = serializedObject.FindProperty("perderAggroTiempo");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Referencias", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(objetivoProp);
        EditorGUILayout.PropertyField(bocaProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rotación", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(velocidadRotacionProp);
        EditorGUILayout.PropertyField(rotarSoloYProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Disparo", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cadenciaProp);
        EditorGUILayout.PropertyField(alcanceProp);
        EditorGUILayout.PropertyField(mascaraProp);
        EditorGUILayout.PropertyField(prefabProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Modo y seguimiento", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tipoProp, new GUIContent("Tipo de torreta"));

        // Mostrar campos de seguimiento pero deshabilitarlos si no aplican
        bool esSeguidora = ((Torreta.TipoTorreta)tipoProp.enumValueIndex) == Torreta.TipoTorreta.Seguidora;

        EditorGUI.BeginDisabledGroup(!esSeguidora);
        EditorGUILayout.PropertyField(followDistanceProp, new GUIContent("Distancia delante"));
        EditorGUILayout.PropertyField(followResponsivenessProp, new GUIContent("Responsiveness"));
        EditorGUILayout.PropertyField(rotacionOffsetProp, new GUIContent("Rotación offset (Euler)"));
        EditorGUILayout.PropertyField(invertirDireccionProp, new GUIContent("Invertir apuntado"));
        EditorGUI.EndDisabledGroup();

        if (!esSeguidora)
        {
            EditorGUILayout.HelpBox("Modo Estacionaria: la torreta se queda fija cuando detecta al jugador.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Modo Seguidora: mantiene distancia delante de la cámara y se mueve de forma suave.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Aggro / Tiempo", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(detectionRadiusProp, new GUIContent("Radio detección"));
        EditorGUILayout.PropertyField(perderAggroTiempoProp, new GUIContent("Tiempo perder aggro (s)"));
        EditorGUILayout.PropertyField(tiempoVidaProp);
        EditorGUILayout.PropertyField(velocidadRetiradaProp);
        EditorGUILayout.PropertyField(tiempoRetiradaProp);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
