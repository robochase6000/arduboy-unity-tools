using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu]
public class CharacterDatabase : ScriptableObject
{

    [Serializable]
    public class CharacterStateData
    {
        public string StateName = "Idle";
        public int ImageWidth = 32;
        public int ImageHeight = 32;
        public int Frames = 2;
    }

    [Serializable]
    public class CharacterData
    {
        public string Name;
        public CharacterStateData[] States;
    }

    public List<string> CharacterNames;
    public List<CharacterStateData> CharacterStates = new();

    public int ExportSpriteWidth = 16;
    public int ExportSpriteHeight = 32;
    public bool UseImageSizeForSpriteDimensions = false;
    public List<Texture2D> TexturesToGenerate = new();
}


#if UNITY_EDITOR
[CustomEditor(typeof(CharacterDatabase))]
public class CharacterDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Create Characters"))
        {
            var database = (CharacterDatabase)target;
            foreach (var characterName in database.CharacterNames)
            {
                foreach (var state in database.CharacterStates)
                {
                    var imageWidth = state.ImageWidth;
                    var imageHeight = state.ImageHeight * state.Frames;
                    var texture = new Texture2D(imageWidth, imageHeight);
                    for (int x = 0; x < imageWidth; x++)
                    {
                        for (int y = 0; y < imageHeight; y++)
                        {
                            texture.SetPixel(x, y, Color.black);
                        }
                    }
                    
                    for (int i = 0; i < state.ImageWidth * state.ImageHeight / 8; i++)
                    {
                        var x = UnityEngine.Random.Range(0, imageWidth);
                        var y = UnityEngine.Random.Range(0, imageHeight);
                        texture.SetPixel(x, y, Color.white);
                    }
                    
                    byte[] bytes = texture.EncodeToPNG();
                    var dirPath = Application.dataPath + "/ExportedAssets/";
                    if(!Directory.Exists(dirPath)) {
                        Directory.CreateDirectory(dirPath);
                    }

                    var baseFilename = $"character_{characterName}_{state.StateName}";
                    //var fileName = $"{baseFilename}_{state.ImageWidth}x{state.ImageHeight}";
                    var fileName = $"{baseFilename}";
                    File.WriteAllBytes(dirPath + fileName + ".png", bytes);

                    int spriteWidth = database.UseImageSizeForSpriteDimensions
                        ? imageWidth
                        : database.ExportSpriteWidth;
                    int spriteHeight = database.UseImageSizeForSpriteDimensions
                        ? imageHeight
                        : database.ExportSpriteHeight;

                    var spriteData = GenerateSpriteData(baseFilename, texture, spriteWidth, spriteHeight);
                    Debug.Log(spriteData);
                }
            }
            
            AssetDatabase.Refresh();
        }

        if (GUILayout.Button("Generate Sprite Datas"))
        {
            var generateIndividualSpritesOnly = true;
            
            var stringBuilder = new StringBuilder();

            if (!generateIndividualSpritesOnly)
            {
                stringBuilder.AppendLine("#ifndef SPRITES_H");
                stringBuilder.AppendLine("#define SPRITES_H");
                stringBuilder.AppendLine("#define USE_ASSETS");
                stringBuilder.AppendLine("");
                stringBuilder.AppendLine("#include <Arduboy2.h>");
            }
            
            {
                var database = (CharacterDatabase)target;
                foreach (Texture2D texture in database.TexturesToGenerate)
                {
                    var activeTexture = texture;
                    if (!activeTexture.isReadable)
                    {
                        var origTexPath = AssetDatabase.GetAssetPath(activeTexture);
                        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(origTexPath);
                        textureImporter.isReadable = true;
                        textureImporter.SaveAndReimport();
                        activeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(origTexPath);
                    }
                    Debug.Log($"texture.name {activeTexture.name}");
                    
                    int spriteWidth = database.UseImageSizeForSpriteDimensions
                        ? texture.width
                        : database.ExportSpriteWidth;
                    int spriteHeight = database.UseImageSizeForSpriteDimensions
                        ? texture.height
                        : database.ExportSpriteHeight;
                    
                    stringBuilder.Append(GenerateSpriteData(activeTexture.name, activeTexture, spriteWidth, spriteHeight));
                    stringBuilder.AppendLine(" ");
                    stringBuilder.AppendLine(" ");
                    stringBuilder.AppendLine(" ");
                }
            }
            
            if (!generateIndividualSpritesOnly)
            {
                stringBuilder.AppendLine("#endif // SPRITES_H");    
            }

            var dirPath = Application.dataPath + "/ExportedAssets/";
            if(!Directory.Exists(dirPath)) {
                Directory.CreateDirectory(dirPath);
            }

            var filename = $"spriteData.txt";
            var filePath = dirPath + filename;
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(stringBuilder.ToString());
            }

            AssetDatabase.Refresh();
        }
    }

    private string GenerateSpriteData(string imageName, Texture2D texture, int spriteWidth, int spriteHeight)
    {
        // Generate the sprite string
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"const unsigned char PROGMEM {imageName}[] =");
        stringBuilder.AppendLine("{");
        stringBuilder.AppendLine("#ifdef USE_ASSETS");
        stringBuilder.AppendLine("// width, height");
        stringBuilder.AppendLine($"{spriteWidth}, {spriteHeight},");
        stringBuilder.AppendLine("// TILE 00");
        {
            var pageCount = (int)Mathf.Ceil((float)texture.height / 8f);
            var columnCount = texture.width;
            var currentByte = 0;
            var rowCounter = 0;
            var tileCounter = 0;
        
            // Read the sprite page-by-page
            for(var page = 0; page < pageCount; page++) {

                // Read the page column-by-column
                for(var column = 0; column < columnCount; column++) {

                    // Read the column into a byte
                    var spriteByte = 0;
                    for(var yPixel = 0; yPixel < 8; yPixel++) {

                        // If the color of the pixel is not black, count it as white
                        var pixelColor = texture.GetPixel(column, texture.height - (page * 8 + yPixel));
                        if(pixelColor.r > 0.2f || pixelColor.g > 0.2f || pixelColor.b > 0.2f) {
                            spriteByte |= (1 << yPixel);
                        }
                    }
                
                    // Print the column in hex notation, add a comma for formatting
                    var digitStr = spriteByte.ToString("X2").ToLower();
                    if(digitStr.Length == 1) {
                        digitStr = "0" + digitStr;
                    }
                    stringBuilder.Append("0x" + digitStr + ", ");
                    if(currentByte % texture.width == texture.width-1){
                        stringBuilder.Append("\n");
                        rowCounter++;
                        if(rowCounter == texture.width / 8 && tileCounter < texture.height / texture.width - 1){
                            tileCounter++;
                            var tileNumber = tileCounter < 10 ? "0"+tileCounter : tileCounter.ToString();
                            stringBuilder.AppendLine("// TILE " + tileNumber);
                            rowCounter = 0;
                        }
                    }
                    currentByte++;
                }
            }
        }
        stringBuilder.AppendLine("#endif");
        stringBuilder.AppendLine("};");
        return stringBuilder.ToString();
    }
}
#endif