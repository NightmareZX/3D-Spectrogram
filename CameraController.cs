using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinForms_TestApp
{
    public class CameraController
    {
        public float xRot = 0;
        public float yRot = 0;
        public float zRot = 0;
        public float scaleFactor = 3.0f;
        public bool dragging = false;
        public float curX = 0;
        public float curY = 0;
    }
}
