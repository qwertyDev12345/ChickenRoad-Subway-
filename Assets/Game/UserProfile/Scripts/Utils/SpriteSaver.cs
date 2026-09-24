using System.IO;
using UnityEngine;

namespace UserProfile.Utils
{
    public static class SpriteSaver
    {
        public static void SaveSprite(string fileName, Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            
            byte[] spriteBytes = texture.EncodeToPNG();
            
            string path = Path.Combine(Application.persistentDataPath, fileName);
            
            File.WriteAllBytes(path, spriteBytes);
            
            Debug.Log($"Sprite saved at {path}");
        }

        public static Sprite LoadSprite(string fileName)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);

            if (File.Exists(path))
            {
                byte[] spriteBytes = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(spriteBytes);

                Sprite loadedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                Debug.Log("Sprite loaded");

                return loadedSprite;
            }

            return null;
        }
    }
}
