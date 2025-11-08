using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bastard
{
    public static class TransformExtensions
    {
        public static string RelativePath(this Transform self, Transform root)
        {
            var path = new List<string>();
            for (var current = self; current != null; current = current.parent)
            {
                if (current == root)
                {
                    return string.Join("/", path.ToArray());
                }

                path.Insert(0, current.name);
            }

            throw new Exception("no RelativePath");
        }

        public static Transform GetChildByPath(this Transform parent, string path)
        {
            var target = parent;
            foreach (var name in path.Split("/"))
            {
                var err = true;
                for (int i = 0; i < target.childCount; i++)
                {
                    var child = target.GetChild(i);
                    if (child.name == name)
                    {
                        target = child;
                        err = false;
                        break;
                    }
                }
                if (err)
                {
                    return null;
                }
            }
            return target;
        }
    }
}