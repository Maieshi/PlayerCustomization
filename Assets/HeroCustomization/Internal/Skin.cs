using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct Skin
{
    public string Position;
    public string Link;
    public bool IsModel;

    public Skin(string Position, string Link, bool IsModel)
    {
        this.Position = Position;
        this.Link = Link;
        this.IsModel = IsModel;
    }
}