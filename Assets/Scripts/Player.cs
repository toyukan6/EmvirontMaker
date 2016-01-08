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
        public GameObject baseGround;
        /// <summary>
        /// 地面のテクスチャ
        /// </summary>
        private Texture2D groundTexture;
        /// <summary>
        /// 切断したあとの地面たち
        /// </summary>
        private List<GameObject> cutGrounds = new List<GameObject>();
        /// <summary>
        /// スプライトの色
        /// </summary>
        private Color spriteColor = Color.white;
        /// <summary>
        /// 点群表示用
        /// </summary>
        public GameObject PointCloudPrehab;
        /// <summary>
        /// 置くモーションの名前
        /// </summary>
        private string motionName = "";
        /// <summary>
        /// 角度
        /// </summary>
        private double degree = 0;
        /// <summary>
        /// 角度の入力を受けとる文字列
        /// </summary>
        private string degreeStr = "0";
        /// <summary>
        /// 障害物にするかどうか
        /// </summary>
        private bool failure = false;
        /// <summary>
        /// レイヤー
        /// </summary>
        private int layer = 3;
        /// <summary>
        /// レイヤーの入力を受けとる文字列
        /// </summary>
        private string layerStr = "3";
        /// <summary>
        /// 領域選択中かどうか
        /// </summary>
        private bool selectRange = false;
        /// <summary>
        /// 選択された点
        /// </summary>
        private List<Rect> range = new List<Rect>();
        /// <summary>
        /// 画像選択中かどうか
        /// </summary>
        private bool selectTexture = false;
        /// <summary>
        /// 選択された画像のインデックス
        /// </summary>
        private List<int> indexes = new List<int>();
        /// <summary>
        /// 角度を固定しているかどうか
        /// </summary>
        private bool fixing = false;
        /// <summary>
        /// ビルボード配置密度
        /// </summary>
        private float density = 0.5f;
        /// <summary>
        /// 領域拡張法の閾値
        /// </summary>
        private int extensionThreshold;
        /// <summary>
        /// extensionThresholdの値入力用文字列
        /// </summary>
        private string extensionThresholdStr = "11";
        /// <summary>
        /// 領域拡張法の開始値
        /// </summary>
        private int extensionStart;
        /// <summary>
        /// extensionStartの値入力用文字列
        /// </summary>
        private string extensionStartStr = "20";
        /// <summary>
        /// 領域選択の開始点
        /// </summary>
        private Vector3? startRange;
        /// <summary>
        /// 右クリックで何が起こるか
        /// </summary>
        private RightClickState rightClickState = RightClickState.DownTexture;
        /// <summary>
        /// 川生成モード
        /// </summary>
        private bool createRiver = false;
        #endregion

        #region 歩行モード
        /// <summary>
        /// 歩行制御
        /// </summary>
        private FirstPersonController controller;
        #endregion

        private void Awake() {
            Functions.Initialize();
            firstPersonCamera = this.GetComponent<Camera>();
            mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
            textures = new List<Texture2D>();
            string path = "Textures";
            var files = Directory.GetFiles(path);
            foreach (var f in files) {
                using (FileStream stream = new FileStream(f, FileMode.Open)) {
                    using (BinaryReader reader = new BinaryReader(stream)) {
                        long length = stream.Length;
                        var bytes = new List<byte>();
                        if (length > int.MaxValue) {
                            long remain = length;
                            while(remain > 0) {
                                int readLength = (int)Math.Min(int.MaxValue, remain);
                                foreach (var b in reader.ReadBytes(readLength)) {
                                    bytes.Add(b);
                                }
                                remain -= int.MaxValue;
                            }
                        } else {
                            bytes = reader.ReadBytes((int)length).ToList();
                        }
                        var tex = new Texture2D(2, 2);
                        tex.LoadImage(bytes.ToArray());
                        textures.Add(tex);
                    }
                }
            }
            var material = baseGround.GetComponent<MeshRenderer>().sharedMaterial;
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

        int waitFrame = 0;

        private Vector3 GetScreenPos(Vector3 basePos) {
            var pos = basePos - new Vector3(Screen.width, Screen.height, 0) / 2;
            var screenPos = Camera.main.ScreenToViewportPoint(pos);
            var worldPos = Camera.main.ScreenToWorldPoint(pos);
            screenPos.z = screenPos.y;
            var cameraY = mainCamera.transform.position.y;
            //var ratio = (double)Screen.width / Screen.height;
            var xratio = cameraY * 1.55f;
            var zratio = cameraY * 1.1625f;
            screenPos = new Vector3(screenPos.x * xratio, screenPos.y, screenPos.z * zratio);
            screenPos += worldPos;
            screenPos.y = 0;
            return screenPos;
        }

        private void EditModeUpdate() {
            var screenPos = GetScreenPos(Input.mousePosition);
            if (Input.GetMouseButtonDown(0)) {
                if (BetweenScreenPos(editArea)) {
                    EditClicked(screenPos);
                } else if (BetweenScreenPos(textureArea)) {
                    int index = (int)((Input.mousePosition.x - editRect.width - showRect.width - leftRect.width) / 100);
                    index += showTextureOffset;
                    if (selectTexture) {
                        indexes.Add(index);
                    } else if (!selectRange) {
                        var srender = Cursor.GetComponent<SpriteRenderer>();
                        if (index >= 0 && index < textures.Count) {
                            var texture = textures[index];
                            var size = new Vector2(texture.width, texture.height);
                            srender.sprite = Texture2DToSprite(texture);
                            ChangeSelectedObject(null);
                        }
                    }
                }
            } else if (!Input.GetMouseButton(0)) {
                waitFrame--;
                if (waitFrame < 0) {
                    ChangeSelectedObject(null);
                }
            }
            if (Input.GetMouseButton(0) && clickedPosition.HasValue) {
                var diff = screenPos - clickedPosition.Value;
                selectedObject.transform.position = firstPosition + diff;
            }
            if (Input.GetMouseButtonUp(0)) {
                if (startRange.HasValue) {
                    Vector3 endRange = screenPos;
                    float startX = Math.Min(startRange.Value.x, endRange.x);
                    float width = Math.Abs(startRange.Value.x - endRange.x);
                    float startZ = Math.Min(startRange.Value.z, endRange.z);
                    float height = Math.Abs(startRange.Value.z - endRange.z);
                    startRange = null;
                    range.Add(new Rect(startX, startZ, width, height));
                } else if (clickedPosition.HasValue) {
                    int selectedIndex = textureObjects.IndexOf(selectedObject);
                    var sprender = selectedObject.GetComponent<SpriteRenderer>();
                    var size = sprender.bounds.size;
                    var nowRect = textureRects[selectedIndex];
                    var rect = new Rect(selectedObject.transform.position.x - size.x / 2, selectedObject.transform.position.z - size.z / 2, size.x, size.z);
                    textureRects[selectedIndex] = rect;
                    undoAct = () => {
                        selectedObject.transform.position = firstPosition;
                        textureRects[selectedIndex] = nowRect;
                    };
                    clickedPosition = null;
                }
            }
            if (Input.GetMouseButtonDown(1)) {
                switch (rightClickState) {
                    case RightClickState.DownTexture:
                        if (selectedObject != null)
                            UpDownTexture(selectedObject, Direction.Down);
                        break;
                    case RightClickState.UpTexture:
                        if (selectedObject != null)
                            UpDownTexture(selectedObject, Direction.Up);
                        break;
                    default:
                        break;
                }
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
            //cursorRender.enabled = true;
        }

        private void EditClicked(Vector3 screenPos) {
            if (selectRange ^ createRiver) {
                startRange = screenPos;
            } else {
                var trect = textureRects.Find(t => Between(t, screenPos));
                if (trect == new Rect(0, 0, 0, 0)) {
                    ChangeSelectedObject(null);
                    var srender = Cursor.GetComponent<SpriteRenderer>();
                    if (srender.sprite != null) {
                        var obj = CreateSpriteObject(screenPos, srender.sprite);
                        srender.sprite = null;
                        undoAct = () => DeleteTexture(obj);
                    }
                } else {
                    int index = textureRects.IndexOf(trect);
                    int selectedIndex = textureObjects.IndexOf(selectedObject);
                    if (index != selectedIndex) {
                        ChangeSelectedObject(index);
                        clickedPosition = screenPos;
                        firstPosition = selectedObject.transform.position;
                    } else {
                        CreateBillBoard(selectedObject);
                    }
                }
            }
        }

        private List<GameObject> UpDownTexture(GameObject selectedObject, Direction direction) {
            var results = new List<GameObject>();
            var queue = new Queue<Vector2>();
            var srender = selectedObject.GetComponent<SpriteRenderer>();
            Texture2D texture = srender.sprite.texture;
            int[,] label = new int[texture.height, texture.width];
            for (int i = 0; i < texture.height; i++) {
                for (int j = 0; j < texture.width; j++) {
                    label[i, j] = 0;
                }
            }
            for (int j = 0; j < texture.width; j++) {
                for (int i = texture.height - 1; i >= 0; i--) {
                    Color d = texture.GetPixel(j, i);
                    if (d.Length() < extensionStart / 255f) {
                        queue.Enqueue(new Vector2(j, i));
                        break;
                    }
                }
            }
            while (queue.Count > 0) {
                var q = queue.Dequeue();
                //print(q);
                if (label[(int)q.y, (int)q.x] == 0) {
                    label[(int)q.y, (int)q.x] = 1;
                    Color d = texture.GetPixel((int)q.x, (int)q.y);
                    var candidacy = new List<Vector2>();
                    if (q.x > 0) candidacy.Add(new Vector2(q.x - 1, q.y));
                    if (q.x < texture.width - 1) candidacy.Add(new Vector2(q.x + 1, q.y));
                    if (q.y > 0) candidacy.Add(new Vector2(q.x, q.y - 1));
                    if (q.y < texture.height - 1) candidacy.Add(new Vector2(q.x, q.y + 1));
                    if (q.x > 0 && q.y > 0) candidacy.Add(new Vector2(q.x - 1, q.y - 1));
                    if (q.x > 0 && q.y < texture.height - 1) candidacy.Add(new Vector2(q.x - 1, q.y + 1));
                    if (q.x < texture.width - 1 && q.y > 0) candidacy.Add(new Vector2(q.x + 1, q.y - 1));
                    if (q.x < texture.width - 1 && q.y < texture.height - 1) candidacy.Add(new Vector2(q.x + 1, q.y + 1));
                    foreach (var c in candidacy) {
                        Color d2 = texture.GetPixel((int)c.x, (int)c.y);
                        var l = (d - d2).Length();
                        if (l < extensionThreshold / 255f) {
                            queue.Enqueue(c);
                        }
                    }
                }
            }
            int tmpLabel = 2;
            var remain = new List<Vector2>();
            for (int i = 0; i < label.GetLength(0); i++) {
                for (int j = 0; j < label.GetLength(1); j++) {
                    if (label[i, j] == 0) {
                        label[i, j] = tmpLabel++;
                        remain.Add(new Vector2(j, i));
                    }
                }
            }

            var equals = new Dictionary<int, int>();

            foreach (var r in remain) {
                int value = -1;
                if (r.x > 0 && label[(int)r.y, (int)r.x - 1] != 1)
                    value = label[(int)r.y, (int)r.x - 1];
                else if (r.y > 0 && label[(int)r.y - 1, (int)r.x] != 1)
                    value = label[(int)r.y - 1, (int)r.x];
                else if (r.x > 0 && r.y > 0 && label[(int)r.y - 1, (int)r.x - 1] != 1)
                    value = label[(int)r.y - 1, (int)r.x - 1];
                if (equals.ContainsKey(value)) {
                    value = equals[value];
                }
                if (value > 0) {
                    equals[label[(int)r.y, (int)r.x]] = value;
                }
            }
            var cluster = new Dictionary<int, int>();
            foreach (var v in equals.Values) {
                cluster[v] = 0;
            }
            for (int i = 0; i < label.GetLength(0); i++) {
                for (int j = 0; j < label.GetLength(1); j++) {
                    if (equals.ContainsKey(label[i, j])) {
                        label[i, j] = equals[label[i, j]];
                    }
                    if (cluster.ContainsKey(label[i, j]))
                        cluster[label[i, j]]++;
                    else
                        cluster[label[i, j]] = 1;
                }
            }
            Texture2D[] result = new Texture2D[2];
            for (int i = 0; i < result.Length; i++) {
                result[i] = new Texture2D(texture.width, texture.height);
            }
            var others = new List<int>();
            for (int i = 0; i < texture.height; i++) {
                for (int j = 0; j < texture.width; j++) {
                    var color = texture.GetPixel(j, i);
                    int setIndex = 0;
                    if (label[i, j] == 1 || (cluster.ContainsKey(label[i, j]) && cluster[label[i, j]] < 2000)) {
                        setIndex = 1;
                    } else if (label[i, j] != 1 && !others.Contains(label[i, j])) {
                        others.Add(label[i, j]);
                    }
                    for (int k = 0; k < result.Length; k++) {
                        if (k == setIndex) {
                            result[k].SetPixel(j, i, color);
                        } else {
                            result[k].SetPixel(j, i, new Color(1, 1, 1, 0));
                        }
                    }
                }
            }
            for (int i = 0; i < result.Length; i++) {
                result[i].Apply();
            }
            int firstLabel = label[texture.height - 1, 0];
            var heights = new List<int>();
            for (int j = 0; j < texture.width; j++) {
                for (int i = texture.height - 1; i >= 0; i--) {
                    if (label[i, j] != firstLabel) {
                        heights.Add(texture.height - 1 - i);
                        break;
                    }
                }
            }
            int minHeight = heights.Min();
            int maxHeight = heights.Max();
            Vector3 position = selectedObject.transform.position;
            Vector3 firstPosition = position;
            Vector3 firstAngle = selectedObject.transform.localEulerAngles;
            Sprite firstSprite = selectedObject.GetComponent<SpriteRenderer>().sprite;
            Destroy(selectedObject.gameObject);
            ChangeSelectedObject(null);
            //CreateSpriteObject(position, Texture2DToSprite(result[0]));
            GameObject obj0 = CreateSpriteObject(position, Texture2DToSprite(result[0]));
            bool beforeFix = fixing;
            fixing = true;
            GameObject bill = CreateBillBoard(obj0);
            fixing = beforeFix;
            float theta = bill.transform.localEulerAngles.y + 90;
            theta = (float)(theta * Math.PI / 180);
            var diff = new Vector2((texture.height / 2 - maxHeight) / 100f, 0).Rotate(-theta);
            bill.transform.position -= new Vector3(diff.x, bill.transform.position.y * 2, diff.y);
            bill.transform.localEulerAngles = new Vector3(0, firstAngle.y, 0);
            if (direction == Direction.Up) {
                position += new Vector3(0, minHeight / 100f, 0);
            } else if (direction == Direction.Down) {
                position -= new Vector3(0, minHeight / 100f, 0);
            }
            var obj1 = CreateSpriteObject(position, Texture2DToSprite(result[1]));
            obj1.GetComponent<SpriteRenderer>().sortingLayerID = Functions.SortingLayerUniqueIDs[1];
            obj1.transform.localEulerAngles = firstAngle;
            results.Add(bill);
            results.Add(obj1);
            undoAct = () => {
                Destroy(bill.gameObject);
                DeleteTexture(obj1.gameObject);
                CreateSpriteObject(firstPosition, firstSprite);
            };
            return results;
        }

        //private void DownGround() {
        //    Vector3 screenPos = GetScreenPos(Input.mousePosition);
        //    Vector3 diff = screenPos - this.transform.position;
        //    double theta = Math.Atan2(-diff.z, diff.x);
        //    Direction direction;
        //    if (theta >= -Math.PI * 3 / 4 && theta < -Math.PI / 4) {
        //        direction = Direction.Up;
        //    } else if (theta >= -Math.PI / 4 && theta < Math.PI / 4) {
        //        direction = Direction.Right;
        //    } else if (theta >= Math.PI / 4 && theta < Math.PI * 3 / 4) {
        //        direction = Direction.Down;
        //    } else {
        //        direction = Direction.Left;
        //    }
        //    print(direction);
        //    baseGround.SetActive(false);
        //    var newPositions = new Vector3[2];

        //}

        //private GameObject WithinGroundSearch(Vector3 position) {
        //    for (int i = 0; i < cutGrounds.Count; i++) {
        //        GameObject ground = cutGrounds[i];

        //    }
        //}

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

            if (Input.GetKey(KeyCode.J)) {
                mainCamera.transform.position += new Vector3(0, -0.1f, 0);
            } else if (Input.GetKey(KeyCode.K)) {
                mainCamera.transform.position += new Vector3(0, 0.1f, 0);
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
                waitFrame = 20;
                clickedPosition = null;
            } else {
                selectedObject = null;
                clickedPosition = null;
            }
        }

        private void Undo() {
            if (undoAct != null) {
                try {
                    undoAct();
                } catch { }
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
            var angles = obj.transform.localEulerAngles;
            obj.transform.localEulerAngles = new Vector3(angles.x, (float)degree, angles.z);
            if (failure) {
                var boxcollider = obj.AddComponent<BoxCollider>();
                boxcollider.enabled = failure;
                var boxsize = boxcollider.size;
                boxcollider.size = new Vector3(boxsize.x, boxsize.y, 10);
            }
            var osprite = obj.GetComponent<SpriteRenderer>();
            if (layer >= 0 && layer < Functions.SortingLayerUniqueIDs.Length) {
                osprite.sortingLayerID = Functions.SortingLayerUniqueIDs[layer];
            }
            return obj;
        }

        private GameObject CreateBillBoard(GameObject selectedObject) {
            if (selectedObject != null) {
                var selectedSprite = selectedObject.GetComponent<SpriteRenderer>().sprite;
                var selectedSize = selectedSprite.bounds.size;
                var selectPosition = selectedObject.transform.position;
                var newPosition = new Vector3(selectPosition.x, selectedSize.y / 2f, selectPosition.z);
                ChangeColor(selectedObject, Color.white);
                var obj = Instantiate(selectedObject, newPosition, Quaternion.identity) as GameObject;
                var bill = obj.GetComponent<BillBoard>();
                bill.SetPlayer(this.gameObject);
                if (fixing) {
                    bill.enabled = false;
                    var angles = obj.transform.localEulerAngles;
                    obj.transform.localEulerAngles = new Vector3(angles.x, (float)degree, angles.z);
                }
                var boxcollider = obj.GetComponent<BoxCollider>();
                if (boxcollider != null) {
                    boxcollider.enabled = failure;
                    var colSize = boxcollider.size;
                    boxcollider.size = new Vector3(colSize.x, colSize.y, 0.1f);
                }
                var position = selectedObject.transform.position;
                var angle = selectedObject.transform.localEulerAngles;
                Action act = () => {
                    Destroy(obj.gameObject);
                    var gameobj = CreateSpriteObject(position, selectedSprite);
                    gameobj.transform.localEulerAngles = angle;
                };
                undoAct = act;
                DeleteTexture(selectedObject);
                return obj;
            } else return null;
        }

        private Sprite Texture2DToSprite(Texture2D texture) {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        int beforeWidth = 0;
        int beforeHeight = 0;
        private void WalkModeUpdate() {
            var pos = this.transform.position;
            Back.transform.position = pos;
            //if (groundTexture != null) {
            //    var tmp = baseGround.transform.localScale;
            //    var scale = new Vector2(tmp.x, tmp.z);
            //    var groundWidth = scale.x * 10;
            //    var groundHeight = scale.y * 10;
            //    var dotW = groundWidth / groundTexture.width;
            //    var dotH = groundHeight / groundTexture.height;
            //    var start = baseGround.transform.position + new Vector3(-groundWidth / 2, 0, -groundHeight / 2);
            //    var playerPos = this.transform.position;
            //    var diff = start - playerPos;
            //    int width = (int)(Math.Abs(diff.x) / dotW);
            //    int height = (int)(Math.Abs(diff.z) / dotH);
            //    print($"ground:{groundTexture.width} width:{width} height:{height} color:{groundTexture.GetPixel(width, height).ToString()}");
            //    if (groundTexture.GetPixel(width, height) == new Color(1, 1, 1, 0)) {
            //        //var x = width;
            //        //do {
            //        //    x++;
            //        //} while (x < groundTexture.width && groundTexture.GetPixel(x, height) != new Color(1, 1, 1, 0));
            //        //if (x >= groundTexture.width) {
            //        //    x = width;
            //        //    do {
            //        //        x--;
            //        //    } while (x >= 0 && groundTexture.GetPixel(x, height) != new Color(1, 1, 1, 0));
            //        //    if (x < 0) {
            //        //        x = width;
            //        //    }
            //        //}
            //        //var newPos = start + new Vector3(x * dotW, 0, height * dotH);
            //        //print(newPos);
            //        //controller.transform.position = newPos;
            //        print("IsOut");
            //    }
            //}
            //Ground.transform.position = new Vector3(pos.x, 0, pos.z);
        }

        private void OnGUI() {
            editButton = GUI.Button(new Rect(0, 0, 100, 100), (editMode ? "歩く" : "編集する"));
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
            showButton = GUI.Button(showRect, (showTextures ? "隠す" : "表示する"));
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
            var back = GUI.Button(new Rect(0, 500, 50, 50), "背景");
            if (back) {
                var material = Back.GetComponent<MeshRenderer>().sharedMaterial;
                var srender = Cursor.GetComponent<SpriteRenderer>();
                if (srender.sprite != null) {
                    material.mainTexture = srender.sprite.texture;
                    Back.gameObject.SetActive(true);
                }
            }
            var ground = GUI.Button(new Rect(50, 500, 50, 50), "地面");
            if (ground) {
                if (cutGrounds.Count > 0) {
                    cutGrounds.ForEach(c => Destroy(c.gameObject));
                    cutGrounds.Clear();
                    baseGround.SetActive(true);
                }
                var meshRenderer = baseGround.GetComponent<MeshRenderer>();
                meshRenderer.enabled = false;
                Material material = meshRenderer.sharedMaterial;
                var srender = Cursor.GetComponent<SpriteRenderer>();
                if (srender.sprite != null) {
                    material.color = spriteColor;
                    var texture = srender.sprite.texture;
                    var list = new List<Vector2>();
                    var scale = baseGround.transform.localScale;
                    if (groundTexture == null) {
                        if (texture.height > texture.width) {
                            var ratio = (float)texture.height / texture.width;
                            baseGround.transform.localScale = new Vector3(scale.x, scale.y, scale.z * ratio);
                        } else {
                            var ratio = (float)texture.width / texture.height;
                            baseGround.transform.localScale = new Vector3(scale.x * ratio, scale.y, scale.z);
                        }
                        material.mainTexture = texture;
                    }
                    groundTexture = texture;
                    var obj = CreateSpriteObject(Vector3.zero, Texture2DToSprite(texture));
                    textureRects[textureRects.Count - 1] = new Rect(0, 0, 1, 1);
                    obj.transform.localEulerAngles = new Vector3(90, 0, 0);
                    float x = 3.65f * Math.Max(2750f / texture.width, 11470f / texture.height);
                    //print(ratio2);
                    obj.transform.localScale = new Vector3(x, x, 1);
                    obj.GetComponent<SpriteRenderer>().sortingLayerID = Functions.SortingLayerUniqueIDs[layer];
                }
            }
            string riverText = "川を作る";
            if (createRiver) {
                riverText = "形の指定を\n終了する";
            }
            if (GUI.Button(new Rect(0, 550, 100, 50), riverText) && !selectRange) {
                if (createRiver) {
                    StartCoroutine(CreateRiver());
                } else {
                    createRiver = true;
                    range.Clear();
                }
            }
            string errMessage = "";
            GUI.TextField(new Rect(100, 500, 100, 20), "レイヤー");
            layerStr = GUI.TextField(new Rect(100, 520, 100, 80), layerStr);
            if (!int.TryParse(layerStr, out layer)) {
                errMessage += "自然数を入力してください:layer";
            } else if (layer < 0 || layer >= Functions.SortingLayerUniqueIDs.Length) {
                errMessage += $"レイヤーは{Functions.SortingLayerUniqueIDs.Length}までにしてください";
            }
            var motion = GUI.Button(new Rect(200, 500, 100, 50), "モーションを\n置く");
            GUI.TextField(new Rect(300, 500, 100, 20), "モーション名");
            motionName = GUI.TextField(new Rect(300, 520, 100, 80), motionName);
            if (motion) {
                if (Directory.Exists($"polygons/{motionName}")) {
                    var pos = GetScreenPos(mainCamera.transform.position);
                    pos.y = 1.357328f;
                    var pcObj = Instantiate(PointCloudPrehab, pos, Quaternion.identity) as GameObject;
                    var pc = pcObj.GetComponent<PointCloud>();
                    if (pc != null) {
                        pc.DirName = motionName;
                    }
                    List<Vector2> positions = range.Select(r => r.position).ToList();
                    Vector2? start = null, end = null;
                    if (positions.Count > 0) {
                        start = positions[0];
                        if (positions.Count > 1) {
                            end = positions[1];
                        }
                    }
                    pc.Initialize(start, end);
                } else {
                    errMessage += "そのようなディレクトリは存在しません:モーション";
                }
            }
            string[] rightClickTexts = new[] { "画像を沈める", "画像を上げる" };
            if (GUI.Button(new Rect(200, 550, 100, 50), rightClickTexts[(int)rightClickState])) {
                rightClickState = (RightClickState)(((int)rightClickState + 1) % Enum.GetNames(typeof(RightClickState)).Length);
            }
            if (GUI.Button(new Rect(400, 500, 100, 20), fixing ? "角度固定中" : "角度自在中")) {
                fixing = !fixing;
            }
            GUI.TextField(new Rect(400, 520, 100, 20), "角度");
            var before = degreeStr;
            degreeStr = GUI.TextField(new Rect(400, 540, 100, 60), degreeStr);
            if (before != degreeStr) {
                if (double.TryParse(degreeStr, out degree)) {
                    var csrender = Cursor.GetComponent<SpriteRenderer>();
                    var angle = csrender?.transform.localEulerAngles;
                    if (angle.HasValue) {
                        csrender.transform.localEulerAngles = new Vector3(angle.Value.x, (float)degree, angle.Value.z);
                    }
                } else {
                    errMessage += "実数を入力してください:角度";
                }
            }
            if (GUI.Button(new Rect(500, 500, 100, 50), failure ? "障害物" : "障害物\nでない")) {
                failure = !failure;
            }
            string selectRangeShow = "";
            if (selectTexture) {
                selectRangeShow = "画像選択を\n終える";
            } else if (selectRange) {
                selectRangeShow = "線分選択を\n終える";
            } else {
                selectRangeShow = "領域選択開始";
            }
            if (GUI.Button(new Rect(500, 550, 100, 50), selectRangeShow) && !createRiver) {
                if (selectTexture) {
                    StartCoroutine(RandomEstablish());
                } else if (selectRange) {
                    selectTexture = true;
                    showTextures = true;
                } else {
                    selectRange = true;
                    var csrender = Cursor.GetComponent<SpriteRenderer>();
                    csrender.sprite = null;
                    range.Clear();
                    indexes.Clear();
                }
            }
            GUI.TextArea(new Rect(610, 500, 20, 20), "高");
            GUI.TextArea(new Rect(610, 580, 20, 20), "低");
            GUI.TextArea(new Rect(610, 530, 20, 40), "密\n度");
            density = GUI.VerticalSlider(new Rect(600, 500, 10, 100), density, 1, 0);

            GUI.TextArea(new Rect(630, 500, 85, 20), "開始値");
            extensionStartStr = GUI.TextField(new Rect(630, 520, 85, 80), extensionStartStr);
            if (!int.TryParse(extensionStartStr, out extensionStart)) {
                errMessage += "整数を入力してください:開始値";
            }
            GUI.TextArea(new Rect(715, 500, 85, 20), "閾値");
            extensionThresholdStr = GUI.TextField(new Rect(715, 520, 85, 80), extensionThresholdStr);
            if (!int.TryParse(extensionThresholdStr, out extensionThreshold)) {
                errMessage += "整数を入力してください:閾値";
            }
            //print(errMessage);
        }

        private IEnumerator<GameObject> CreateRiver() {
            createRiver = false;
            var texture = textures[4];
            var objects = new List<GameObject>();
            double beforeDegree = degree;
            var acts = new List<Action>();
            for (int i = 0; i < range.Count - 1; i++) {
                Vector2 start = range[i].position;
                Vector2 end = range[i + 1].position;
                Vector2 diff = (end - start).normalized;
                double theta = Math.Atan2(-diff.y, diff.x);
                degree = theta * 180 / Math.PI;
                Vector2 createPosition = start + diff * texture.width / 200;
                var firstPosition = new Vector3(createPosition.x, 0, createPosition.y);
                double angle = GetAngle(end - createPosition, end - start);
                double mag = (end - createPosition).sqrMagnitude;
                double threshold = 0.01;
                var position = new Vector3(createPosition.x, 0, createPosition.y);
                GameObject obj = CreateSpriteObject(position, Texture2DToSprite(texture));
                List<GameObject> results = UpDownTexture(obj, Direction.Down);
                foreach (var r in results) {
                    objects.Add(r);
                }
                createPosition += diff * texture.width / 100;
                while (mag < threshold * threshold || angle >= 0 && angle < Math.PI / 2) {
                    position = new Vector3(createPosition.x, 0, createPosition.y);
                    Vector3 offset = position - firstPosition;
                    foreach (var r in results) {
                        var clone = Instantiate(r, r.transform.position + offset, Quaternion.identity) as GameObject;
                        clone.transform.localEulerAngles = r.transform.localEulerAngles;
                        objects.Add(clone);
                    }
                    createPosition += diff * texture.width / 100;
                    angle = GetAngle(end - createPosition, end - start);
                    yield return null;
                }
            }
            undoAct = () => {
                objects.ForEach(o => Destroy(o.gameObject));
            };
            degree = beforeDegree;
        }

        private double GetAngle(Vector2 vec1, Vector2 vec2) {
            double cos = (vec1.x * vec2.x + vec1.y * vec2.y) / (vec1.magnitude * vec2.magnitude);
            if (cos > 1) cos = 1;
            else if (cos < -1) cos = -1;
            return Math.Acos(cos);
        }

        private IEnumerator<GameObject> RandomEstablish() {
            selectRange = false;
            selectTexture = false;
            var length = 0f;
            var area = 0.0;
            for (int i = 0; i < range.Count - 1; i++) {
                length += (range[i].position - range[i + 1].position).magnitude;
                area += range[i].Area();
            }
            if (range.Count > 0) {
                area += range.Last().Area();
            }
            if (length > area) {
                int number = (int)(length * density);
                var list = new List<GameObject>();
                if (indexes.Count > 0) {
                    for (int i = 0; i < number; i++) {
                        var sindex = Functions.GetRandomInt(range.Count - 1);
                        var start = range[sindex];
                        var end = range[sindex + 1];
                        var diff = end.position - start.position;
                        if (fixing) {
                            var atan = Math.Atan2(-diff.y, diff.x);
                            degree = atan * 180 / Math.PI + 90;
                        }
                        float rand = (float)Functions.GetRandomDouble();
                        var pos = start.position + diff * rand;
                        var index = indexes[Functions.GetRandomInt(indexes.Count)];
                        var obj = CreateSpriteObject(pos, Texture2DToSprite(textures[index]));
                        var result = CreateBillBoard(obj);
                        list.Add(result);
                        yield return result;
                    }
                    undoAct = () => {
                        list.ForEach(l => Destroy(l.gameObject));
                        list.Clear();
                    };
                }
            } else {
                var list = new List<GameObject>();
                print(range.Count);
                for (int i = 0; i < range.Count; i++) {
                    var target = range[i];
                    int number = (int)(target.Area() * density);
                    print(number);
                    if (indexes.Count > 0) {
                        for (int j = 0; j < number; j++) {
                            var sindex = Functions.GetRandomInt(range.Count - 1);
                            float randX = (float)Functions.GetRandomDouble(target.x, target.x + target.width);
                            float randY = (float)Functions.GetRandomDouble(target.y, target.y + target.height);
                            var pos = new Vector3(randX, 0, randY);
                            var index = indexes[Functions.GetRandomInt(indexes.Count)];
                            var obj = CreateSpriteObject(pos, Texture2DToSprite(textures[index]));
                            var result = CreateBillBoard(obj);
                            list.Add(result);
                            yield return result;
                        }
                    }
                }
                undoAct = () => {
                    list.ForEach(l => Destroy(l.gameObject));
                    list.Clear();
                };
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

    enum RightClickState {
        DownTexture,
        UpTexture,
    }

    enum Direction {
        Up,
        Right,
        Down,
        Left
    }

    static class Vector2Extension {
        public static Vector2 Rotate(this Vector2 vec, double theta) {
            float x = (float)(vec.x * Math.Cos(theta) - vec.y * Math.Sin(theta));
            float y = (float)(vec.x * Math.Sin(theta) + vec.y * Math.Cos(theta));
            return new Vector2(x, y);
        }
    }

    static class RectExtension {
        public static double Area(this Rect rect) {
            return rect.width * rect.height;
        }
    }

    class UndoFunctions {
        public Action undoAction { get; private set; }

        public UndoFunctions(Action act) {
            undoAction = act;
        }
    }
}
