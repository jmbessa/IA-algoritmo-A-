using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {
    //Se o node é caminhável ou não
    public bool walkable;

    //Se o node é especial ou não
    public bool isSpecialNode;

    //Posição do node no mundo
    public Vector3 worldPosition;

    //Posição do node na grid
    public int gridX;
    public int gridY;

    //Custo particular desse tipo de node (entra no cálculo do gCost)
    public int movementPenalty;

    //Custos Gerais
    public int gCost;
    public int hCost;

    //Guarda Node que faz parte do caminho
    public Node parent;

    //Construtor
    public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
        movementPenalty = _penalty;
    }

    //Valor de fCost = gcost + hCost (custo total)
    public int fCost {
        get{ return gCost + hCost; }
    }

}
