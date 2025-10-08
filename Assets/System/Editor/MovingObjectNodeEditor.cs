using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(MovingObjectNode))]
[CanEditMultipleObjects]
public class MovingObjectNodeEditor : Editor
{
    public void OnDestroy()
    {
        MovingObjectNode node = (MovingObjectNode)target;
        int nodeInstanceID = node.GetInstanceID();
        int index = ArrayUtility.IndexOf(node.movingObject.nodes,node);
        Undo.undoRedoPerformed += () => OnUndoRedo(nodeInstanceID, index);
        RemoveNullNodes();
    }

    public void RemoveNullNodes()
    {
        MovingObjectNode node = (MovingObjectNode)target;
        MovingObject movingObject = node.movingObject;
        bool cleanNodes = false;
        for (int i = 0; i < movingObject.nodes.Length; i++)
        {
            MovingObjectNode eachNode = movingObject.nodes[i];
            if (eachNode == null) cleanNodes = true;
            else eachNode.lastKnownIndex = i;
        }
        if (cleanNodes)
        {
            // Undo.RegisterCompleteObjectUndo(movingObject, "Moving Object - Cleared Null Nodes");
            List<MovingObjectNode> nodeList = new List<MovingObjectNode>();
            nodeList.AddRange(movingObject.nodes);
            nodeList.RemoveAll(x => x == null);
            movingObject.nodes = nodeList.ToArray();

            EditorUtility.SetDirty(movingObject);
        }
    }

    private void OnUndoRedo(int nodeId, int index)
    {
        // This gets called on any undo/redo, so we need to check if our node was restored
        MovingObjectNode restoredNode = EditorUtility.InstanceIDToObject(nodeId) as MovingObjectNode;

        if (restoredNode != null && restoredNode.movingObject != null)
        {
            MovingObject movingObject = restoredNode.movingObject;

            List<MovingObjectNode> nodeList = new List<MovingObjectNode>(movingObject.nodes);
            if (nodeList.Count <= index || nodeList[index] != restoredNode)
            {
                Undo.RegisterCompleteObjectUndo(movingObject, "Restore Node to List");
                nodeList.Insert(index, restoredNode);
                movingObject.nodes = nodeList.ToArray();
                EditorUtility.SetDirty(movingObject);
            }
        }
        
        // Unregister the callback to avoid memory leaks
        Undo.undoRedoPerformed -= () => OnUndoRedo(nodeId, index);
    }
}
