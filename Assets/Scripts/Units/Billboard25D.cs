using UnityEngine;

namespace ElementalHexTactics3D.Units
{
    /// <summary>
    /// Forces a 2D sprite standee to continuously face the tactical camera,
    /// giving the classic 2.5D HD-2D / Tactics Diorama aesthetic (Triangle Strategy / FFT / Paper Mario).
    /// </summary>
    [ExecuteAlways]
    public class Billboard25D : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Header("Billboard Settings")]
        [Tooltip("When true, matches camera tilt so sprite never foreshortens. When false, only rotates around Y.")]
        [SerializeField] private bool matchCameraPitch = true;

        [SerializeField] private bool flipX = false;
        private SpriteRenderer spriteRenderer;

        public bool FlipX
        {
            get => flipX;
            set
            {
                flipX = value;
                if (spriteRenderer != null) spriteRenderer.flipX = flipX;
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            FindCameraIfNull();
        }

        private void Start()
        {
            FindCameraIfNull();
            AlignWithCamera();
        }

        private void LateUpdate()
        {
            AlignWithCamera();
        }

        private void FindCameraIfNull()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }
        }

        public void AlignWithCamera()
        {
            if (targetCamera == null)
            {
                FindCameraIfNull();
                if (targetCamera == null) return;
            }

            if (matchCameraPitch)
            {
                // Align rotation to match camera lens plane
                transform.rotation = targetCamera.transform.rotation;
            }
            else
            {
                // Cylindrical billboard: rotate only around world Y
                Vector3 forward = targetCamera.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
            }

            if (spriteRenderer != null && spriteRenderer.flipX != flipX)
            {
                spriteRenderer.flipX = flipX;
            }
        }

        public void FaceDirection(Vector3 worldTarget)
        {
            Vector3 diff = worldTarget - transform.position;
            // Determine if target is to the left or right relative to camera view
            if (targetCamera != null)
            {
                Vector3 localDiff = targetCamera.transform.InverseTransformDirection(diff);
                FlipX = localDiff.x < -0.1f;
            }
            else
            {
                FlipX = diff.x < -0.1f;
            }
        }
    }
}

