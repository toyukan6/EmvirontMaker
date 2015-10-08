using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private GameObject ground;
        private GameObject pointer;
        private TextureManager manager;
        private Texture2D[][] textures;
        private Vector2[][] starts;
        private Vector2[][] ends;
        private Vector3 startPos;
        private GameObject[] cylinders;
        private GameObject[] cylinders_oppo;
        private int textureNumber = 0;
        public float Mouse;
        public GameObject Cylinder;

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
            manager = GameObject.Find("TextureManager").GetComponent<TextureManager>();
            textures = manager.Textures;
            starts = manager.StartPoses;
            ends = manager.EndPoses;
            t_contorller.SetTexture2D(textures[0][0]);
            ground = GameObject.Find("Ground");
            ground.GetComponent<MeshRenderer>().material.mainTexture = textures[0][1];
            cylinders = new GameObject[textures.Max(t => t.Length) - 2];
            cylinders_oppo = new GameObject[textures.Max(t => t.Length) - 2];
            MakeCylinders();
            startPos = this.transform.position;
            m_MouseLook.Init(transform, m_Camera.transform);
        }

        private void MakeCylinders() {
            for (int j = 2; j < starts[0].Length; j++) {
                float percent = starts[0][j].x / textures[0][0].width;
                float theta = Mathf.PI * 2 * percent;
                var offset = new Vector3(50 * -Mathf.Sin(theta), 0, 50 * Mathf.Cos(theta));
                var c = Instantiate(Cylinder, this.transform.position + offset, Quaternion.identity) as GameObject;
                var v = starts[0][j] - ends[0][j];
                c.transform.localScale = new Vector3(v.x, v.y, v.x) / 100;
                c.GetComponent<CylinderTextureController>().SetTexture(textures[0][j]);
                cylinders[j - 2] = c;
                cylinders_oppo[j - 2] = Instantiate(c, this.transform.position - offset, Quaternion.identity) as GameObject;
            }
        }

        // Update is called once per frame
        private void Update() {
            RotateView();
            ChangeTexture();
            if (Input.GetMouseButton(0)) {
                var pos = CrossPlatformInputManager.mousePosition;
                mousePositions.Add(pos);
                var t_pos = PointerToTexture();
                for (int i = 0; i < textures.Length; i++) {
                    var t = textures[i][0];
                    t.SetPixel((int)t_pos.x, (int)t_pos.y, Color.black);
                    t.SetPixel((int)t_pos.x - 1, (int)t_pos.y, Color.black);
                    t.SetPixel((int)t_pos.x + 1, (int)t_pos.y, Color.black);
                    t.SetPixel((int)t_pos.x, (int)t_pos.y - 1, Color.black);
                    t.SetPixel((int)t_pos.x, (int)t_pos.y + 1, Color.black);
                    t.Apply();
                }
            } else if (lastDrawPos.HasValue) {
                lastDrawPos = null;
            }
        }

        private void PlayLandingSound() {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }

        private void ChangeTexture() {
            var pos = this.transform.position;
            int tn = textureNumber;
            if (startPos.x - pos.x > 0.5) {
                if (textureNumber == 0)
                    textureNumber = 1;
            } else if (textureNumber == 1) {
                textureNumber = 0;
            }
            if (tn != textureNumber) {
                t_contorller.SetTexture2D(textures[textureNumber][0]);
                for (int i = 0; i < cylinders.Length; i++) {
                    cylinders[i].GetComponent<CylinderTextureController>().SetTexture(textures[textureNumber][i + 2]);
                    cylinders_oppo[i].GetComponent<CylinderTextureController>().SetTexture(textures[textureNumber][i + 2]);
                }
            }
            for (int i = 0; i < cylinders.Length; i++) {
                cylinders[i].transform.localEulerAngles = new Vector3(cylinders[i].transform.localEulerAngles.x, this.transform.eulerAngles.y, this.transform.eulerAngles.z);
            }
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
            back.transform.position = this.transform.position + new Vector3(0, 23, 0);

            //ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
        }

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

        private Vector2 PointerToTexture() {
            var texture = t_contorller.GetTexture();
            var screenLength = back.transform.localScale.x * Mathf.PI;
            var textureStartPoint = new Vector3(50, 0, 0);
            var pointerPos = pointer.transform.position;
            var theta = Mathf.Atan2(pointerPos.z - textureStartPoint.z, pointerPos.x - textureStartPoint.x);
            theta -= Mathf.Sign(theta) * Mathf.PI;
            if (theta > 0) theta = Mathf.PI / 2 - theta;
            var lengthPercent = Mathf.Abs(theta) / Mathf.PI * 2;
            float x = texture.width * lengthPercent;
            float y = (pointerPos.y + 6) * texture.height / 60;
            return new Vector2(x, y);
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
