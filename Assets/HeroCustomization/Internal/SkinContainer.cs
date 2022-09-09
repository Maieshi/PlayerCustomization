using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SkinContainer
{
    public List<Skin> Skins;

    public bool IsAnimated;

    public string UID;

    public SkinContainer(List<Skin> Skins, bool IsAnimated, string UID)
    {
        this.Skins = Skins;
        this.IsAnimated = IsAnimated;
        this.UID = UID;
    }
}
