using System;
using UnityEngine;

// Taken by TheCSDev from <https://discussions.unity.com/t/check-if-component-is-requred-by-another/172189> in the unity forums.

namespace CodeRebirth.src.Util.Extensions;

public static class GameObjectUtils
{
    /// <summary>
    /// Iterates over all <paramref name="gameObject"/> components,
    /// and checks if any of them require a given <paramref name="componentType"/>.
    /// </summary>
    /// <returns>True if <paramref name="gameObject"/> requires <paramref name="componentType"/>.</returns>
    public static bool RequiresComponent(this GameObject gameObject, Type componentType)
    {
        //iterate all gameObject's components to see if
        //one of them requires the componentType
        foreach (Component component in gameObject.GetComponents<Component>())
        {
            //iterate all component's attributes, look for
            //the RequireComponent attributes
            foreach (object attr in component.GetType().GetCustomAttributes(true))
            {
                //cast the attribute to RequireComponent
                //check if the attribute is RequireComponent
                if (attr is RequireComponent rcAttr)
                {
                    //check all three of the required types to see if
                    //componentType is required (for some reason, you
                    //can require up to 3 component types per attribute).
                    if ((rcAttr.m_Type0?.IsAssignableFrom(componentType) ?? false) ||
                        (rcAttr.m_Type1?.IsAssignableFrom(componentType) ?? false) ||
                        (rcAttr.m_Type2?.IsAssignableFrom(componentType) ?? false))
                    {
                        //if the component is required, return true
                        return true;
                    }
                }
            }
        }

        //if no components require it, return false
        return false;
    }
}