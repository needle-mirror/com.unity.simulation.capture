using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




/// <summary>
/// The AddCollectorMenu attribute allows you to organize collectors under different menu paths
/// </summary>
public class AddCollectorMenuAttribute : Attribute
{
    /// <summary>
    /// The assigned collector menu path. Path directories should be separated using forward slash characters.
    /// </summary>
    public string menuPath;

    /// <summary>
    /// Add a collector to the AddCollectorMenus
    /// </summary>
    /// <param name="menuPath">The assigned collector menu path</param>
    public AddCollectorMenuAttribute(string menuPath)
    {
        this.menuPath = menuPath;
    }
}
