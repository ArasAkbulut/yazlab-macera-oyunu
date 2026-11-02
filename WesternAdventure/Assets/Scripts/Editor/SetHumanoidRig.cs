using UnityEditor;
using UnityEngine;

namespace WesternAdventure.EditorTools
{
    /// <summary>
    /// Mixamo'dan indirilen FBX karakterlerin rig tipini Humanoid yapar — bu sayede
    /// Unity'nin Avatar retargeting sistemiyle herhangi bir Mixamo animasyonu
    /// bu karaktere (ve köpek gibi başka Humanoid karakterlere) uygulanabilir.
    /// </summary>
    public static class SetHumanoidRig
    {
        [MenuItem("WesternAdventure/TunnelGuard'ı Humanoid Rig Yap")]
        public static void ApplyHumanoidRig()
        {
            const string path = "Assets/Characters/TunnelGuard.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"ModelImporter bulunamadı: {path}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();

            Debug.Log($"{path} artık Humanoid rig kullanıyor.");
        }
    }
}
