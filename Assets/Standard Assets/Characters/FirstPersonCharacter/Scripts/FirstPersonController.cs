using System;
using System.Collections;
using System.Collections.Generic;
//using System.Threading.Tasks;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson {
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour {
        [SerializeField]
        private bool m_IsWalking;
        [SerializeField]
        private float m_WalkSpeed;
        [SerializeField]
        private float m_RunSpeed;
        [SerializeField]
        [Range(0f, 1f)]
        private float m_RunstepLenghten;
        [SerializeField]
        private float m_JumpSpeed;
        [SerializeField]
        private float m_StickToGroundForce;
        [SerializeField]
        private float m_GravityMultiplier;
        [SerializeField]
        private MouseLook m_MouseLook;
        [SerializeField]
        private bool m_UseFovKick;
        [SerializeField]
        private FOVKick m_FovKick = new FOVKick();
        [SerializeField]
        private bool m_UseHeadBob;
        [SerializeField]
        private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField]
        private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField]
        private float m_StepInterval;
        [SerializeField]
        private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField]
        private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField]
        private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private AudioSource m_AudioSource;
        private int m_Target;
        private List<Vector3> mousePositions;
        private Vector3? lastDrawPos;
        private GameObject back;
        private TextureController t_contorller;
        private GameObject pointer;
        public float Mouse;

        private void Awake() {
            back = GameObject.Find("Back");
        }

        // Use this for initialization
        private void Start() {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_AudioSource = GetComponent<AudioSource>();
            m_Target = 0;
            mousePositions = new List<Vector3>();
            lastDrawPos = null;
            t_contorller = back.GetComponent<TextureController>();
            pointer = GameObject.Find("Pointer");
            m_MouseLook.Init(transform, m_Camera.transform);
        }


        // Update is called once per frame
        private void Update() {
            RotateView();
            PointerToTexture();
            if (Input.GetMouseButton(0)) {
                var pos = CrossPlatformInputManager.mousePosition;
                mousePositions.Add(pos);
            } else if (lastDrawPos.HasValue) {
                lastDrawPos = null;
            }
        }

        private void PlayLandingSound() {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate() {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;


            if (!m_CharacterController.isGrounded) {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
            back.transform.position = this.transform.position;

            //ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
        }

        //private void Paint() {
        //    if (mousePositions.Count > 0) {
        //        var data = texture.GetPixels();
        //        int width = 1;
        //        if (!lastDrawPos.HasValue) {
        //            lastDrawPos = mousePositions[0];
        //            mousePositions.RemoveAt(0);
        //        }
        //        if (mousePositions.Count == 0) {
        //            var pos = lastDrawPos.Value;
        //            if (pos.x > 0 && pos.x < Screen.width && pos.y > 0 && pos.y < Screen.height)
        //                BlackOut((int)pos.x, (int)(pos.y), data, width);
        //        } else {
        //            foreach (var pos in mousePositions) {
        //                if (pos.x > 0 && pos.x < Screen.width && pos.y > 0 && pos.y < Screen.height) {
        //                    DrawLine(lastDrawPos.Value, pos, data, width);
        //                }
        //                lastDrawPos = pos;
        //            }
        //        }
        //        Texture2D newTex = new Texture2D(texture.width, texture.height);
        //        newTex.SetPixels(data);
        //        newTex.Apply();
        //        DestroyImmediate(texture);
        //        texture = newTex;
        //        mousePositions.Clear();
        //    }
        //}

        //private void DrawLine(Vector3 start, Vector3 end, Color[] data, int range) {
        //    var delta = (end - start).normalized / 2;
        //    while((start - end).magnitude > 1) {
        //        if (start.x > 0 && start.x < Screen.width && start.y > 0 && start.y < Screen.height) {
        //            BlackOut((int)(start.x), (int)(start.y), data, range);
        //        }
        //        start += delta;
        //    }
        //}

        //private void BlackOut(int x, int y, Color[] data, int range) {
        //    int index = (x + m_Target) % texture.width + y * texture.height / Screen.height * texture.width;
        //    ChangeIListData(index, data, Color.black);
        //    for (int i = 1; i < range; i++) {
        //        ChangeIListData(index + i, data, Color.black);
        //        ChangeIListData(index - i, data, Color.black);
        //        ChangeIListData(index + texture.width * i, data, Color.black);
        //        ChangeIListData(index - texture.width * i, data, Color.black);
        //    }
        //}

        private void ChangeIListData<T>(int index, IList a, T data) {
            if (index >= 0 && index < a.Count) {
                try {
                    a[index] = data;
                } catch(ArrayTypeMismatchException e){
                    print(e.Message);
                }
            }
        }

        private void PlayJumpSound() {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed) {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0)) {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) *
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep)) {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio() {
            if (!m_CharacterController.isGrounded) {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed) {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob) {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded) {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed * (m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            } else {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed) {
            // Read input
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(0, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1) {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0) {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView() {
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            int delta = 1;
            if (horizontal > 0) m_Target += delta;
            else if (horizontal < 0) m_Target -= delta;
            float theta = m_Target * Mathf.PI / 180;
            //(0, 0, 1)‚ðxz•½–Êã‚Åtheta‚¾‚¯‰E‚É‰ñ“]‚³‚¹‚½•ûŒü‚ðŒü‚­
            var vec = new Vector3(1 * Mathf.Sin(theta), 0, 1 * Mathf.Cos(theta));

            transform.localRotation = Quaternion.LookRotation(vec);
            MovePointer(this.transform.position + vec * back.transform.localScale.x / 2);
        }

        private void MovePointer(Vector3 pos) {
            var m_pos = CrossPlatformInputManager.mousePosition;
            var screenSize = new Vector3(Screen.width, Screen.height, 0);
            var screenCenter = screenSize * 0.5f;
            var diff = m_pos - screenCenter;
            //Ž‹“_‚É‚æ‚édiff‚Ì•â³
            float theta = m_Target * Mathf.PI / 180;
            diff = new Vector3(diff.x * Mathf.Cos(theta) + diff.z * Mathf.Sin(theta), diff.y, diff.z * Mathf.Cos(theta) - diff.x * Mathf.Sin(theta));
            var length = pos.magnitude;
            pos += diff * Mouse;
            //’·‚³‚ð•Û‚Â
            pos = pos.normalized * length;
            pointer.transform.position = pos;
            pointer.transform.eulerAngles = this.transform.eulerAngles; //í‚É‚±‚Á‚¿‚ðŒü‚­‚æ‚¤‚É
        }

        private void PointerToTexture() {
            var texture = t_contorller.GetTexture();
            var screenLength = back.transform.localScale.x * Mathf.PI;
            var textureStartPoint = new Vector3(50, 0, 0);
            var pointerPos = pointer.transform.position;
            var theta = Mathf.Atan2(pointerPos.z - textureStartPoint.z, pointerPos.x - textureStartPoint.x);
            print(theta);
            var lengthPercent = theta / Mathf.PI;

        }

        private void OnControllerColliderHit(ControllerColliderHit hit) {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below) {
                return;
            }

            if (body == null || body.isKinematic) {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
