using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Builds board materials for legacy palettes.</summary>
public static class PremiumCosmeticsArt
{
    private const string Root = "Assets/_Game/Development/Data/Cosmetics/Premium";
    public static void Apply(CosmeticsCatalog catalog)
    {
        Directory.CreateDirectory(Root); AssetDatabase.Refresh();
        foreach(var theme in catalog.Themes.Where(x=>!x.IsOriginal))
        {
            switch(theme.Id)
            {
                case "midnight": Palette(theme,"Celestial","222B62","56629C","25305B","B6CCDF","253C60","C9D7FF","AABEFF"); break;
                case "ocean": Palette(theme,"Lagoon","14556B","6AAEB5","153B50","B7D9DC","2C5967","8EEAE5","A4D2F0"); break;
                case "sunset": Palette(theme,"Coral Sunset","81466C","E7A381","4B2B4B","E5CBC7","744D5D","FFE2B2","FFBDCF"); break;
                case "forest": Palette(theme,"Jade Garden","28564E","91B39A","183D35","C5D9CB","365C51","C6EBAB","A9E5D0"); break;
                default: Palette(theme,"Opal Aurora","4B4385","A6B5D9","302C55","D2D2E6","534F7B","DFCDFF","ABEEE6"); break;
            }
            Boards(catalog,theme); EditorUtility.SetDirty(theme);
        }
        EditorUtility.SetDirty(catalog);
    }
    private static void Palette(ThemeData t,string name,string top,string bottom,string surface,string face,string frame,string accent,string secondary)
    {
        t.BackgroundTop=Hex(top);t.BackgroundBottom=Hex(bottom);t.Surface=Hex(surface);t.BoardFace=Hex(face);t.BoardFrame=Hex(frame);
        t.BoardGap=Color.Lerp(t.BoardFrame,Color.black,.62f);t.Accent=Hex(accent);t.Secondary=Hex(secondary);
        var data=new SerializedObject(t);data.FindProperty("themeName").stringValue=name;data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Boards(CosmeticsCatalog c,ThemeData theme)
    {
        var references=c.PreviewBoard.GetComponentInChildren<Renderer>().sharedMaterials;
        theme.BoardSurfaceMaterials=new Material[references.Length];
        for(int i=0;i<references.Length;i++)
        {
            var source=references[i];string path=Root+"/"+theme.Id+"_Board_"+i;
            var material=AssetDatabase.LoadAssetAtPath<Material>(path+".mat");
            if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,path+".mat");} else EditorUtility.CopySerialized(source,material);
            if(source.HasProperty("_BaseMap") && source.GetTexture("_BaseMap")!=null)
            {
                bool face=source.name.Contains("GridFace"),gap=source.name.Contains("GridGaps");
                var texture=Grade(source.GetTexture("_BaseMap"),face?theme.BoardFace:gap?theme.BoardGap:theme.BoardFrame,path+".png");
                material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);
                material.SetTexture("_EmissionMap",texture);material.SetColor("_EmissionColor",Color.white*(face?.08f:.025f));material.EnableKeyword("_EMISSION");
                material.SetFloat("_Smoothness",face?.4f:.6f);
            }
            else
            {
                if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",theme.Accent);
                if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",theme.Accent);
                if(material.HasProperty("_EmissionIntensity"))material.SetFloat("_EmissionIntensity",.28f);
            }
            material.name=theme.Id+" Board "+i;theme.BoardSurfaceMaterials[i]=material;EditorUtility.SetDirty(material);
        }
    }
    private static Texture2D Grade(Texture source,Color tint,string path)
    {
        var target=RenderTexture.GetTemporary(512,512,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;
        var readable=new Texture2D(512,512,TextureFormat.RGBA32,false);
        try
        {
            Graphics.Blit(source,target);RenderTexture.active=target;readable.ReadPixels(new Rect(0,0,512,512),0,0);readable.Apply();
            var pixels=readable.GetPixels();float maximum=Mathf.Max(.001f,pixels.Max(x=>x.grayscale));
            for(int i=0;i<pixels.Length;i++){float detail=Mathf.Clamp01(pixels[i].grayscale/maximum);Color color=tint*Mathf.Lerp(.42f,1.08f,Mathf.Sqrt(detail));color.a=pixels[i].a;pixels[i]=color;}
            readable.SetPixels(pixels);readable.Apply();File.WriteAllBytes(path,readable.EncodeToPNG());
        }
        finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(readable);}
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
    private static T Asset<T>(string path) where T:ScriptableObject
    {var result=AssetDatabase.LoadAssetAtPath<T>(path);if(result==null){result=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(result,path);}return result;}
}
