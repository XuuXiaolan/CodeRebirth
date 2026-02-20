using System.Collections.Generic;
using System.Text;
using Dawn;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

[CreateAssetMenu(fileName = "LogCommandDetails", menuName = "CodeRebirth/LogCommandDetails", order = 2)]
public class LogCommandDetails : ScriptableObject
{
    [field: SerializeField]
    public NamespacedKey<DawnTerminalCommandInfo> NamespacedKey { get; private set; }

    [field: SerializeField]
    public TerminalCommandBasicInformation CommandBasicInformation { get; private set; }

    [field: SerializeField]
    public string MainKeyword { get; private set; }

    [field: SerializeField]
    public List<string> InputKeywords { get; private set; } = new();
    [field: SerializeField]
    [field: TextArea(2, 10)]
    public List<string> ResultDisplayTexts { get; private set; } = new();

    [field: SerializeField]
    public List<NamespacedKey> SavedResultKeys { get; private set; } = new();
    [field: SerializeField]
    public List<string> TextToAppendAfterEmptyResult { get; private set; } = new();
    [field: SerializeField]
    [field: TextArea(2, 10)]
    public string EmptyResultDisplayText { get; private set; }
    [field: SerializeField]
    [field: TextArea(2, 10)]
    public string PostEmptyResultDisplayText { get; private set; }

    internal string ResultDisplayText(string userInput)
    {
        PersistentDataContainer? save = DawnLib.GetCurrentSave();
        if (save == null)
        {
            return EmptyResultDisplayText;
        }

        HashSet<NamespacedKey> savedKeys = save.GetOrCreateDefault<HashSet<NamespacedKey>>(Dawn.Utils.ExtraScanEvents._dataKey);
        for (int i = 0; i < InputKeywords.Count; i++)
        {
            if (!savedKeys.Contains(SavedResultKeys[i]))
                continue;

            if (userInput.Equals(InputKeywords[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return ResultDisplayTexts[i];
            }
        }

        StringBuilder stringBuilder = new(EmptyResultDisplayText);
        for (int i = 0; i < InputKeywords.Count; i++)
        {
            if (!savedKeys.Contains(SavedResultKeys[i]))
                continue;

            stringBuilder.Append(TextToAppendAfterEmptyResult[i]);
        }

        stringBuilder.Append(PostEmptyResultDisplayText);

        return stringBuilder.ToString();
    }
}