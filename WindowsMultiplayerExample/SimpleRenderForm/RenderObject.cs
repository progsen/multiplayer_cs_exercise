using System;
using System.Drawing;

namespace SimpleRenderForm
{
    class RenderObject
    {
        public RectangleF rectangle;
        public float frame;
        internal Point dir;

        public float speed = 40;
        internal float animationSpeed=10;
        internal string name;
    }
}
