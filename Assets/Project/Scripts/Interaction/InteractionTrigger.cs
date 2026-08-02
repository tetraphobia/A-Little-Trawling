using UnityEngine;

namespace LittleTrawling.Interaction
{
    /// <summary>
    /// Trigger component attached to interactable objects.
    /// Detects when the player avatar enters or exits interaction range.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionTrigger : MonoBehaviour
    {
        [Tooltip("Tag of player object.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Trigger radius if a SphereCollider is generated automatically.")]
        [SerializeField] private float triggerRadius = 3.0f;

        private IInteractable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<IInteractable>() ?? GetComponentInParent<IInteractable>();
            
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = triggerRadius;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            if (_interactable == null) _interactable = GetComponent<IInteractable>() ?? GetComponentInParent<IInteractable>();

            if (_interactable != null && InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.RegisterInteractable(_interactable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;

            if (_interactable != null && InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.UnregisterInteractable(_interactable);
            }
        }

        private bool IsPlayer(Collider other)
        {
            return other.CompareTag(playerTag) || (other.transform.parent != null && other.transform.parent.CompareTag(playerTag));
        }

        private void OnDisable()
        {
            if (_interactable != null && InteractionSystem.Instance != null)
            {
                InteractionSystem.Instance.UnregisterInteractable(_interactable);
            }
        }
    }
}
