using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabGrid : MonoBehaviour
{

    public GameObject prefab;
    public bool first = false;
    public int width = 1;
    public int depth = 1;
    public int spacing = 10;
    public bool editor = true;

    void Awake()
    {
        if(!editor && Application.isEditor)
        {
            return;
        }

        for(int i = 0; i<width; i++)
        {
            for (int j = 0; j < depth; j++)
            {
                if(i == 0 && j == 0 && !first)
                {
                    continue;
                }
                Instantiate(prefab, transform.position + new Vector3(i*spacing, 0, j*spacing), transform.rotation);
            }
        }
    }
}
