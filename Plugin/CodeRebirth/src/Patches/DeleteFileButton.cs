using CodeRebirth.src.Util;

namespace CodeRebirth.src.Patches;
public static class DeleteFileButtonPatch
{
    public static void Init()
    {
        On.DeleteFileButton.DeleteFile += DeleteFileButton_DeleteFile;
    }

    private static void DeleteFileButton_DeleteFile(On.DeleteFileButton.orig_DeleteFile orig, DeleteFileButton self)
    {
        orig(self);
        ES3Settings settings;
        if (CodeRebirthUtils.Instance != null)
        {
            settings = CodeRebirthUtils.Instance.SaveSettings;
        }
        else
        {
            settings = new ES3Settings($"CRLCSaveFile{self.fileToDelete + 1}", ES3.EncryptionType.None);
        }
        CodeRebirthUtils.ResetCodeRebirthData(settings);
    }
}