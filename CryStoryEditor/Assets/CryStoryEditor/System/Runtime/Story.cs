﻿/**********************************************************
*Author: wangjiaying
*Date: 2016.6.16
*Func:
**********************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryStory.Runtime
{
    public class Story : DragModifier
    {
        public List<Mission> _missionList = new List<Mission>();
        public int ID;
    }
}