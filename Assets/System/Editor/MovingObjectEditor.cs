using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(MovingObject))]
public class MovingObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MovingObject movingObject = (MovingObject)target;

        if (GUILayout.Button("Add Node"))
        {
            Undo.IncrementCurrentGroup();
            int undoGroupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Moving Object - Add Node");
            Undo.RegisterCompleteObjectUndo(movingObject, "Moving Object - Add Node To List");
            GameObject newNode = new GameObject();
            Undo.RegisterCreatedObjectUndo(newNode, "Moving Object - Create Node");
            newNode.name = movingObject.name + " Node" + movingObject.nodes.Length;
            newNode.transform.parent = movingObject.transform.parent;
            newNode.transform.position = new Vector3(
                SceneView.lastActiveSceneView.camera.transform.position.x,
                SceneView.lastActiveSceneView.camera.transform.position.y,
                0
            );
            MovingObjectNode nodeIcon = newNode.AddComponent<MovingObjectNode>();
            nodeIcon.nodeColor = movingObject.nodeColor;
            nodeIcon.nodeIcon = movingObject.movingObjectIcon;
            nodeIcon.SetMovingObject(movingObject);
            movingObject.AddNewNode(nodeIcon);
            EditorUtility.SetDirty(movingObject);
            Undo.CollapseUndoOperations(undoGroupIndex);
        }
    }
}