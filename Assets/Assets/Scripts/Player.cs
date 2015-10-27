using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace EnvironmentMaker {
    class Player : MonoBehaviour {

        /// <summary>
        /// エディットモードかどうか
        /// </summary>
        private bool editMode = false;
        /// <summary>
        /// 一人称視点側のカメラ
        /// </summary>
        private Camera firstPersonCamera;
        /// <summary>
        /// 俯瞰視点側のカメラ
        /// </summary>
        private Camera mainCamera;
        /// <summary>
        /// エディットボタンの状態
        /// </summary>
        private bool editButton;
        /// <summary>
        /// エディットボタンの大きさと位置
        /// </summary>
        private Rect editRect = new Rect(0, 0, 100, 100);

        #region エディットモード
        /// <summary>
        /// 画像表示用のプレハブ
        /// </summary>
        public GameObject SpritePrehab;
        /// <summary>
        /// 画像の配置位置を表すカーソル
        /// </summary>
        private GameObject Cursor;
        /// <summary>
        /// 表示に使える画像たち
        /// </summary>
        private List<Texture2D> textures;
        /// <summary>
        /// textures表示用のボタンの状態
        /// </summary>
        private bool showButton;
        /// <summary>
        /// textures表示用ボタンの大きさと位置
        /// </summary>
        private Rect showRect = new Rect(100, 0, 100, 100);
        /// <summary>
        /// texturesを表示しているかどうか
        /// </summary>
        private bool showTextures = false;
        /// <summary>
        /// 画像の配置できるスクリーンの領域
        /// </summary>
        private Rect editArea = new Rect(0, 100, Screen.width, Screen.height - 200);
        /// <summary>
        /// texturesの表示されている領域
        /// </summary>
        private Rect textureArea;
        /// <summary>
        /// 置かれた画像たち
        /// </summary>
        private List<GameObject> textureObjects = new List<GameObject>();
        /// <summary>
        /// textureの置かれている領域
        /// </summary>
        private List<Rect> textureRects = new List<Rect>();
        /// <summary>
        /// 表示する画像のインデックスのオフセット
        /// </summary>
        private int showTextureOffset = 0;
        /// <summary>
        /// オフセットを左に動かすボタンの状態
        /// </summary>
        private bool leftButton;
        /// <summary>
        /// オフセットを左に動かすボタンの大きさ
        /// </summary>
        private Rect leftRect = new Rect(200, 0, 50, 100);
        /// <summary>
        /// オフセットを右に動かすボタンの状態
        /// </summary>
        private bool rightButton;
        /// <summary>
        /// オフセットを右に動かすボタンの大きさ
        /// </summary>
        private Rect rightRect = new Rect(550, 0, 50, 100);
        /// <summary>
        /// 選ばれている画像
        /// </summary>
        private GameObject selectedObject = null;
        /// <summary>
        /// 立方体のプレハブ
        /// </summary>
        public GameObject CubePrehab;
        /// <summary>
        /// クリックされた場所
        /// </summary>
        private Vector3? clickedPosition = null;
        /// <summary>
        /// クリックされたテクスチャが最初にあったところ
        /// </summary>
        private Vector3 firstPosition;
        /// <summary>
        /// アンドゥで実行すること
        /// </summary>
        private Action undoAct;
        /// <summary>
        /// プレイヤーのアイコン
        /// </summary>
        private GameObject playerIcon;
        /// <summary>
        /// アイコンのプレハブ
        /// </summary>
        public GameObject iconPrehab;
        /// <summary>
        /// 背景
        /// </summary>
        public GameObject Back;
        /// <summary>
        /// 地面
        /// </summary>
        public GameObject Ground;
        /// <summary>
        /// スプライトの色
        /// </summary>
        private Color spriteColor = Color.white;
        #endregion

        #region 歩行モード
        /// <summary>
        /// 歩行制御
        /// </summary>
        private FirstPersonController controller;
        #endregion

        private void Awake() {
            firstPersonCamera = this.GetComponent<Camera>();
            mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
            textures = new List<Texture2D>();
            string path = "Textures";
            var files = Directory.GetFiles(path);
            foreach (var f in files) {
                using (FileStream stream = new FileStream(f, FileMode.Open)) {
                    using (BinaryReader reader = new BinaryReader(stream)) {
                        var bytes = new List<byte>();
                        try {
                            while(true) {
                                bytes.Add(reader.ReadByte());
                            }
                        } catch {
                            var tex = new Texture2D(2, 2);
                            tex.LoadImage(bytes.ToArray());
                            textures.Add(tex);
                        }
                    }
                }
            }
            var material = Ground.GetComponent<MeshRenderer>().sharedMaterial;
            material.mainTexture = null;
            controller = this.GetComponentInParent<FirstPersonController>();
        }

        private void Update() {
            if (editMode) {
                EditModeUpdate();
            } else {
                WalkModeUpdate();
            }
        }

        private void EditModeUpdate() {
            var pos = Input.mousePosition - new Vector3(Screen.width, Screen.height, 0) / 2;
            var screenPos = Camera.main.ScreenToViewportPoint(pos);
            var worldPos = Camera.main.ScreenToWorldPoint(pos);
            screenPos.z = screenPos.y;
            screenPos *= 12;
            screenPos += worldPos;
            screenPos.y = 0;
            if (Input.GetMouseButtonDown(0)) {
                if (BetweenScreenPos(editArea)) {
                    var trect = textureRects.Find(t => Between(t, screenPos));
                    if (trect == new Rect(0, 0, 0, 0)) {
                        ChangeSelectedObject(null);
                        var srender = Cursor.GetComponent<SpriteRenderer>();
                        var obj = CreateSpriteObject(screenPos, srender.sprite);
                        undoAct = () => DeleteTexture(obj);
                    } else {
                        int index = textureRects.IndexOf(trect);
                        int selectedIndex = textureObjects.IndexOf(selectedObject);
                        if (index != selectedIndex) {
                            ChangeSelectedObject(index);
                            clickedPosition = screenPos;
                            firstPosition = selectedObject.transform.position;
                        } else {
                            var selectedSprite = selectedObject.GetComponent<SpriteRenderer>().sprite;
                            var selectedSize = selectedSprite.bounds.size;
                            var selectPosition = selectedObject.transform.position;
                            var newPosition = new Vector3(selectPosition.x, selectedSize.y / 2.5f, selectPosition.z);
                            ChangeColor(selectedObject, Color.white);
                            var obj = Instantiate(selectedObject, newPosition, Quaternion.identity) as GameObject;
                            var bill = obj.GetComponent<BillBoard>();
                            bill.SetPlayer(this.gameObject);
                            var position = selectedObject.transform.position;
                            Action act = () => {
                                Destroy(obj.gameObject);
                                CreateSpriteObject(position, selectedSprite);
                            };
                            undoAct = act;
                            DeleteTexture(selectedObject);
                        }
                    }
                } else if (BetweenScreenPos(textureArea)) {
                    int index = (int)((Input.mousePosition.x - editRect.width - showRect.width - leftRect.width) / 100);
                    index += showTextureOffset;
                    var srender = Cursor.GetComponent<SpriteRenderer>();
                    if (index >= 0 && index < textures.Count) {
                        var texture = textures[index];
                        srender.sprite = Texture2DToSprite(texture);
                        ChangeSelectedObject(null);
                    }
                }
            }
            if (Input.GetMouseButton(0) && clickedPosition.HasValue) {
                var diff = screenPos - clickedPosition.Value;
                selectedObject.transform.position = firstPosition + diff;
            }
            if (Input.GetMouseButtonUp(0)) {
                if (clickedPosition.HasValue) {
                    undoAct = () => selectedObject.transform.position = firstPosition;
                    clickedPosition = null;
                }
            }
            if (Input.GetMouseButtonDown(1)) {
                ChangeSelectedObject(null);
            }
            CameraMove();
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) {
                DeleteTexture(selectedObject);
            }
            if (Input.GetKeyDown(KeyCode.U)) {
                Undo();
            }
            Cursor.transform.position = screenPos;
            var cursorRender = Cursor.GetComponent<SpriteRenderer>();
            if (BetweenScreenPos(editArea)) {
                cursorRender.enabled = true;
            } else {
                cursorRender.enabled = false;
            }
            //cursorRender.enabled = false;
        }

        private void CameraMove() {
            if (Input.GetKey(KeyCode.A)) {
                mainCamera.transform.position += new Vector3(-0.1f, 0, 0);
            } else if (Input.GetKey(KeyCode.W)) {
                mainCamera.transform.position += new Vector3(0, 0, 0.1f);
            } else if (Input.GetKey(KeyCode.S)) {
                mainCamera.transform.position += new Vector3(0, 0, -0.1f);
            } else if (Input.GetKey(KeyCode.D)) {
                mainCamera.transform.position += new Vector3(0.1f, 0, 0);
            }
        }

        private void DeleteTexture(GameObject texture) {
            if (texture != null) {
                var index = textureObjects.IndexOf(texture);
                textureObjects.RemoveAt(index);
                textureRects.RemoveAt(index);
                Destroy(texture.gameObject);
                texture = null;
                clickedPosition = null;
            }
        }

        private void ChangeSelectedObject(int? index) {
            if (selectedObject != null) {
                ChangeColor(selectedObject, Color.white);
            }
            if (index.HasValue) {
                selectedObject = textureObjects[index.Value];
                ChangeColor(selectedObject, Color.gray);
                clickedPosition = null;
            } else {
                selectedObject = null;
                clickedPosition = null;
            }
        }

        private void Undo() {
            if (undoAct != null) {
                undoAct();
                undoAct = null;
            }
        }

        private GameObject CreateSpriteObject(Vector3 pos, Sprite sprite) {
            var sprender = SpritePrehab.GetComponent<SpriteRenderer>();
            sprender.sprite = sprite;
            sprender.color = spriteColor;
            var obj = Instantiate(SpritePrehab, pos, Quaternion.Euler(90, 0, 0)) as GameObject;
            textureObjects.Add(obj);
            var size = sprender.bounds.size;
            var rect = new Rect(pos.x - size.x / 2, pos.z - size.z / 2, size.x, size.z);
            textureRects.Add(rect);
            return obj;
        }

        private Sprite Texture2DToSprite(Texture2D texture) {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        private void WalkModeUpdate() {
            var pos = this.transform.position;
            Back.transform.position = pos;
            Ground.transform.position = new Vector3(pos.x, 0, pos.z);
        }

        private void OnGUI() {
            editButton = GUI.Button(new Rect(0, 0, 100, 100), (editMode ? "Quit" : "Edit"));
            if (editButton) {
                editMode = !editMode;
                if (editMode) {
                    IntoEditMode();
                } else {
                    IntoWalkMode();
                }
            }
            if (editMode) {
                EditModeGUI();
            } else {
                WalkModeGUI();
            }
        }

        private void IntoEditMode() {
            mainCamera.enabled = true;
            firstPersonCamera.enabled = false;
            undoAct = null;
            controller.enabled = false;
            if (Cursor == null) {
                Cursor = Instantiate(SpritePrehab, Vector3.zero, Quaternion.Euler(90, 0, 0)) as GameObject;
            }
            if (playerIcon == null) {
                playerIcon = Instantiate(iconPrehab, this.transform.position, Quaternion.Euler(90, 0, 0)) as GameObject;
            }
        }

        private void IntoWalkMode() {
            mainCamera.enabled = false;
            firstPersonCamera.enabled = true;
            controller.enabled = true;
            if (Cursor != null) {
                Destroy(Cursor.gameObject);
                Cursor = null;
            }
            if (playerIcon != null) {
                Destroy(playerIcon.gameObject);
                playerIcon = null;
            }
        }

        private void EditModeGUI() {
            showButton = GUI.Button(showRect, (showTextures ? "Hidden" : "Textures"));
            if (showButton) {
                showTextures = !showTextures;
            }
            if (showTextures) {
                if (showTextureOffset > 0)
                    leftButton = GUI.Button(leftRect, "←");
                else
                    leftButton = false;
                if (showTextureOffset < textures.Count - 3)
                    rightButton = GUI.Button(rightRect, "→");
                else
                    rightButton = false;

                if (leftButton) {
                    showTextureOffset = Math.Max(0, showTextureOffset - 3);
                } else if (rightButton) {
                    showTextureOffset = Math.Min(showTextureOffset + 3, textures.Count - 3);
                }

                int firstX = (int)(leftRect.x + leftRect.width);
                int max = 3;
                for (int i = 0; i < max; i++) {
                    if (i + showTextureOffset >= textures.Count) break;
                    var texture = textures[i + showTextureOffset];
                    GUI.DrawTexture(new Rect(firstX + 100 * i, 0, 100, 100), texture);
                }
                textureArea = new Rect(firstX, 0, 100 * max, 100);
            } else {
                textureArea = new Rect();
            }
            var back = GUI.Button(new Rect(0, 600, 100, 100), "背景にする");
            if (back) {
                var material = Back.GetComponent<MeshRenderer>().sharedMaterial;
                var srender = Cursor.GetComponent<SpriteRenderer>();
                material.mainTexture = srender.sprite.texture;
                Back.gameObject.SetActive(true);
            }
            var ground = GUI.Button(new Rect(100, 600, 100, 100), "地面にする");
            if (ground) {
                var material = Ground.GetComponent<MeshRenderer>().sharedMaterial;
                var srender = Cursor.GetComponent<SpriteRenderer>();
                material.mainTexture = srender.sprite.texture;
                material.color = spriteColor;
            }
        }

        private void WalkModeGUI() {

        }

        private bool BetweenScreenPos(Rect area) {
            var pos = Input.mousePosition;
            pos.y = Screen.height - pos.y;
            var p = new Vector2(pos.x, pos.y);
            return Between(area, p);
        }

        private bool Between(Rect area, Vector3 pos) {
            return Between(area, new Vector2(pos.x, pos.z));
        }

        private bool Between(Rect area, Vector2 pos) {
            return (pos.x >= area.x && pos.x <= area.x + area.width && pos.y >= area.y && pos.y <= area.y + area.height);
        }

        private void ChangeColor(GameObject obj, Color color) {
            var render = obj.GetComponent<SpriteRenderer>();
            if (render != null) render.color = color;
        }
    }

    class UndoFunctions {
        public Action undoAction { get; private set; }

        public UndoFunctions(Action act) {
            undoAction = act;
        }
    }
}
