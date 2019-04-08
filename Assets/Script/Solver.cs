using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Solver 
{

    public static List<int> Solve(int[] map, int width, int start, int goal)
    {
        int height = Mathf.CeilToInt((float)map.Length / width);
        Func<int, int> i2x = (i => i % width);
        Func<int, int> i2y = (i => i / width);
        Func<int, int, int> xy2i = (x, y) => y * width + x;
        Func<int, int, bool> isIn = (x, y) => x >= 0 && x < width && y >= 0 && y < height;
        Func<int, int, int> distance = (x, y) => Mathf.RoundToInt(Mathf.Pow(x - i2x(goal), 2) + Mathf.Pow(y - i2y(goal), 2));
        int maxCost = Mathf.RoundToInt(Mathf.Pow(width, 2) + Mathf.Pow(height, 2));
        Func<int, int> floorCost = c => c / maxCost * maxCost;

        List<int[]> open = new List<int[]>();
        List<int> closed = new List<int>();
        List<int[]> ptr = new List<int[]>();
        List<int> route = new List<int>();

        open.Add(new int[] { start, 0 });
        while (open.Count != 0)
        {
            int[] n = open[0];
            open.RemoveAt(0);

            if (n[0] == goal)
            {
                int tofind = goal;
                route.Add(goal);
                while (tofind != start)
                {
                    foreach (var p in ptr)
                    {
                        if (p[1] == tofind)
                        {
                            route.Insert(0, p[0]);
                            tofind = p[0];
                            break;
                        }
                    }
                }
                break;
            }
            else
            {
                closed.Add(n[0]);
                int x = i2x(n[0]);
                int y = i2y(n[0]);
                int[][] d = new int[][]
                {
                    new int[]{x-1, y },
                    new int[]{x+1, y },
                    new int[]{x, y-1 },
                    new int[]{x, y+1 },
                };
                for (int i = 0; i < d.Length; i++)
                {
                    int idx = xy2i(d[i][0], d[i][1]);
                    if (isIn(d[i][0], d[i][1]) && map[idx] != -1)
                    {
                        int[] m = new int[] { idx, floorCost(n[1]) + maxCost + distance(d[i][0], d[i][1]) };
                        int[] open_node = open.Find(o => o[0] == m[0]);
                        if (open_node != null && m[1] < open_node[1])
                        {
                            open.Remove(open_node);
                            ptr.RemoveAt(ptr.FindIndex(o => o[1] == m[0]));
                        }
                        if ((open_node == null || m[1] < open_node[1]) && closed.Contains(m[0]) == false)
                        {
                            open.Add(m);
                            ptr.Add(new int[] { n[0], m[0] });
                        }
                    }
                }
            }
            open.Sort((a, b) => a[1] - b[1] == 0 ? a[0] - b[0] : a[1] - b[1]);
        }
        return route;
    }
} 

