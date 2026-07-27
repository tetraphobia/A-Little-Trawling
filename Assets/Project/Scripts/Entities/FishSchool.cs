using UnityEngine;
using LittleTrawling.Core;
using LittleTrawling.Vehicles;

namespace LittleTrawling.Entities
{
    /// <summary>
    /// Represents a school of fish swimming in open water.
    /// </summary>
    public class FishSchool : MonoBehaviour
    {
        [Header("School Settings")]
        [Tooltip("Max interaction distance from the boat.")]
        [SerializeField] private float interactRadius = 6.0f;

        [Tooltip("Number of fish remaining in this school before it despawns.")]
        [SerializeField] private int fishRemaining = 4;

        private BoatController _boat;
        private bool _isBoatNear;
        private Transform _shadowContainer;

        public int FishRemaining => fishRemaining;
        public bool IsDepleted => fishRemaining <= 0;

        private void Start()
        {
            if (fishRemaining <= 0)
                fishRemaining = Random.Range(3, 6);

            CreateFishShadows();
            Debug.Log($"[FishSchool] Initialized FishSchool at world position {transform.position} with {fishRemaining} fish remaining.");
        }

        private void CreateFishShadows()
        {
            // Create a parent object for fish shadows
            var container = new GameObject("Shadows");
            container.transform.SetParent(transform, false);
            _shadowContainer = container.transform;

            Shader shadowShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Legacy Shaders/Diffuse");

            // 1. Water Ripple Ring on Surface
            var ripple = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ripple.name = "WaterRipple";
            ripple.transform.SetParent(transform, false);
            ripple.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ripple.transform.localScale = new Vector3(6f, 0.01f, 6f);
            var colRipple = ripple.GetComponent<Collider>();
            if (colRipple != null) Destroy(colRipple);
            var mrRipple = ripple.GetComponent<MeshRenderer>();
            if (mrRipple != null)
            {
                if (shadowShader != null) mrRipple.material = new Material(shadowShader);
                mrRipple.material.color = new Color(0.2f, 0.7f, 0.9f, 0.35f);
            }

            // 2. Swimming Fish Shadow Quads
            int count = Random.Range(5, 9);
            for (int i = 0; i < count; i++)
            {
                var shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shadow.name = $"FishShadow_{i}";
                shadow.transform.SetParent(_shadowContainer, false);

                // Flatten and position slightly above water surface
                shadow.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                Vector2 circle = Random.insideUnitCircle * 2.8f;
                shadow.transform.localPosition = new Vector3(circle.x, 0.12f, circle.y);
                shadow.transform.localScale = new Vector3(0.7f, 1.6f, 1f);

                // Remove collider
                var col = shadow.GetComponent<Collider>();
                if (col != null) Destroy(col);

                // Material color (vivid dark navy fish silhouette)
                var mr = shadow.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    if (shadowShader != null) mr.material = new Material(shadowShader);
                    mr.material.color = new Color(0.02f, 0.12f, 0.22f, 0.95f);
                }
            }
        }

        private void Update()
        {
            // Rotate shadows to simulate swimming underwater
            if (_shadowContainer != null)
            {
                _shadowContainer.Rotate(Vector3.up, 30f * Time.deltaTime);
            }

            // Track distance to player boat
            if (_boat == null)
            {
                _boat = Object.FindAnyObjectByType<BoatController>();
            }

            if (_boat != null)
            {
                float dist = Vector3.Distance(_boat.transform.position, transform.position);
                bool wasNear = _isBoatNear;
                _isBoatNear = (dist <= interactRadius);

                if (_isBoatNear && !wasNear)
                {
                    Debug.Log($"[FishSchool] Boat entered interaction radius for school at {transform.position} (dist={dist:F2}m)");
                }
            }
            else
            {
                _isBoatNear = false;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }

        public bool CanFish()
        {
            var gm = GameManager.Instance;
            return _isBoatNear && !IsDepleted && gm != null && gm.IsState(GameState.Walking);
        }

        public void ConsumeFish()
        {
            fishRemaining--;
            if (fishRemaining <= 0)
            {
                Destroy(gameObject, 0.5f);
            }
        }

        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (!_isBoatNear || IsDepleted || gm == null || !gm.IsState(GameState.Walking)) return;

            int width = 300;
            int height = 40;
            Rect rect = new Rect((Screen.width - width) / 2f, Screen.height - 150, width, height);

            GUI.Box(rect, "");
            GUI.Label(rect, "<size=16><b>Interact to Fish</b></size>", new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true
            });
        }
    }
}
