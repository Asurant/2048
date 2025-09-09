using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _length = 4;
    [SerializeField] private Node _nodePrefab;

    void Stat()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < _width; x++) {
            for (int y = 0; y < _length; y++) {
                var node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
            }
        }
    }
}
