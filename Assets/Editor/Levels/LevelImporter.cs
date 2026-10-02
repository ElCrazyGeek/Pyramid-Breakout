using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Pyramid.Levels.Editor
{
    [ScriptedImporter(1, "level")]
    public sealed class LevelImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var output = ScriptableObject.CreateInstance<LevelDefinition>();
            output.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            try
            {
                string text = File.ReadAllText(ctx.assetPath);
                LevelCompiler.Compile(text, ctx.assetPath, output, path =>
                {
                    ctx.DependsOnSourceAsset(path);
                    var catalog = AssetDatabase.LoadAssetAtPath<ContentCatalog>(path);
                    if (catalog)
                        foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                            if (dependency != ctx.assetPath) ctx.DependsOnSourceAsset(dependency);
                    return catalog;
                });
                using (var hash = SHA256.Create()) output.sourceHash = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
            }
            catch (Exception error)
            {
                output.valid = false;
                output.diagnostics = error.Message;
                ctx.LogImportError(error.Message);
            }
            ctx.AddObjectToAsset("LevelDefinition", output);
            ctx.SetMainObject(output);
        }
    }
}
