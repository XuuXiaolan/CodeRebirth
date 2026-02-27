using System.Collections;
using System.Collections.Generic;
using Dawn;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.ProBuilder;

namespace CodeRebirth.src.Content.DevTools;

public class HologramCopy
{
    public GameObject HologramObject { get; private set; }

    private Vector3 rotationOffset = Vector3.zero;

    public void SetUpHologram(GameObject gameObject)
    {
        // create a hologram copy of the map object and set it up to be displayed as a hologram
        HologramObject = GameObject.Instantiate(gameObject, Vector3.zero, Quaternion.identity);
        List<Component> components = [.. HologramObject.GetComponentsInChildren<Component>()];
        HashSet<Renderer> renderers = new();
        foreach (Component component in components)
        {
            if (component is Transform or MeshFilter)
            {
                continue;
            }

            if (component is Renderer renderer)
            {
                renderers.Add(renderer);
                continue;
            }

            if (component is ProBuilderMesh proBuilderMesh)
            {
                renderers.Add(proBuilderMesh.renderer);
                continue;
            }

            Component.Destroy(component);
        }

        HologramObject.SetActive(false);
    }

    public void UpdateTick(UnityEngine.RaycastHit raycastHit)
    {
        Vector3 positionOffset = Vector3.zero;
        positionOffset += upDownOffset * GameNetworkManager.Instance.localPlayerController.transform.up;
        positionOffset += leftRightOffset * GameNetworkManager.Instance.localPlayerController.transform.right;
        positionOffset += forwardBackOffset * GameNetworkManager.Instance.localPlayerController.transform.forward;
        HologramObject.transform.position = raycastHit.point + positionOffset;
        HologramObject.transform.rotation = Quaternion.Euler(rotationOffset);
        HologramObject.SetActive(true);
    }

    private enum OffsetAxis { UpDown, LeftRight, ForwardBack, RotateX, RotateY, RotateZ }

    private void AddOffset(OffsetAxis axis, float delta)
    {
        switch (axis)
        {
            case OffsetAxis.UpDown:
                upDownOffset += delta;
                break;
            case OffsetAxis.LeftRight:
                leftRightOffset += delta;
                break;
            case OffsetAxis.ForwardBack:
                forwardBackOffset += delta;
                break;
            case OffsetAxis.RotateX:
                rotationOffset.x += delta;
                break;
            case OffsetAxis.RotateY:
                rotationOffset.y += delta;
                break;
            case OffsetAxis.RotateZ:
                rotationOffset.z += delta;
                break;
        }
    }

    internal void ResetRotationAndPositionEdits()
    {
        upDownOffset = 0f;
        leftRightOffset = 0f;
        forwardBackOffset = 0f;
        rotationOffset = Vector3.zero;
    }

    private float upDownOffset = 0f;
    private float leftRightOffset = 0f;
    private float forwardBackOffset = 0f;

    internal void EditPositionOffset(Vector3 normalizedDirection, KeyControl keyControl, float amount)
    {
        if (normalizedDirection == Vector3.up)
        {
            upDownOffset += amount;
            GameNetworkManager.Instance.localPlayerController.StartCoroutine(IncreaseNumberOverTime(OffsetAxis.UpDown, keyControl, amount));
        }
        else if (normalizedDirection == Vector3.right)
        {
            leftRightOffset += amount;
            GameNetworkManager.Instance.localPlayerController.StartCoroutine(IncreaseNumberOverTime(OffsetAxis.LeftRight, keyControl, amount));
        }
        else if (normalizedDirection == Vector3.forward)
        {
            forwardBackOffset += amount;
            GameNetworkManager.Instance.localPlayerController.StartCoroutine(IncreaseNumberOverTime(OffsetAxis.ForwardBack, keyControl, amount));
        }
    }

    internal void RotateUpDown(KeyControl keyControl, float angleIncrease)
    {
        rotationOffset += new Vector3(angleIncrease, 0, 0);
        GameNetworkManager.Instance.localPlayerController.StartCoroutine(IncreaseNumberOverTime(OffsetAxis.RotateX, keyControl, angleIncrease));

    }

    internal void RotateLeftRight(KeyControl keyControl, float angleIncrease)
    {
        rotationOffset += new Vector3(0, angleIncrease, 0);
        GameNetworkManager.Instance.localPlayerController.StartCoroutine(IncreaseNumberOverTime(OffsetAxis.RotateY, keyControl, angleIncrease));
    }

    private IEnumerator IncreaseNumberOverTime(OffsetAxis offsetAxis, KeyControl keyControl, float amountToIncrease)
    {
        yield return new WaitForSeconds(0.1f);
        if (!keyControl.isPressed)
        {
            yield break;
        }

        AddOffset(offsetAxis, amountToIncrease);
        yield return new WaitForSeconds(0.1f);
        if (!keyControl.isPressed)
        {
            yield break;
        }

        AddOffset(offsetAxis, amountToIncrease);
        yield return new WaitForSeconds(0.05f);
        if (!keyControl.isPressed)
        {
            yield break;
        }

        AddOffset(offsetAxis, amountToIncrease);
        yield return null;
        while (keyControl.isPressed)
        {
            AddOffset(offsetAxis, amountToIncrease);
            yield return null;
        }
    }

    internal void HandleSpawningOriginal(DawnMapObjectInfo dawnMapObjectInfoContainer, UnityEngine.RaycastHit raycastHit)
    {
        Vector3 positionOffset = Vector3.zero;
        positionOffset += upDownOffset * GameNetworkManager.Instance.localPlayerController.transform.up;
        positionOffset += leftRightOffset * GameNetworkManager.Instance.localPlayerController.transform.right;
        positionOffset += forwardBackOffset * GameNetworkManager.Instance.localPlayerController.transform.forward;

        if (dawnMapObjectInfoContainer.HasNetworkObject)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                GameObject gameObject = GameObject.Instantiate(dawnMapObjectInfoContainer.MapObject, raycastHit.point + positionOffset, Quaternion.Euler(rotationOffset));
                gameObject.GetComponent<NetworkObject>().Spawn();
            }
        }
        else
        {
            GameObject.Instantiate(dawnMapObjectInfoContainer.MapObject, raycastHit.point + positionOffset, Quaternion.Euler(rotationOffset));
        }
    }
}