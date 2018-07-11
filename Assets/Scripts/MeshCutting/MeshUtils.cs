using System.Collections.Generic;
using UnityEngine;

public static class MeshUtils
{

    /// <summary>
    /// Find center of polygon by averaging vertices
    /// </summary>
    public static Vector3 FindCenter(List<Vector3> pairs)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        for (int i = 0; i < pairs.Count; i += 2)
        {
            center += pairs[i];
            count++;
        }

        return center / count;
    }


    /// <summary>
    /// Reorder a list of pairs of vectors (one dimension list where i and i + 1 defines a line segment)
    /// So that it forms a closed polygon 
    /// </summary>
    public static void ReorderList(List<Vector3> pairs)
    {
        int nbFaces = 0;
        int faceStart = 0;
        int i = 0;
        Vector3 tempFirst, tempSecond;

        i = 0;
        while (i < pairs.Count)
        {
            // Find next adjacent edge
            for (int j = i + 2; j < pairs.Count; j += 2)
            {
                // We use the equals function to test for complete equality, not approximate
                if (pairs[j] == pairs[i + 1])
                {
                    // If j is already at the correct place we break the loop
                    if (j == i + 2) break;

                    // Put j at i+2
                    tempFirst = pairs[i + 2];
                    tempSecond = pairs[i + 3];
                    pairs[i + 2] = pairs[j];
                    pairs[i + 3] = pairs[j + 1];
                    pairs[j] = tempFirst;
                    pairs[j + 1] = tempSecond;
                    break;
                }
            }

            if (i + 3 >= pairs.Count)
            {
                // Why does this happen?
                /* This seems to happen because edges are so small that at a certain point
                 * some vertices are equal to eachother and one edge is being 
                 */

                Debug.Log("Huh?");
                break;
            }
            else if (pairs[i + 3] == pairs[faceStart])   //TODO: Index out of range error happens here!
            {
                // A face is complete.
                nbFaces++;
                i += 4;
                faceStart = i;
            }
            else
            {
                i += 2;
            }
        }
    }

}
