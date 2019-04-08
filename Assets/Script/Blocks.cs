using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Blocks
{
    public class BlockObj
    {
        public BlockObj(int x, int z, GameObject b)
        {
            this.X = x;
            this.Z = z;
            this.Block = b;
        }
        public int X { get; private set; }
        public int Z { get; private set; }
        public GameObject Block { get; set; }
    }

    GameObject prefab;
    Transform floor;
    int width;
    int height;
    BlockObj[] blocks;
    int[] map;
    bool remap;
    Vector3 blockSize;
    string prefsName;

    public Blocks(GameObject prefab, Transform floor, int dx, int dz, string prefsName)
    {
        this.prefab = prefab;
        this.floor = floor;
        this.width = dx;
        this.height = dz;
        this.prefsName = prefsName;
        this.blockSize = prefab.GetComponent<Transform>().localScale;

        blocks = new BlockObj[width * height];
        map = new int[blocks.Length];
         foreach(var  item in blocks.Select((v,i) =>  new { v, i })){
            blocks[item.i] = new BlockObj(i2x(item.i), i2z(item.i), null);
        }
    }

    public void Init(Dictionary<string, int[]> objPositions)
    {
        int[] mapv = PlayerPrefs.GetString(prefsName).Split(',').Where(s => s.Length != 0).Select(s => int.Parse(s)).ToArray();
        foreach (var item in blocks.Select((v, i) => new { v, i }))
        {
            int x = i2x(item.i);
            int z = i2z(item.i);

            bool b0 = objPositions.Any(i => i.Value[0] == x && i.Value[1] == z) == false;
            bool b1 = b0 && (item.i < mapv.Length ? mapv[item.i] == -1 : false);
            if (b1)
            {
                CreateBlock(x, z);
            }
        }
    }

    public int i2x(int i)
    {
        return i % width;
    }
    public int i2z(int i)
    {
        return i / width;
    }
    public int[] i2xz(int i)
    {
        return new int[] { i2x(i), i2z(i) };
    }
    public int xz2i(int[] xz)
    {
        return xz2i(xz[0], xz[1]);
    }
    public int xz2i(int x, int z)
    {
        return x + z * width;
    }

    public BlockObj Find(GameObject obj)
    {
        return Array.Find<BlockObj>(blocks, x => x.Block == obj);
    }
    public int[] GetBlockIndexXZ(Vector3 pos)
    {
        int[] index =  {
            Mathf.FloorToInt(( ( pos.x - (floor.position.x - floor.localScale.x/2f) ) * width / floor.localScale.x )),
            Mathf.FloorToInt( ( pos.z - (floor.position.z - floor.localScale.z/2f) ) * height / floor.localScale.z ),
        };
        return index;
    }
    public int GetBlockIndex(Vector3 pos)
    {
        return xz2i(GetBlockIndexXZ(pos));
    }
    public void CreateBlock(int x, int z, bool save = true)
    {
        blocks[xz2i(x, z)].Block = UnityEngine.Object.Instantiate(prefab, GetBlockPosition(x, z), Quaternion.identity);
        remap = true;
        if (save)
        {
             SavePrefs();
        }
    }
    public Vector3 GetBlockPosition(int index)
    {
        return GetBlockPosition(i2x(index), i2z(index));
    }
    public Vector3 GetBlockPosition(int iX, int iZ)
    {
        return new Vector3(
            iX * floor.localScale.x / width + (floor.position.x - floor.localScale.x / 2f) + blockSize.x / 2f,
            floor.position.y + floor.localScale.y / 2f + blockSize.y / 2f,
            iZ * floor.localScale.z / height + (floor.position.z - floor.localScale.z / 2f) + blockSize.z / 2f
            );
    }
    public void RemoveBlock(int x, int z, bool save = true)
    {
        RemoveBlock(blocks[xz2i(x, z)], save);
    }
    public void RemoveBlock(BlockObj obj, bool save = true)
    {
        UnityEngine.Object.Destroy(obj.Block);
        obj.Block = null;
        remap = true;
        if (save)
        {
             SavePrefs();
        }
    }

    public void SavePrefs()
    {
        GetMap();
        PlayerPrefs.SetString(prefsName, string.Join(",", map.Select(x => x.ToString()).ToArray()));
        PlayerPrefs.Save();
    }
    public void DeletePrefs()
    {
        PlayerPrefs.DeleteKey(prefsName);
    }
    public int[] GetMap()
    {
        if (remap)
        {
            foreach (var item in blocks.Select((v, i) => new { v, i }))
            {
                map[xz2i(item.v.X, item.v.Z)] = (item.v.Block == null ? 1 : -1);
            }
            remap = false;
        }
        return map;
    }
    public bool All(System.Func<int, int, bool> f)
    {
        return blocks.Select((v, i) => new { v, i }).All(item => { return f(i2x(item.i), i2z(item.i)); });
    }
    public bool IsIn(int x, int z)
    {
        return x >= 0 && x < width && z >= 0 && z < height;
    }
    public bool IsWall(int x, int z)
    {
        GetMap();
        return IsIn(x, z) == false || map[xz2i(x, z)] == -1;
    }
    public List<int> Solve(int start, int end)
    {
        return Solver.Solve(GetMap(), width, start, end);
    }
}
