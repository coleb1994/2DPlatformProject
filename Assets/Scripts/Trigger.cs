using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class Trigger : MonoBehaviour
{
    [Tooltip("The event to trigger when the player or an object enters the trigger area.")]
    public UnityEvent triggerEnterEvent;
    public TriggerExitEvent triggerExitEvent;
    public TriggeringObjectEvents triggeringObjectEvents;

    [Space]
    [Tooltip("Whether or not this trigger can be activated by the player.")]
    public bool triggerableByPlayer = true;
    [Tooltip("Whether or not this trigger can be activated by ANY other objects, like physics objects.")]
    public bool triggerableByObject = false;
    [Tooltip("Whether or not this trigger can be activated by SPECIFIC other objects, like physics objects. Only objects with the tag specified in 'Object Tag' will activate this trigger.")]
    public bool triggerableByTaggedObject = false;
    [Tooltip("The specific tag objects will need to activate this trigger, if 'Triggerable By Tagged Object' is selected. Tags can be chosen at the top of a GameObject's inspector, under the dropdown labeled 'Tag'. You can make your own tags by selecting 'Add Tag...'.")]
    public string objectTag = "";
    [Space]
    [Tooltip("Whether or not this trigger will destroy itself after it is activated. Use for events you only want to happen once.")]
    public bool destroyAfterTrigger;
    private Collider2D collider2D;

    [Space]
    [Header("Gizmos")]
    [Tooltip("Whether or not the trigger area will be visible in the editor window. This won't be visisle in the game while playing.")]
    public bool drawTriggerArea = true;
    [Tooltip("Whether or not lines will be drawn to all connected components in the editor window. This won't be visisle in the game while playing.")]
    public bool drawTriggerLines = true;    
    [Tooltip("The color of the trigger area when drawn in the editor.")]
    public Color triggerAreaColor = new Color(0, 1, 0, 0.5f);
    [Tooltip("Whether or not this script prints information to the debug console.")]
    public bool consoleLogOnTrigger = false;

    public void Start()
    {
        collider2D = GetComponent<Collider2D>();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (CanObjectTriggerThis(other))
        {
            if (consoleLogOnTrigger)
            {
                Debug.Log("Trigger Enter activated: " + gameObject.name);
            }

            triggerEnterEvent.Invoke();
            triggeringObjectEvents.triggeringObjectEnterEvent.Invoke(other.gameObject);

            if (destroyAfterTrigger)
                {
                    Destroy(gameObject);
                }
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        if (CanObjectTriggerThis(other))
        {
            if (consoleLogOnTrigger)
            {
                Debug.Log("Trigger Exit activated: " + gameObject.name);
            }

            triggerExitEvent.triggerExitEvent.Invoke();
            triggeringObjectEvents.triggeringObjectExitEvent.Invoke(other.gameObject);
        }
    }

    private bool CanObjectTriggerThis(Collider2D other)
    {
        if (triggerableByObject
            && !other.gameObject.GetComponent<PlayerController>()
            && !other.gameObject.isStatic)
        {
            Collider2D collider = other.gameObject.GetComponent<Collider2D>();
            return !collider.isTrigger;
        }
        if (triggerableByPlayer && other.gameObject.GetComponent<PlayerController>())
        {
            return true;
        }
        if (triggerableByTaggedObject && other.gameObject.tag == objectTag.Trim())
        {
            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (drawTriggerLines)
        {
            Gizmos.color = Color.green;
            for (int j = 0; j < triggerEnterEvent.GetPersistentEventCount(); j++)
            {
                Object target = triggerEnterEvent.GetPersistentTarget(j);
                if (target != null && target is GameObject)
                {
                    Gizmos.DrawLine(transform.position, (target as GameObject).transform.position);
                }
                if (target != null && target is MonoBehaviour && !(target as MonoBehaviour).GetComponent<PlayerController>())
                {
                    Gizmos.DrawLine(transform.position, (target as MonoBehaviour).transform.position);
                }
            }
            Gizmos.color = Color.yellow;
            for (int j = 0; j < triggerExitEvent.triggerExitEvent.GetPersistentEventCount(); j++)
            {
                Object target = triggerExitEvent.triggerExitEvent.GetPersistentTarget(j);
                if (target != null && target is GameObject)
                {
                    Gizmos.DrawLine(transform.position, (target as GameObject).transform.position);
                }
                if (target != null && target is MonoBehaviour && !(target as MonoBehaviour).GetComponent<PlayerController>())
                {
                    Gizmos.DrawLine(transform.position, (target as MonoBehaviour).transform.position);
                }
            }

        }

        if (drawTriggerArea)
        {
            if (collider2D == null) collider2D = GetComponent<Collider2D>();
            if (collider2D != null && collider2D is BoxCollider2D)
            {
                Gizmos.color = triggerAreaColor;
                Vector3 worldCenter = collider2D.transform.TransformPoint(collider2D.offset);
                Vector3 worldExtents = collider2D.transform.TransformVector((collider2D as BoxCollider2D).size);
                Gizmos.DrawCube(worldCenter, worldExtents);
            }
        }
    }

    [System.Serializable]
    public class TriggerExitEvent
    {
        [Tooltip("The event to trigger when the player or an object EXITS the trigger area.")]
        public UnityEvent triggerExitEvent;
    }

    [System.Serializable]
    public class TriggeringObjectEvents
    {
        [Tooltip("The event to trigger when an object ENTERS the trigger area. The components activated by this event will apply their changes to the specific object that entered this trigger area.")]
        public UnityEvent<GameObject> triggeringObjectEnterEvent;
        [Tooltip("The event to trigger when an object EXITS the trigger area. The components activated by this event will apply their changes to the specific object that entered this trigger area.")]
        public UnityEvent<GameObject> triggeringObjectExitEvent;
    }
}
