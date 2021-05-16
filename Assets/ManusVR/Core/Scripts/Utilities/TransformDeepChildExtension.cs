using UnityEngine;

namespace ManusVR.Core.Utilities
{
    public static class TransformDeepChildExtension
    {
        /// <summary>
        /// Finds a child transform of a given transform recursively, using a breadth-first search.
        /// </summary>
        /// <param name="parent">The Transform that the children should be searched for.</param>
        /// <param name="name">The name of the (grand)child to be found.</param>
        /// <returns>The child Transform with the given name if it was found, and null otherwise.</returns>
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            // Look for a direct child called name.
            Transform result = parent.Find(name);
            if (result != null)
            {
                return result;
            }

            // No direct child called name found. Do a search for each child.
            foreach (Transform child in parent)
            {
                result = FindDeepChild(child, name);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
