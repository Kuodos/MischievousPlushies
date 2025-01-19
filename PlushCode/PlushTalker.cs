using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using UnityEngine;


namespace MischievousPlushies.PlushCode
{
    public class PlushTalker
    {
        public static string? recordings;
        void Awake(){
            if(recordings==null){
                recordings = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                
            }
        }
    }
}