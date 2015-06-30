using UnityEngine;
using System.Collections;
using OpenCvSharp;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace EnvironmentMaker {
    public class Segment : MonoBehaviour {

        private Vector2? startPos;
        private GameObject imageObject;
        private Image image;
        private Texture2D firstImage;
        private int[,] label;
        private List<bool> labelButtons;

        // Use this for initialization
        void Start() {
            imageObject = GameObject.Find("Image");
            var tex = Resources.Load<Texture2D>("texture");
            firstImage = new Texture2D(tex.width, tex.height);
            firstImage.SetPixels(tex.GetPixels());
            firstImage.Apply();
            label = new int[firstImage.height, firstImage.width];
            for (int i = 0; i < label.GetLength(0); i++) {
                for (int j = 0; j < label.GetLength(1); j++) {
                    label[i, j] = 0;
                }
            }
            labelButtons = new List<bool>();
            labelButtons.Add(false);
            image = imageObject.GetComponent<Image>();
            ChangeImagePosX();
            ChangeImagePosY();
            var texture = (Texture2D)image.mainTexture;
            imageObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, texture.width);
            imageObject.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, texture.height);
            var colors = texture.GetPixels();
            var newTexture = new Texture2D(texture.width, texture.height);
            newTexture.SetPixels(colors);
            newTexture.Apply();
            var textureCenter = new Vector2(texture.width, texture.height) * 0.5f;
            image.sprite = Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), textureCenter);
        }

        public void ChangeImagePosX() {
            Slider slider = GameObject.Find("ImagePosX").GetComponent<Slider>();
            imageObject.transform.position = new Vector3(slider.value, imageObject.transform.position.y);
        }

        public void ChangeImagePosY() {
            Slider slider = GameObject.Find("ImagePosY").GetComponent<Slider>();
            imageObject.transform.position = new Vector3(imageObject.transform.position.x, slider.value);
        }

        // Update is called once per frame
        void Update() {
            var colors = firstImage.GetPixels();
            var newTexture = new Texture2D(firstImage.width, firstImage.height);
            newTexture.SetPixels(colors);
            var textureCenter = new Vector2(firstImage.width, firstImage.height) * 0.5f;
            if (Input.GetMouseButton(0)) {
                if (Input.GetMouseButtonDown(0)) {
                    var pos = GetTexturePos(firstImage);
                    if (label[(int)pos.y, (int)pos.x] > 0) {
                        labelButtons[label[(int)pos.y, (int)pos.x]] = true;
                    } else {
                        startPos = pos;
                    }
                }
                ChangeColor(newTexture);
            } else if (Input.GetMouseButtonUp(0)) {
                ChangeColor(firstImage);
                newTexture.SetPixels(colors);
                startPos = null;
                for (int i = 0; i < labelButtons.Count; i++) {
                    labelButtons[i] = false;
                }
            }
            ChoosedLabel(newTexture);
            newTexture.Apply();
            DestroyImmediate(image.sprite);
            image.sprite = Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), textureCenter);
        }

        private void ChoosedLabel(Texture2D texture) {
            for (int i = 0; i < label.GetLength(0); i++) {
                for (int j = 0; j < label.GetLength(1); j++) {
                    if (labelButtons[label[i, j]]) {
                        var c = texture.GetPixel(j, i);
                        texture.SetPixel(j, i, new Color(c.r * 2, c.g, c.b, c.a));
                    }
                }
            }
        }

        private Vector2 GetTexturePos(Texture2D texture) {
            var m_pos = Input.mousePosition;
            var i_pos = image.transform.position;
            return new Vector2(m_pos.x - i_pos.x + texture.width * 0.5f, m_pos.y - i_pos.y + texture.height * 0.5f);
        }

        public void OnButtonClick() {
            
            Application.LoadLevel("Second");
        }

        private void ChangeColor(Texture2D texture) {
            var pos = GetTexturePos(texture);
            if (pos.x >= 0 && pos.y >= 0 && pos.x < texture.width && pos.y < texture.height && startPos.HasValue) {
                var minX = Mathf.Min(pos.x, startPos.Value.x);
                var minY = Mathf.Min(pos.y, startPos.Value.y);
                var maxX = Mathf.Max(pos.x, startPos.Value.x);
                var maxY = Mathf.Max(pos.y, startPos.Value.y);
                var sizeX = maxX - minX;
                var sizeY = maxY - minY;
                for (int i = 0; i < sizeX; i++) {
                    texture.SetPixel((int)minX + i, (int)minY, Color.white);
                    texture.SetPixel((int)minX + i, (int)maxY, Color.white);
                }
                for (int i = 0; i < sizeY; i++) {
                    texture.SetPixel((int)minX, (int)minY + i, Color.white);
                    texture.SetPixel((int)maxX, (int)minY + i, Color.white);
                }
                if (Input.GetMouseButtonUp(0)) {
                    var l = Maximize(label) + 1;
                    for (int i = 0; i < sizeX; i++) {
                        for (int j = 0; j < sizeY; j++) {
                            label[(int)minY + j, (int)minX + i] = l;
                        }
                    }
                    labelButtons.Add(false);
                }
            }
        }

        private void OnGUI() {
            for (int i = 0; i < labelButtons.Count; i++) {
                labelButtons[i] = GUI.Button(new Rect(0, 100 * i, 100, 100), "ラベル" + i);
            }
        }

        private T Maximize<T>(T[,] array) where T : IComparable {
            T max = array[0, 0];
            for (int i = 0; i < array.GetLength(0); i++) {
                for (int j = 0; j < array.GetLength(1); j++) {
                    if (max.CompareTo(array[i, j]) < 0) {
                        max = array[i, j];
                    }
                }
            }

            return max;
        }
    }
}
