using System;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

[Serializable]
public class ScanNodeData
{
    public bool editMinRange;
    public int minRange;
    public bool editMaxRange;
    public int maxRange;
    public bool editHeader;
    public string headerText;
    public bool editSubText;
    public string subText;
    public bool editNodeType;
    public int nodeType;
}

public class EditScanNodeProperties : MonoBehaviour
{
    [SerializeField]
    internal ScanNodeProperties? _scanNodeProperties = null;

    [SerializeField]
    private ScanNodeData _editedScanNodeData = null!;

    public void EditScanNode()
    {
        if (_scanNodeProperties == null)
            return;

        if (_editedScanNodeData.editMinRange) _scanNodeProperties.minRange = _editedScanNodeData.minRange;
        if (_editedScanNodeData.editMaxRange) _scanNodeProperties.maxRange = _editedScanNodeData.maxRange;
        if (_editedScanNodeData.editHeader) _scanNodeProperties.headerText = _editedScanNodeData.headerText;
        if (_editedScanNodeData.editSubText) _scanNodeProperties.subText = _editedScanNodeData.subText;
        if (_editedScanNodeData.editNodeType) _scanNodeProperties.nodeType = _editedScanNodeData.nodeType;
    }
}