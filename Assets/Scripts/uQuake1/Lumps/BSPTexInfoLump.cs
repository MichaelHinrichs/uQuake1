﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class BSPTexInfoLump
{
    public List<BSPTexInfo> texinfo = new List<BSPTexInfo>();

    public BSPTexInfoLump()
    {
    }

    public void PrintInfo()
    {
        Debug.Log("TexInfos:\r\n");
        foreach (BSPTexInfo tex in texinfo)
        {
            Debug.Log(tex.ToString());
        }
    }
}

